using TradeView.Core.Domain;

namespace TradeView.Api.Contracts;

public sealed record QuoteResponse(
    string Symbol,
    decimal Price,
    decimal Volume,
    DateTime TimestampUtc)
{
    public static QuoteResponse From(Instrument instrument, PriceTick tick) =>
        new(instrument.Symbol, tick.Price, tick.Volume, tick.TimestampUtc);
}

public sealed record TickResponse(decimal Price, decimal Volume, DateTime TimestampUtc)
{
    public static TickResponse From(PriceTick tick) => new(tick.Price, tick.Volume, tick.TimestampUtc);
}

public sealed record PriceHistoryResponse(string Symbol, int Count, IReadOnlyList<TickResponse> Ticks);
