using TradeView.Core.Abstractions;
using TradeView.Core.Domain;
using TradeView.Core.Exceptions;
using TradeView.Core.Models;

namespace TradeView.Core.Services;

public sealed class MarketDataService
{
    public const int MaxHistoryLimit = 5000;

    private readonly IInstrumentRepository _instruments;
    private readonly IPriceTickRepository _ticks;
    private readonly IMarketDataFeed _feed;
    private readonly IClock _clock;

    public MarketDataService(
        IInstrumentRepository instruments,
        IPriceTickRepository ticks,
        IMarketDataFeed feed,
        IClock clock)
    {
        _instruments = instruments;
        _ticks = ticks;
        _feed = feed;
        _clock = clock;
    }

    /// <summary>
    /// Pulls one round of quotes for every active instrument and persists them as ticks.
    /// Returns the new ticks (with <see cref="PriceTick.Instrument"/> populated).
    /// </summary>
    public async Task<IReadOnlyList<PriceTick>> IngestAsync(CancellationToken cancellationToken = default)
    {
        var instruments = await _instruments.GetAllAsync(activeOnly: true, cancellationToken);
        if (instruments.Count == 0)
        {
            return Array.Empty<PriceTick>();
        }

        var latest = await _ticks.GetLatestAsync(instruments.Select(i => i.Id).ToList(), cancellationToken);
        var lastKnownPrices = latest.ToDictionary(kv => kv.Key, kv => kv.Value.Price);

        var quotes = await _feed.GetQuotesAsync(instruments, lastKnownPrices, cancellationToken);
        var byId = instruments.ToDictionary(i => i.Id);

        var ticks = quotes
            .Where(q => byId.ContainsKey(q.InstrumentId) && q.Price > 0)
            .Select(q => new PriceTick
            {
                InstrumentId = q.InstrumentId,
                Instrument = byId[q.InstrumentId],
                Price = q.Price,
                Volume = q.Volume,
                TimestampUtc = q.TimestampUtc
            })
            .ToList();

        if (ticks.Count > 0)
        {
            await _ticks.AddRangeAsync(ticks, cancellationToken);
        }

        return ticks;
    }

    /// <summary>Latest tick for every instrument (active or not) that has one.</summary>
    public async Task<IReadOnlyList<(Instrument Instrument, PriceTick Tick)>> GetLatestQuotesAsync(
        CancellationToken cancellationToken = default)
    {
        var instruments = await _instruments.GetAllAsync(activeOnly: false, cancellationToken);
        var latest = await _ticks.GetLatestAsync(instruments.Select(i => i.Id).ToList(), cancellationToken);

        return instruments
            .Where(i => latest.ContainsKey(i.Id))
            .Select(i => (i, latest[i.Id]))
            .ToList();
    }

    public async Task<(Instrument Instrument, PriceTick Tick)> GetLatestQuoteAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        var instrument = await GetInstrumentAsync(symbol, cancellationToken);
        var tick = await _ticks.GetLatestAsync(instrument.Id, cancellationToken)
            ?? throw new NotFoundException("Quote", instrument.Symbol);
        return (instrument, tick);
    }

    public async Task<IReadOnlyList<PriceTick>> GetHistoryAsync(
        string symbol,
        DateTime? fromUtc,
        DateTime? toUtc,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var instrument = await GetInstrumentAsync(symbol, cancellationToken);
        var (from, to) = ResolveWindow(fromUtc, toUtc);
        ValidateLimit(limit);
        return await _ticks.GetHistoryAsync(instrument.Id, from, to, limit, cancellationToken);
    }

    public async Task<PriceSummary> GetSummaryAsync(
        string symbol,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        var instrument = await GetInstrumentAsync(symbol, cancellationToken);
        var (from, to) = ResolveWindow(fromUtc, toUtc);
        var ticks = await _ticks.GetHistoryAsync(instrument.Id, from, to, MaxHistoryLimit, cancellationToken);

        return PriceStatistics.Summarize(instrument.Symbol, from, to, ticks)
            ?? throw new NotFoundException("Price data in window for", instrument.Symbol);
    }

    public Task<int> PruneAsync(TimeSpan retention, CancellationToken cancellationToken = default)
        => _ticks.DeleteOlderThanAsync(_clock.UtcNow - retention, cancellationToken);

    private async Task<Instrument> GetInstrumentAsync(string symbol, CancellationToken cancellationToken)
    {
        var normalized = InstrumentService.NormalizeSymbol(symbol);
        return await _instruments.GetBySymbolAsync(normalized, cancellationToken)
            ?? throw new NotFoundException("Instrument", normalized);
    }

    private (DateTime From, DateTime To) ResolveWindow(DateTime? fromUtc, DateTime? toUtc)
    {
        var to = (toUtc ?? _clock.UtcNow).ToUniversalTime();
        var from = (fromUtc ?? to.AddHours(-1)).ToUniversalTime();

        if (from > to)
        {
            throw new DomainValidationException("'from' must be earlier than 'to'.");
        }

        return (from, to);
    }

    private static void ValidateLimit(int limit)
    {
        if (limit is < 1 or > MaxHistoryLimit)
        {
            throw new DomainValidationException($"limit must be between 1 and {MaxHistoryLimit}.");
        }
    }
}
