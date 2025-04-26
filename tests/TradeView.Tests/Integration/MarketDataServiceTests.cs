using Microsoft.Extensions.Options;
using TradeView.Core.Exceptions;
using TradeView.Core.Services;
using TradeView.Infrastructure.MarketData;
using TradeView.Infrastructure.Persistence;
using TradeView.Infrastructure.Repositories;
using TradeView.Tests.TestSupport;

namespace TradeView.Tests.Integration;

public sealed class MarketDataServiceTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();
    private readonly TradeViewDbContext _db;
    private readonly FixedClock _clock = new();
    private readonly MarketDataService _service;

    public MarketDataServiceTests()
    {
        _db = _database.CreateContext();
        var feed = new SimulatedMarketDataFeed(
            Options.Create(new MarketDataOptions { VolatilityPercent = 1, RandomSeed = 3 }),
            _clock);

        _service = new MarketDataService(
            new InstrumentRepository(_db),
            new PriceTickRepository(_db),
            feed,
            _clock);
    }

    [Fact]
    public async Task Ingest_writes_one_tick_per_active_instrument()
    {
        await SqliteTestDatabase.AddInstrumentAsync(_db, "AAPL");
        await SqliteTestDatabase.AddInstrumentAsync(_db, "MSFT");
        var paused = await SqliteTestDatabase.AddInstrumentAsync(_db, "NVDA");
        paused.IsActive = false;
        await _db.SaveChangesAsync();

        var ticks = await _service.IngestAsync();

        Assert.Equal(2, ticks.Count);
        Assert.DoesNotContain(ticks, t => t.InstrumentId == paused.Id);
        Assert.Equal(2, _db.PriceTicks.Count());
    }

    [Fact]
    public async Task Latest_quote_and_history_reflect_ingested_ticks()
    {
        await SqliteTestDatabase.AddInstrumentAsync(_db, "AAPL");

        for (var i = 0; i < 5; i++)
        {
            _clock.Advance(TimeSpan.FromSeconds(2));
            await _service.IngestAsync();
        }

        var (_, latest) = await _service.GetLatestQuoteAsync("aapl");
        var history = await _service.GetHistoryAsync("AAPL", null, null, limit: 3);

        Assert.Equal(_clock.UtcNow, latest.TimestampUtc);
        Assert.Equal(3, history.Count);
        Assert.True(history[0].TimestampUtc < history[^1].TimestampUtc);
        Assert.Equal(latest.Price, history[^1].Price);

        var summary = await _service.GetSummaryAsync("AAPL", null, null);
        Assert.Equal(5, summary.TickCount);
        Assert.Equal(latest.Price, summary.Close);
    }

    [Fact]
    public async Task Invalid_history_requests_are_rejected()
    {
        await SqliteTestDatabase.AddInstrumentAsync(_db, "AAPL");

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _service.GetHistoryAsync("AAPL", _clock.UtcNow, _clock.UtcNow.AddHours(-1), 10));
        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _service.GetHistoryAsync("AAPL", null, null, 0));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GetLatestQuoteAsync("AAPL"));
    }

    [Fact]
    public async Task Prune_removes_ticks_older_than_retention()
    {
        var aapl = await SqliteTestDatabase.AddInstrumentAsync(_db, "AAPL");
        await SqliteTestDatabase.AddTickAsync(_db, aapl, 100m, _clock.UtcNow.AddHours(-30));
        await SqliteTestDatabase.AddTickAsync(_db, aapl, 101m, _clock.UtcNow.AddHours(-1));

        var removed = await _service.PruneAsync(TimeSpan.FromHours(24));

        Assert.Equal(1, removed);
    }

    public void Dispose()
    {
        _db.Dispose();
        _database.Dispose();
    }
}
