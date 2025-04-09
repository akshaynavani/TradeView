using System.ComponentModel.DataAnnotations;
using TradeView.Core.Domain;
using TradeView.Core.Models;

namespace TradeView.Api.Contracts;

public sealed class RecordTradeRequest
{
    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string Symbol { get; init; } = string.Empty;

    [Required]
    public TradeSide? Side { get; init; }

    [Range(typeof(decimal), "0.00000001", "1000000000")]
    public decimal Quantity { get; init; }

    /// <summary>Fill price. Omit to execute at the latest market quote.</summary>
    [Range(typeof(decimal), "0.00000001", "1000000000")]
    public decimal? Price { get; init; }

    [Range(typeof(decimal), "0", "1000000000")]
    public decimal Fees { get; init; }

    /// <summary>Execution time (UTC). Defaults to now.</summary>
    public DateTime? ExecutedAtUtc { get; init; }

    [StringLength(500)]
    public string? Notes { get; init; }
}

public sealed record TradeResponse(
    long Id,
    string Symbol,
    TradeSide Side,
    decimal Quantity,
    decimal Price,
    decimal Fees,
    decimal GrossValue,
    DateTime ExecutedAtUtc,
    string? Notes)
{
    public static TradeResponse From(Trade t) => new(
        t.Id,
        t.Instrument?.Symbol ?? string.Empty,
        t.Side,
        t.Quantity,
        t.Price,
        t.Fees,
        t.Quantity * t.Price,
        t.ExecutedAtUtc,
        t.Notes);
}

public sealed record PositionResponse(
    string Symbol,
    decimal Quantity,
    decimal AverageCost,
    decimal CostBasis,
    decimal? MarketPrice,
    decimal? MarketValue,
    decimal RealizedPnl,
    decimal? UnrealizedPnl,
    decimal? UnrealizedPnlPercent,
    decimal TotalPnl,
    decimal TotalFees,
    int TradeCount)
{
    public static PositionResponse From(PositionSummary p) => new(
        p.Symbol,
        p.Quantity,
        p.AverageCost,
        Round(p.CostBasis),
        p.MarketPrice,
        p.MarketValue.HasValue ? Round(p.MarketValue.Value) : null,
        Round(p.RealizedPnl),
        p.UnrealizedPnl.HasValue ? Round(p.UnrealizedPnl.Value) : null,
        p.UnrealizedPnlPercent,
        Round(p.TotalPnl),
        p.TotalFees,
        p.TradeCount);

    internal static decimal Round(decimal value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);
}

public sealed record PortfolioResponse(
    DateTime AsOfUtc,
    decimal TotalCostBasis,
    decimal TotalMarketValue,
    decimal TotalRealizedPnl,
    decimal TotalUnrealizedPnl,
    decimal TotalPnl,
    decimal TotalFees,
    IReadOnlyList<PositionResponse> Positions)
{
    public static PortfolioResponse From(PortfolioSummary s) => new(
        s.AsOfUtc,
        PositionResponse.Round(s.TotalCostBasis),
        PositionResponse.Round(s.TotalMarketValue),
        PositionResponse.Round(s.TotalRealizedPnl),
        PositionResponse.Round(s.TotalUnrealizedPnl),
        PositionResponse.Round(s.TotalPnl),
        s.TotalFees,
        s.Positions.Select(PositionResponse.From).ToList());
}
