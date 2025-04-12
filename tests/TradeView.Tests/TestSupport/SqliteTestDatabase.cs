using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TradeView.Core.Domain;
using TradeView.Infrastructure.Persistence;

namespace TradeView.Tests.TestSupport;

/// <summary>
/// In-memory SQLite database shared across contexts for one test. Uses the real provider so
/// queries are translated exactly as in production (unlike the EF InMemory provider).
/// </summary>
public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteTestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public TradeViewDbContext CreateContext()
        => new(new DbContextOptionsBuilder<TradeViewDbContext>().UseSqlite(_connection).Options);

    public static async Task<Instrument> AddInstrumentAsync(
        TradeViewDbContext db,
        string symbol,
        decimal seedPrice = 100m,
        AssetClass assetClass = AssetClass.Equity)
    {
        var instrument = new Instrument
        {
            Symbol = symbol,
            Name = symbol,
            AssetClass = assetClass,
            Currency = "USD",
            SeedPrice = seedPrice,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Instruments.Add(instrument);
        await db.SaveChangesAsync();
        return instrument;
    }

    public static async Task<PriceTick> AddTickAsync(TradeViewDbContext db, Instrument instrument, decimal price, DateTime timestampUtc)
    {
        var tick = new PriceTick
        {
            InstrumentId = instrument.Id,
            Instrument = instrument,
            Price = price,
            Volume = 100m,
            TimestampUtc = timestampUtc
        };
        db.PriceTicks.Add(tick);
        await db.SaveChangesAsync();
        return tick;
    }

    public void Dispose() => _connection.Dispose();
}
