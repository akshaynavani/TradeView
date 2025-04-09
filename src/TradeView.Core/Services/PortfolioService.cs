using TradeView.Core.Abstractions;
using TradeView.Core.Domain;
using TradeView.Core.Exceptions;
using TradeView.Core.Models;

namespace TradeView.Core.Services;

/// <summary>Records trades and produces position / portfolio P&amp;L marked to the latest quotes.</summary>
public sealed class PortfolioService
{
    private readonly IInstrumentRepository _instruments;
    private readonly ITradeRepository _trades;
    private readonly IPriceTickRepository _ticks;
    private readonly IClock _clock;

    public PortfolioService(
        IInstrumentRepository instruments,
        ITradeRepository trades,
        IPriceTickRepository ticks,
        IClock clock)
    {
        _instruments = instruments;
        _trades = trades;
        _ticks = ticks;
        _clock = clock;
    }

    /// <summary>
    /// Records a fill. When <paramref name="price"/> is omitted the trade executes at the latest quote.
    /// The full trade history for the instrument is replayed first so a back-dated sell cannot
    /// push the position negative at any point in time.
    /// </summary>
    public async Task<Trade> RecordTradeAsync(
        string symbol,
        TradeSide side,
        decimal quantity,
        decimal? price,
        decimal fees,
        DateTime? executedAtUtc,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var instrument = await GetInstrumentAsync(symbol, cancellationToken);
        var now = _clock.UtcNow;
        var executedAt = executedAtUtc.HasValue ? MarketDataService.AsUtc(executedAtUtc.Value) : now;

        if (executedAt > now.AddMinutes(1))
        {
            throw new DomainValidationException("Trades cannot be executed in the future.");
        }

        var fillPrice = price ?? (await _ticks.GetLatestAsync(instrument.Id, cancellationToken))?.Price
            ?? throw new DomainValidationException(
                $"No price supplied and no market quote is available yet for {instrument.Symbol}.");

        var trade = new Trade
        {
            InstrumentId = instrument.Id,
            Side = side,
            Quantity = quantity,
            Price = fillPrice,
            Fees = fees,
            ExecutedAtUtc = executedAt,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };

        // Unsaved trades have Id 0; give the candidate the highest id so it sorts after
        // existing trades executed at the same instant.
        var existing = await _trades.GetForInstrumentAsync(instrument.Id, cancellationToken);
        var candidate = new Trade
        {
            Id = long.MaxValue,
            InstrumentId = trade.InstrumentId,
            Side = trade.Side,
            Quantity = trade.Quantity,
            Price = trade.Price,
            Fees = trade.Fees,
            ExecutedAtUtc = trade.ExecutedAtUtc
        };
        PnlCalculator.EnsureConsistent(instrument.Id, instrument.Symbol, existing.Append(candidate));

        await _trades.AddAsync(trade, cancellationToken);
        await _trades.SaveChangesAsync(cancellationToken);
        trade.Instrument = instrument;
        return trade;
    }

    public async Task<Trade> GetTradeAsync(long id, CancellationToken cancellationToken = default)
        => await _trades.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Trade", id);

    public async Task<IReadOnlyList<Trade>> ListTradesAsync(
        string? symbol,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        int? instrumentId = null;
        if (!string.IsNullOrWhiteSpace(symbol))
        {
            instrumentId = (await GetInstrumentAsync(symbol, cancellationToken)).Id;
        }

        return await _trades.QueryAsync(
            instrumentId,
            fromUtc.HasValue ? MarketDataService.AsUtc(fromUtc.Value) : null,
            toUtc.HasValue ? MarketDataService.AsUtc(toUtc.Value) : null,
            cancellationToken);
    }

    /// <summary>Deletes a trade, refusing if that would leave a later sell uncovered.</summary>
    public async Task DeleteTradeAsync(long id, CancellationToken cancellationToken = default)
    {
        var trade = await GetTradeAsync(id, cancellationToken);
        var remaining = (await _trades.GetForInstrumentAsync(trade.InstrumentId, cancellationToken))
            .Where(t => t.Id != id);

        PnlCalculator.EnsureConsistent(trade.InstrumentId, trade.Instrument?.Symbol ?? trade.InstrumentId.ToString(), remaining);

        _trades.Remove(trade);
        await _trades.SaveChangesAsync(cancellationToken);
    }

    public async Task<PositionSummary> GetPositionAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var instrument = await GetInstrumentAsync(symbol, cancellationToken);
        var trades = await _trades.GetForInstrumentAsync(instrument.Id, cancellationToken);
        var latest = await _ticks.GetLatestAsync(instrument.Id, cancellationToken);

        return PnlCalculator.Calculate(instrument.Id, instrument.Symbol, trades, latest?.Price);
    }

    public async Task<PortfolioSummary> GetPortfolioAsync(bool includeClosed, CancellationToken cancellationToken = default)
    {
        var trades = await _trades.GetAllAsync(cancellationToken);
        var byInstrument = trades.GroupBy(t => t.InstrumentId).ToList();

        var latest = await _ticks.GetLatestAsync(byInstrument.Select(g => g.Key).ToList(), cancellationToken);

        var positions = byInstrument
            .Select(g =>
            {
                var symbol = g.First().Instrument?.Symbol ?? g.Key.ToString();
                decimal? mark = latest.TryGetValue(g.Key, out var tick) ? tick.Price : null;
                return PnlCalculator.Calculate(g.Key, symbol, g, mark);
            })
            .Where(p => includeClosed || p.IsOpen)
            .OrderBy(p => p.Symbol)
            .ToList();

        return new PortfolioSummary(_clock.UtcNow, positions);
    }

    private async Task<Instrument> GetInstrumentAsync(string symbol, CancellationToken cancellationToken)
    {
        var normalized = InstrumentService.NormalizeSymbol(symbol);
        return await _instruments.GetBySymbolAsync(normalized, cancellationToken)
            ?? throw new NotFoundException("Instrument", normalized);
    }
}
