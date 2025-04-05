using TradeView.Core.Domain;
using TradeView.Core.Models;

namespace TradeView.Core.Services;

/// <summary>Pure aggregation helpers over price ticks.</summary>
public static class PriceStatistics
{
    /// <summary>
    /// Builds an OHLC summary. Aggregation is done in memory on purpose: SQLite cannot
    /// aggregate or order decimal columns server-side, and windows are bounded by a limit.
    /// </summary>
    public static PriceSummary? Summarize(string symbol, DateTime fromUtc, DateTime toUtc, IReadOnlyList<PriceTick> ticks)
    {
        if (ticks.Count == 0)
        {
            return null;
        }

        var ordered = ticks.OrderBy(t => t.TimestampUtc).ThenBy(t => t.Id).ToList();
        var open = ordered[0].Price;
        var close = ordered[^1].Price;
        var high = ordered.Max(t => t.Price);
        var low = ordered.Min(t => t.Price);
        var totalVolume = ordered.Sum(t => t.Volume);

        var vwap = totalVolume > 0
            ? ordered.Sum(t => t.Price * t.Volume) / totalVolume
            : ordered.Average(t => t.Price);

        var change = close - open;
        var changePercent = open != 0 ? change / open * 100m : 0m;

        return new PriceSummary(
            symbol,
            fromUtc,
            toUtc,
            ordered.Count,
            open,
            high,
            low,
            close,
            Math.Round(change, 6),
            Math.Round(changePercent, 4),
            totalVolume,
            Math.Round(vwap, 6));
    }
}
