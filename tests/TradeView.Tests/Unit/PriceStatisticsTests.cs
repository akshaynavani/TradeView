using TradeView.Core.Domain;
using TradeView.Core.Services;

namespace TradeView.Tests.Unit;

public class PriceStatisticsTests
{
    private static readonly DateTime T0 = new(2024, 6, 3, 14, 0, 0, DateTimeKind.Utc);

    private static PriceTick Tick(long id, decimal price, decimal volume, int second) => new()
    {
        Id = id,
        InstrumentId = 1,
        Price = price,
        Volume = volume,
        TimestampUtc = T0.AddSeconds(second)
    };

    [Fact]
    public void Summarize_computes_ohlc_change_and_vwap()
    {
        // Deliberately out of order: open/close are decided by timestamp.
        var ticks = new[]
        {
            Tick(3, 9m, 1m, 20),
            Tick(1, 10m, 1m, 0),
            Tick(4, 11m, 2m, 30),
            Tick(2, 12m, 1m, 10)
        };

        var summary = PriceStatistics.Summarize("AAPL", T0, T0.AddMinutes(1), ticks)!;

        Assert.Equal(4, summary.TickCount);
        Assert.Equal(10m, summary.Open);
        Assert.Equal(12m, summary.High);
        Assert.Equal(9m, summary.Low);
        Assert.Equal(11m, summary.Close);
        Assert.Equal(1m, summary.Change);
        Assert.Equal(10m, summary.ChangePercent);
        Assert.Equal(5m, summary.TotalVolume);
        Assert.Equal(10.6m, summary.Vwap); // (9 + 10 + 22 + 12) / 5
    }

    [Fact]
    public void Summarize_falls_back_to_mean_price_when_volume_is_zero()
    {
        var ticks = new[] { Tick(1, 10m, 0m, 0), Tick(2, 20m, 0m, 1) };

        var summary = PriceStatistics.Summarize("AAPL", T0, T0.AddMinutes(1), ticks)!;

        Assert.Equal(15m, summary.Vwap);
    }

    [Fact]
    public void Summarize_returns_null_for_empty_window()
    {
        Assert.Null(PriceStatistics.Summarize("AAPL", T0, T0.AddMinutes(1), Array.Empty<PriceTick>()));
    }
}
