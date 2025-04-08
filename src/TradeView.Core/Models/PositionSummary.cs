namespace TradeView.Core.Models;

/// <summary>Position and P&amp;L for a single instrument under the average-cost method.</summary>
public sealed record PositionSummary
{
    public required int InstrumentId { get; init; }

    public required string Symbol { get; init; }

    /// <summary>Open quantity (long-only, never negative).</summary>
    public decimal Quantity { get; init; }

    /// <summary>Average cost per unit of the open quantity, including buy-side fees.</summary>
    public decimal AverageCost { get; init; }

    public decimal CostBasis => Quantity * AverageCost;

    public decimal? MarketPrice { get; init; }

    public decimal? MarketValue => MarketPrice.HasValue ? Quantity * MarketPrice.Value : null;

    public decimal RealizedPnl { get; init; }

    public decimal? UnrealizedPnl => MarketValue.HasValue ? MarketValue.Value - CostBasis : null;

    public decimal? UnrealizedPnlPercent =>
        UnrealizedPnl.HasValue && CostBasis != 0 ? Math.Round(UnrealizedPnl.Value / CostBasis * 100m, 4) : null;

    public decimal TotalPnl => RealizedPnl + (UnrealizedPnl ?? 0m);

    public decimal TotalFees { get; init; }

    public int TradeCount { get; init; }

    public bool IsOpen => Quantity > 0;
}
