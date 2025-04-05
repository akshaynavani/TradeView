using TradeView.Core.Domain;
using TradeView.Core.Models;

namespace TradeView.Core.Abstractions;

/// <summary>
/// Source of live prices. The default implementation is a simulator; a real vendor
/// adapter (Polygon, Alpaca, a broker websocket, ...) can be swapped in via DI.
/// </summary>
public interface IMarketDataFeed
{
    /// <param name="instruments">Instruments to quote.</param>
    /// <param name="lastKnownPrices">Last persisted price per instrument id, used to resume after a restart.</param>
    Task<IReadOnlyList<MarketQuote>> GetQuotesAsync(
        IReadOnlyList<Instrument> instruments,
        IReadOnlyDictionary<int, decimal> lastKnownPrices,
        CancellationToken cancellationToken = default);
}
