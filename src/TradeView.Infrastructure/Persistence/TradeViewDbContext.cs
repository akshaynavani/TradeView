using Microsoft.EntityFrameworkCore;
using TradeView.Core.Domain;

namespace TradeView.Infrastructure.Persistence;

public class TradeViewDbContext : DbContext
{
    public TradeViewDbContext(DbContextOptions<TradeViewDbContext> options)
        : base(options)
    {
    }

    public DbSet<Instrument> Instruments => Set<Instrument>();

    public DbSet<PriceTick> PriceTicks => Set<PriceTick>();

    public DbSet<Trade> Trades => Set<Trade>();

    public DbSet<PriceAlert> PriceAlerts => Set<PriceAlert>();

    public DbSet<AlertTrigger> AlertTriggers => Set<AlertTrigger>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TradeViewDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
