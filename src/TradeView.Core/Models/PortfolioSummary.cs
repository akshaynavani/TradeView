namespace TradeView.Core.Models;

/// <summary>
/// Aggregate P&amp;L across positions. Values are summed as-is; the API assumes a single
/// reporting currency (every seeded instrument is quoted in USD).
/// </summary>
public sealed record PortfolioSummary(
    DateTime AsOfUtc,
    IReadOnlyList<PositionSummary> Positions)
{
    public decimal TotalCostBasis => Positions.Sum(p => p.CostBasis);

    public decimal TotalMarketValue => Positions.Sum(p => p.MarketValue ?? 0m);

    public decimal TotalRealizedPnl => Positions.Sum(p => p.RealizedPnl);

    public decimal TotalUnrealizedPnl => Positions.Sum(p => p.UnrealizedPnl ?? 0m);

    public decimal TotalPnl => TotalRealizedPnl + TotalUnrealizedPnl;

    public decimal TotalFees => Positions.Sum(p => p.TotalFees);
}
