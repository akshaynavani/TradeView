namespace TradeView.Core.Models;

public sealed record MarketQuote(
    int InstrumentId,
    string Symbol,
    decimal Price,
    decimal Volume,
    DateTime TimestampUtc);
