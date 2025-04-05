namespace TradeView.Core.Models;

/// <summary>OHLC-style statistics over a window of ticks.</summary>
public sealed record PriceSummary(
    string Symbol,
    DateTime FromUtc,
    DateTime ToUtc,
    int TickCount,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal Change,
    decimal ChangePercent,
    decimal TotalVolume,
    decimal Vwap);
