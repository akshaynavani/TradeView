using TradeView.Core.Domain;
using TradeView.Core.Exceptions;
using TradeView.Core.Models;

namespace TradeView.Core.Services;

/// <summary>
/// Average-cost P&amp;L for long-only positions.
/// <list type="bullet">
/// <item>Buy: quantity increases; the average cost blends in the fill price plus fees.</item>
/// <item>Sell: realizes (price - averageCost) * quantity - fees; average cost is unchanged.</item>
/// <item>When the position goes flat the average cost resets to zero.</item>
/// </list>
/// Trades are replayed in execution order (ties broken by id). Pure and side-effect free.
/// </summary>
public static class PnlCalculator
{
    public const int Precision = 8;

    public static PositionSummary Calculate(
        int instrumentId,
        string symbol,
        IEnumerable<Trade> trades,
        decimal? marketPrice)
    {
        var quantity = 0m;
        var averageCost = 0m;
        var realized = 0m;
        var fees = 0m;
        var count = 0;

        foreach (var trade in Order(trades))
        {
            if (trade.InstrumentId != instrumentId)
            {
                throw new ArgumentException(
                    $"Trade {trade.Id} belongs to instrument {trade.InstrumentId}, expected {instrumentId}.",
                    nameof(trades));
            }

            ValidateTrade(trade);
            count++;
            fees += trade.Fees;

            switch (trade.Side)
            {
                case TradeSide.Buy:
                    var newQuantity = quantity + trade.Quantity;
                    averageCost = (quantity * averageCost + trade.Quantity * trade.Price + trade.Fees) / newQuantity;
                    quantity = newQuantity;
                    break;

                case TradeSide.Sell:
                    if (trade.Quantity > quantity)
                    {
                        throw new DomainValidationException(
                            $"Sell of {trade.Quantity} {symbol} at {trade.ExecutedAtUtc:O} exceeds the open position of {quantity}.");
                    }

                    realized += (trade.Price - averageCost) * trade.Quantity - trade.Fees;
                    quantity -= trade.Quantity;
                    if (quantity == 0)
                    {
                        averageCost = 0;
                    }

                    break;

                default:
                    throw new DomainValidationException($"Unknown trade side '{trade.Side}'.");
            }
        }

        return new PositionSummary
        {
            InstrumentId = instrumentId,
            Symbol = symbol,
            Quantity = quantity,
            AverageCost = Math.Round(averageCost, Precision),
            MarketPrice = marketPrice,
            RealizedPnl = Math.Round(realized, Precision),
            TotalFees = fees,
            TradeCount = count
        };
    }

    /// <summary>Throws if replaying the trades would ever sell more than is held.</summary>
    public static void EnsureConsistent(int instrumentId, string symbol, IEnumerable<Trade> trades)
        => Calculate(instrumentId, symbol, trades, marketPrice: null);

    private static IEnumerable<Trade> Order(IEnumerable<Trade> trades)
        => trades.OrderBy(t => t.ExecutedAtUtc).ThenBy(t => t.Id);

    private static void ValidateTrade(Trade trade)
    {
        if (trade.Quantity <= 0)
        {
            throw new DomainValidationException("Trade quantity must be greater than zero.");
        }

        if (trade.Price <= 0)
        {
            throw new DomainValidationException("Trade price must be greater than zero.");
        }

        if (trade.Fees < 0)
        {
            throw new DomainValidationException("Trade fees cannot be negative.");
        }
    }
}
