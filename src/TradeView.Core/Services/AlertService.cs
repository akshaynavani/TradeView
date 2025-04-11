using TradeView.Core.Abstractions;
using TradeView.Core.Domain;
using TradeView.Core.Exceptions;

namespace TradeView.Core.Services;

public sealed class AlertService
{
    public const int MaxTriggerLimit = 1000;
    public const int MaxCooldownSeconds = 7 * 24 * 3600;

    private readonly IAlertRepository _alerts;
    private readonly IInstrumentRepository _instruments;
    private readonly IPriceTickRepository _ticks;
    private readonly IAlertNotifier _notifier;
    private readonly IClock _clock;

    public AlertService(
        IAlertRepository alerts,
        IInstrumentRepository instruments,
        IPriceTickRepository ticks,
        IAlertNotifier notifier,
        IClock clock)
    {
        _alerts = alerts;
        _instruments = instruments;
        _ticks = ticks;
        _notifier = notifier;
        _clock = clock;
    }

    public async Task<PriceAlert> CreateAsync(
        string symbol,
        AlertCondition condition,
        decimal threshold,
        bool isRepeating,
        int cooldownSeconds,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var instrument = await GetInstrumentAsync(symbol, cancellationToken);
        ValidateRule(threshold, cooldownSeconds);

        // Capture the current price so PercentMove has a base and every alert records context.
        var latest = await _ticks.GetLatestAsync(instrument.Id, cancellationToken);
        if (condition == AlertCondition.PercentMove && latest is null)
        {
            throw new DomainValidationException(
                $"A percent-move alert needs a reference price, but {instrument.Symbol} has no quotes yet.");
        }

        var alert = new PriceAlert
        {
            InstrumentId = instrument.Id,
            Instrument = instrument,
            Condition = condition,
            Threshold = threshold,
            ReferencePrice = latest?.Price,
            IsActive = true,
            IsRepeating = isRepeating,
            CooldownSeconds = cooldownSeconds,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CreatedAtUtc = _clock.UtcNow
        };

        await _alerts.AddAsync(alert, cancellationToken);
        await _alerts.SaveChangesAsync(cancellationToken);
        return alert;
    }

    public async Task<PriceAlert> GetAsync(int id, CancellationToken cancellationToken = default)
        => await _alerts.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Alert", id);

    public async Task<IReadOnlyList<PriceAlert>> ListAsync(
        string? symbol,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        int? instrumentId = string.IsNullOrWhiteSpace(symbol)
            ? null
            : (await GetInstrumentAsync(symbol, cancellationToken)).Id;

        return await _alerts.QueryAsync(instrumentId, activeOnly, cancellationToken);
    }

    /// <summary>
    /// Partially updates an alert. Re-activating an alert also re-bases its reference price
    /// so a percent-move alert measures from the current market, not from when it was created.
    /// </summary>
    public async Task<PriceAlert> UpdateAsync(
        int id,
        decimal? threshold,
        bool? isActive,
        bool? isRepeating,
        int? cooldownSeconds,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var alert = await GetAsync(id, cancellationToken);

        ValidateRule(threshold ?? alert.Threshold, cooldownSeconds ?? alert.CooldownSeconds);

        if (threshold.HasValue)
        {
            alert.Threshold = threshold.Value;
        }

        if (isRepeating.HasValue)
        {
            alert.IsRepeating = isRepeating.Value;
        }

        if (cooldownSeconds.HasValue)
        {
            alert.CooldownSeconds = cooldownSeconds.Value;
        }

        if (note is not null)
        {
            alert.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        }

        if (isActive.HasValue && isActive.Value != alert.IsActive)
        {
            alert.IsActive = isActive.Value;
            if (alert.IsActive)
            {
                var latest = await _ticks.GetLatestAsync(alert.InstrumentId, cancellationToken);
                alert.ReferencePrice = latest?.Price ?? alert.ReferencePrice;
                alert.LastTriggeredAtUtc = null;
            }
        }

        await _alerts.SaveChangesAsync(cancellationToken);
        return alert;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var alert = await GetAsync(id, cancellationToken);
        _alerts.Remove(alert);
        await _alerts.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AlertTrigger>> GetTriggersAsync(
        int? alertId,
        string? symbol,
        DateTime? sinceUtc,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > MaxTriggerLimit)
        {
            throw new DomainValidationException($"limit must be between 1 and {MaxTriggerLimit}.");
        }

        int? instrumentId = string.IsNullOrWhiteSpace(symbol)
            ? null
            : (await GetInstrumentAsync(symbol, cancellationToken)).Id;

        return await _alerts.GetTriggersAsync(
            alertId,
            instrumentId,
            sinceUtc.HasValue ? MarketDataService.AsUtc(sinceUtc.Value) : null,
            limit,
            cancellationToken);
    }

    /// <summary>
    /// Evaluates every active alert against the newest tick per instrument in <paramref name="ticks"/>,
    /// persists fired triggers together with alert state changes, and notifies.
    /// </summary>
    public async Task<IReadOnlyList<AlertTrigger>> EvaluateAsync(
        IReadOnlyList<PriceTick> ticks,
        CancellationToken cancellationToken = default)
    {
        if (ticks.Count == 0)
        {
            return Array.Empty<AlertTrigger>();
        }

        var latestByInstrument = ticks
            .GroupBy(t => t.InstrumentId)
            .ToDictionary(g => g.Key, g => g.OrderBy(t => t.TimestampUtc).Last());

        var alerts = await _alerts.GetActiveForInstrumentsAsync(latestByInstrument.Keys.ToList(), cancellationToken);
        if (alerts.Count == 0)
        {
            return Array.Empty<AlertTrigger>();
        }

        var now = _clock.UtcNow;
        var triggers = new List<AlertTrigger>();

        foreach (var alert in alerts)
        {
            var tick = latestByInstrument[alert.InstrumentId];
            var symbol = tick.Instrument?.Symbol ?? alert.InstrumentId.ToString();

            var trigger = AlertEvaluator.Apply(alert, symbol, tick.Price, now);
            if (trigger is not null)
            {
                triggers.Add(trigger);
            }
        }

        if (triggers.Count > 0)
        {
            await _alerts.AddTriggersAsync(triggers, cancellationToken);
            await _alerts.SaveChangesAsync(cancellationToken);
            await _notifier.NotifyAsync(triggers, cancellationToken);
        }

        return triggers;
    }

    private static void ValidateRule(decimal threshold, int cooldownSeconds)
    {
        if (threshold <= 0)
        {
            throw new DomainValidationException("Threshold must be greater than zero.");
        }

        if (cooldownSeconds is < 0 or > MaxCooldownSeconds)
        {
            throw new DomainValidationException($"cooldownSeconds must be between 0 and {MaxCooldownSeconds}.");
        }
    }

    private async Task<Instrument> GetInstrumentAsync(string symbol, CancellationToken cancellationToken)
    {
        var normalized = InstrumentService.NormalizeSymbol(symbol);
        return await _instruments.GetBySymbolAsync(normalized, cancellationToken)
            ?? throw new NotFoundException("Instrument", normalized);
    }
}
