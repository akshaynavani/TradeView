using Microsoft.EntityFrameworkCore;
using TradeView.Core.Abstractions;
using TradeView.Core.Domain;
using TradeView.Core.Exceptions;
using TradeView.Core.Services;
using TradeView.Infrastructure.Persistence;
using TradeView.Infrastructure.Repositories;
using TradeView.Tests.TestSupport;

namespace TradeView.Tests.Integration;

public sealed class AlertServiceTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();
    private readonly TradeViewDbContext _db;
    private readonly FixedClock _clock = new();
    private readonly RecordingNotifier _notifier = new();
    private readonly AlertService _service;

    public AlertServiceTests()
    {
        _db = _database.CreateContext();
        _service = new AlertService(
            new AlertRepository(_db),
            new InstrumentRepository(_db),
            new PriceTickRepository(_db),
            _notifier,
            _clock);
    }

    [Fact]
    public async Task Create_captures_the_current_price_as_reference()
    {
        var aapl = await SqliteTestDatabase.AddInstrumentAsync(_db, "AAPL");
        await SqliteTestDatabase.AddTickAsync(_db, aapl, 150m, _clock.UtcNow);

        var alert = await _service.CreateAsync("AAPL", AlertCondition.PercentMove, 2m, false, 60, null, default);

        Assert.Equal(150m, alert.ReferencePrice);
        Assert.True(alert.IsActive);
    }

    [Fact]
    public async Task PercentMove_alert_requires_an_existing_quote()
    {
        await SqliteTestDatabase.AddInstrumentAsync(_db, "AAPL");

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _service.CreateAsync("AAPL", AlertCondition.PercentMove, 2m, false, 60, null, default));
    }

    [Theory]
    [InlineData(0, 60)]
    [InlineData(-5, 60)]
    [InlineData(10, -1)]
    public async Task Invalid_rules_are_rejected(int threshold, int cooldown)
    {
        await SqliteTestDatabase.AddInstrumentAsync(_db, "AAPL");

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _service.CreateAsync("AAPL", AlertCondition.PriceAbove, threshold, false, cooldown, null, default));
    }

    [Fact]
    public async Task Evaluate_fires_persists_and_notifies_one_shot_alert()
    {
        var aapl = await SqliteTestDatabase.AddInstrumentAsync(_db, "AAPL");
        await SqliteTestDatabase.AddTickAsync(_db, aapl, 150m, _clock.UtcNow);
        var alert = await _service.CreateAsync("AAPL", AlertCondition.PriceAbove, 155m, false, 0, "breakout", default);

        _clock.Advance(TimeSpan.FromSeconds(2));
        var quiet = await SqliteTestDatabase.AddTickAsync(_db, aapl, 154m, _clock.UtcNow);
        Assert.Empty(await _service.EvaluateAsync(new[] { quiet }, default));

        _clock.Advance(TimeSpan.FromSeconds(2));
        var breakout = await SqliteTestDatabase.AddTickAsync(_db, aapl, 156.4m, _clock.UtcNow);
        var fired = await _service.EvaluateAsync(new[] { breakout }, default);

        var trigger = Assert.Single(fired);
        Assert.Equal(156.4m, trigger.TriggeredPrice);
        Assert.Single(_notifier.Received);

        var stored = await _db.PriceAlerts.AsNoTracking().SingleAsync(a => a.Id == alert.Id);
        Assert.False(stored.IsActive);
        Assert.Equal(_clock.UtcNow, stored.LastTriggeredAtUtc);

        var history = await _service.GetTriggersAsync(alert.Id, null, null, 10, default);
        Assert.Single(history);

        // Deactivated alerts do not fire again.
        _clock.Advance(TimeSpan.FromSeconds(2));
        var higher = await SqliteTestDatabase.AddTickAsync(_db, aapl, 160m, _clock.UtcNow);
        Assert.Empty(await _service.EvaluateAsync(new[] { higher }, default));
    }

    [Fact]
    public async Task Repeating_alert_fires_again_only_after_cooldown()
    {
        var btc = await SqliteTestDatabase.AddInstrumentAsync(_db, "BTC-USD", 60000m, AssetClass.Crypto);
        await SqliteTestDatabase.AddTickAsync(_db, btc, 60000m, _clock.UtcNow);
        await _service.CreateAsync("BTC-USD", AlertCondition.PriceBelow, 59000m, true, 120, null, default);

        async Task<int> TickAt(decimal price)
        {
            var tick = await SqliteTestDatabase.AddTickAsync(_db, btc, price, _clock.UtcNow);
            return (await _service.EvaluateAsync(new[] { tick }, default)).Count;
        }

        _clock.Advance(TimeSpan.FromSeconds(5));
        Assert.Equal(1, await TickAt(58900m));

        _clock.Advance(TimeSpan.FromSeconds(60));
        Assert.Equal(0, await TickAt(58800m));

        _clock.Advance(TimeSpan.FromSeconds(60));
        Assert.Equal(1, await TickAt(58700m));

        Assert.Equal(2, (await _service.GetTriggersAsync(null, "BTC-USD", null, 10, default)).Count);
    }

    [Fact]
    public async Task Only_the_latest_tick_per_instrument_is_evaluated()
    {
        var aapl = await SqliteTestDatabase.AddInstrumentAsync(_db, "AAPL");
        await SqliteTestDatabase.AddTickAsync(_db, aapl, 150m, _clock.UtcNow);
        await _service.CreateAsync("AAPL", AlertCondition.PriceAbove, 155m, false, 0, null, default);

        var spike = await SqliteTestDatabase.AddTickAsync(_db, aapl, 157m, _clock.UtcNow.AddSeconds(1));
        var settle = await SqliteTestDatabase.AddTickAsync(_db, aapl, 152m, _clock.UtcNow.AddSeconds(2));

        Assert.Empty(await _service.EvaluateAsync(new[] { spike, settle }, default));
    }

    [Fact]
    public async Task Reactivating_an_alert_rebases_reference_price()
    {
        var aapl = await SqliteTestDatabase.AddInstrumentAsync(_db, "AAPL");
        await SqliteTestDatabase.AddTickAsync(_db, aapl, 100m, _clock.UtcNow);
        var alert = await _service.CreateAsync("AAPL", AlertCondition.PercentMove, 5m, false, 0, null, default);

        _clock.Advance(TimeSpan.FromSeconds(1));
        var jump = await SqliteTestDatabase.AddTickAsync(_db, aapl, 106m, _clock.UtcNow);
        Assert.Single(await _service.EvaluateAsync(new[] { jump }, default));

        var updated = await _service.UpdateAsync(alert.Id, null, isActive: true, null, null, null, default);

        Assert.True(updated.IsActive);
        Assert.Equal(106m, updated.ReferencePrice);
        Assert.Null(updated.LastTriggeredAtUtc);
    }

    [Fact]
    public async Task Missing_alert_raises_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAsync(999, default));
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(999, default));
    }

    public void Dispose()
    {
        _db.Dispose();
        _database.Dispose();
    }

    private sealed class RecordingNotifier : IAlertNotifier
    {
        public List<AlertTrigger> Received { get; } = new();

        public Task NotifyAsync(IReadOnlyList<AlertTrigger> triggers, CancellationToken cancellationToken = default)
        {
            Received.AddRange(triggers);
            return Task.CompletedTask;
        }
    }
}
