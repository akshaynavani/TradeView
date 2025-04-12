using TradeView.Core.Domain;

namespace TradeView.Tests.TestSupport;

public static class TradeBuilder
{
    private static readonly DateTime Start = new(2024, 6, 3, 9, 30, 0, DateTimeKind.Utc);
    private static long _nextId = 1;

    public static Trade Buy(decimal quantity, decimal price, decimal fees = 0m, int minute = 0, int instrumentId = 1)
        => Create(TradeSide.Buy, quantity, price, fees, minute, instrumentId);

    public static Trade Sell(decimal quantity, decimal price, decimal fees = 0m, int minute = 0, int instrumentId = 1)
        => Create(TradeSide.Sell, quantity, price, fees, minute, instrumentId);

    private static Trade Create(TradeSide side, decimal quantity, decimal price, decimal fees, int minute, int instrumentId)
        => new()
        {
            Id = Interlocked.Increment(ref _nextId),
            InstrumentId = instrumentId,
            Side = side,
            Quantity = quantity,
            Price = price,
            Fees = fees,
            ExecutedAtUtc = Start.AddMinutes(minute)
        };
}
