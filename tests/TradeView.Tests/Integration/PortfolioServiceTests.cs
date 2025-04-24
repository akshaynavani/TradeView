using TradeView.Core.Domain;
using TradeView.Core.Exceptions;
using TradeView.Core.Services;
using TradeView.Infrastructure.Persistence;
using TradeView.Infrastructure.Repositories;
using TradeView.Tests.TestSupport;

namespace TradeView.Tests.Integration;

public sealed class PortfolioServiceTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();
    private readonly TradeViewDbContext _db;
    private readonly FixedClock _clock = new();
    private readonly PortfolioService _service;

    public PortfolioServiceTests()
    {
        _db = _database.CreateContext();
        _service = new PortfolioService(
            new InstrumentRepository(_db),
            new TradeRepository(_db),
            new PriceTickRepository(_db),
            _clock);
    }

    [Fact]
    public async Task Trade_without_price_fills_at_latest_quote()
    {
        var aapl = await SqliteTestDatabase.AddInstrumentAsync(_db, "AAPL");
        await SqliteTestDatabase.AddTickAsync(_db, aapl, 150m, _clock.UtcNow.AddSeconds(-4));
        await SqliteTestDatabase.AddTickAsync(_db, aapl, 151.25m, _clock.UtcNow.AddSeconds(-2));

        var trade = await _service.RecordTradeAsync("aapl", TradeSide.Buy, 10m, null, 1m, null, " first lot ", default);

        Assert.True(trade.Id > 0);
        Assert.Equal(151.25m, trade.Price);
        Assert.Equal(_clock.UtcNow, trade.ExecutedAtUtc);
        Assert.Equal("first lot", trade.Notes);
    }

    [Fact]
    public async Task Trade_without_price_or_quote_is_rejected()
    {
        await SqliteTestDatabase.AddInstrumentAsync(_db, "MSFT");

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _service.RecordTradeAsync("MSFT", TradeSide.Buy, 1m, null, 0m, null, null, default));
    }

    [Fact]
    public async Task Oversell_is_rejected_and_not_persisted()
    {
        await SqliteTestDatabase.AddInstrumentAsync(_db, "AAPL");
        await _service.RecordTradeAsync("AAPL", TradeSide.Buy, 5m, 100m, 0m, null, null, default);

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _service.RecordTradeAsync("AAPL", TradeSide.Sell, 6m, 110m, 0m, null, null, default));

        Assert.Single(await _service.ListTradesAsync("AAPL", null, null, default));
    }

    [Fact]
    public async Task Backdated_sell_before_the_buy_is_rejected()
    {
        await SqliteTestDatabase.AddInstrumentAsync(_db, "AAPL");
        await _service.RecordTradeAsync("AAPL", TradeSide.Buy, 5m, 100m, 0m, _clock.UtcNow.AddHours(-1), null, default);

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _service.RecordTradeAsync("AAPL", TradeSide.Sell, 5m, 100m, 0m, _clock.UtcNow.AddHours(-2), null, default));
    }

    [Fact]
    public async Task Future_dated_trades_are_rejected()
    {
        await SqliteTestDatabase.AddInstrumentAsync(_db, "AAPL");

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _service.RecordTradeAsync("AAPL", TradeSide.Buy, 1m, 100m, 0m, _clock.UtcNow.AddHours(1), null, default));
    }

    [Fact]
    public async Task Unknown_symbol_raises_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.RecordTradeAsync("NOPE", TradeSide.Buy, 1m, 1m, 0m, null, null, default));
    }

    [Fact]
    public async Task Position_is_marked_to_the_latest_quote()
    {
        var aapl = await SqliteTestDatabase.AddInstrumentAsync(_db, "AAPL");
        await _service.RecordTradeAsync("AAPL", TradeSide.Buy, 10m, 100m, 0m, _clock.UtcNow.AddMinutes(-30), null, default);
        await _service.RecordTradeAsync("AAPL", TradeSide.Sell, 4m, 120m, 0m, _clock.UtcNow.AddMinutes(-10), null, default);
        await SqliteTestDatabase.AddTickAsync(_db, aapl, 110m, _clock.UtcNow);

        var position = await _service.GetPositionAsync("AAPL", default);

        Assert.Equal(6m, position.Quantity);
        Assert.Equal(110m, position.MarketPrice);
        Assert.Equal(80m, position.RealizedPnl);
        Assert.Equal(60m, position.UnrealizedPnl);
        Assert.Equal(140m, position.TotalPnl);
    }

    [Fact]
    public async Task Portfolio_aggregates_open_positions_and_optionally_closed_ones()
    {
        var aapl = await SqliteTestDatabase.AddInstrumentAsync(_db, "AAPL");
        var btc = await SqliteTestDatabase.AddInstrumentAsync(_db, "BTC-USD", 60000m, AssetClass.Crypto);
        await SqliteTestDatabase.AddInstrumentAsync(_db, "NVDA");

        var t = _clock.UtcNow.AddHours(-1);
        await _service.RecordTradeAsync("AAPL", TradeSide.Buy, 10m, 100m, 0m, t, null, default);
        await _service.RecordTradeAsync("BTC-USD", TradeSide.Buy, 0.5m, 60000m, 0m, t, null, default);
        await _service.RecordTradeAsync("NVDA", TradeSide.Buy, 2m, 100m, 0m, t, null, default);
        await _service.RecordTradeAsync("NVDA", TradeSide.Sell, 2m, 125m, 0m, t.AddMinutes(5), null, default);

        await SqliteTestDatabase.AddTickAsync(_db, aapl, 105m, _clock.UtcNow);
        await SqliteTestDatabase.AddTickAsync(_db, btc, 58000m, _clock.UtcNow);

        var open = await _service.GetPortfolioAsync(includeClosed: false, default);
        Assert.Equal(new[] { "AAPL", "BTC-USD" }, open.Positions.Select(p => p.Symbol));
        Assert.Equal(-950m, open.TotalUnrealizedPnl); // +50 AAPL, -1000 BTC
        Assert.Equal(0m, open.TotalRealizedPnl);

        var all = await _service.GetPortfolioAsync(includeClosed: true, default);
        Assert.Equal(3, all.Positions.Count);
        Assert.Equal(50m, all.TotalRealizedPnl);
        Assert.Equal(-900m, all.TotalPnl);
    }

    [Fact]
    public async Task Deleting_a_buy_that_covers_a_later_sell_is_rejected()
    {
        await SqliteTestDatabase.AddInstrumentAsync(_db, "AAPL");
        var buy = await _service.RecordTradeAsync("AAPL", TradeSide.Buy, 5m, 100m, 0m, _clock.UtcNow.AddMinutes(-10), null, default);
        var sell = await _service.RecordTradeAsync("AAPL", TradeSide.Sell, 5m, 110m, 0m, _clock.UtcNow.AddMinutes(-5), null, default);

        await Assert.ThrowsAsync<DomainValidationException>(() => _service.DeleteTradeAsync(buy.Id, default));

        await _service.DeleteTradeAsync(sell.Id, default);
        await _service.DeleteTradeAsync(buy.Id, default);
        Assert.Empty(await _service.ListTradesAsync(null, null, null, default));
    }

    public void Dispose()
    {
        _db.Dispose();
        _database.Dispose();
    }
}
