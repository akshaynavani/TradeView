using Microsoft.Extensions.Options;
using TradeView.Core.Domain;
using TradeView.Infrastructure.MarketData;
using TradeView.Tests.TestSupport;

namespace TradeView.Tests.Unit;

public class SimulatedMarketDataFeedTests
{
    private static readonly Instrument Aapl = new() { Id = 1, Symbol = "AAPL", AssetClass = AssetClass.Equity, SeedPrice = 190m };
    private static readonly Instrument Eur = new() { Id = 2, Symbol = "EUR-USD", AssetClass = AssetClass.Fx, SeedPrice = 1.085m };

    private static readonly IReadOnlyDictionary<int, decimal> NoHistory = new Dictionary<int, decimal>();

    private static SimulatedMarketDataFeed CreateFeed(double volatility = 0.5, int? seed = 42)
        => new(Options.Create(new MarketDataOptions { VolatilityPercent = volatility, RandomSeed = seed }), new FixedClock());

    [Fact]
    public async Task Same_seed_produces_identical_price_paths()
    {
        var a = CreateFeed(seed: 7);
        var b = CreateFeed(seed: 7);

        for (var i = 0; i < 20; i++)
        {
            var qa = await a.GetQuotesAsync(new[] { Aapl, Eur }, NoHistory);
            var qb = await b.GetQuotesAsync(new[] { Aapl, Eur }, NoHistory);
            Assert.Equal(qa.Select(q => q.Price), qb.Select(q => q.Price));
        }
    }

    [Fact]
    public async Task Zero_volatility_starts_from_seed_price()
    {
        var feed = CreateFeed(volatility: 0);

        var quotes = await feed.GetQuotesAsync(new[] { Aapl }, NoHistory);

        Assert.Equal(190m, Assert.Single(quotes).Price);
    }

    [Fact]
    public async Task Resumes_from_last_persisted_price_after_restart()
    {
        var feed = CreateFeed(volatility: 0);
        var persisted = new Dictionary<int, decimal> { [Aapl.Id] = 201.37m };

        var quotes = await feed.GetQuotesAsync(new[] { Aapl }, persisted);

        Assert.Equal(201.37m, Assert.Single(quotes).Price);
    }

    [Fact]
    public async Task Prices_stay_positive_and_rounded_per_asset_class()
    {
        var feed = CreateFeed(volatility: 5);

        for (var i = 0; i < 200; i++)
        {
            var quotes = await feed.GetQuotesAsync(new[] { Aapl, Eur }, NoHistory);
            var equity = quotes.Single(q => q.InstrumentId == Aapl.Id).Price;
            var fx = quotes.Single(q => q.InstrumentId == Eur.Id).Price;

            Assert.True(equity > 0 && fx > 0);
            Assert.Equal(equity, Math.Round(equity, 2));
            Assert.Equal(fx, Math.Round(fx, 5));
        }
    }

    [Fact]
    public async Task Quotes_are_stamped_with_the_clock_time()
    {
        var clock = new FixedClock();
        var feed = new SimulatedMarketDataFeed(Options.Create(new MarketDataOptions { RandomSeed = 1 }), clock);

        var quote = Assert.Single(await feed.GetQuotesAsync(new[] { Aapl }, NoHistory));

        Assert.Equal(clock.UtcNow, quote.TimestampUtc);
        Assert.True(quote.Volume > 0);
    }
}
