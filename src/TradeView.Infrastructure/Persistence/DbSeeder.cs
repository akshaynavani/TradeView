using Microsoft.EntityFrameworkCore;
using TradeView.Core.Abstractions;
using TradeView.Core.Domain;

namespace TradeView.Infrastructure.Persistence;

public static class DbSeeder
{
    private static readonly (string Symbol, string Name, AssetClass AssetClass, string Currency, decimal Price)[] DefaultUniverse =
    {
        ("AAPL", "Apple Inc.", AssetClass.Equity, "USD", 190.00m),
        ("MSFT", "Microsoft Corp.", AssetClass.Equity, "USD", 420.00m),
        ("NVDA", "NVIDIA Corp.", AssetClass.Equity, "USD", 120.00m),
        ("SPY", "SPDR S&P 500 ETF", AssetClass.Index, "USD", 550.00m),
        ("BTC-USD", "Bitcoin", AssetClass.Crypto, "USD", 65000.00m),
        ("ETH-USD", "Ether", AssetClass.Crypto, "USD", 3200.00m),
        ("EUR-USD", "Euro / US Dollar", AssetClass.Fx, "USD", 1.08500m),
        ("XAU-USD", "Gold Spot", AssetClass.Commodity, "USD", 2400.00m)
    };

    public static async Task SeedAsync(TradeViewDbContext db, IClock clock, CancellationToken cancellationToken = default)
    {
        if (await db.Instruments.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = clock.UtcNow;
        db.Instruments.AddRange(DefaultUniverse.Select(i => new Instrument
        {
            Symbol = i.Symbol,
            Name = i.Name,
            AssetClass = i.AssetClass,
            Currency = i.Currency,
            SeedPrice = i.Price,
            IsActive = true,
            CreatedAtUtc = now
        }));

        await db.SaveChangesAsync(cancellationToken);
    }
}
