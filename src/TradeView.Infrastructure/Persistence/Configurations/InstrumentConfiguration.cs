using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeView.Core.Domain;

namespace TradeView.Infrastructure.Persistence.Configurations;

internal sealed class InstrumentConfiguration : IEntityTypeConfiguration<Instrument>
{
    public void Configure(EntityTypeBuilder<Instrument> builder)
    {
        builder.ToTable("Instruments");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Symbol).IsRequired().HasMaxLength(20);
        builder.HasIndex(i => i.Symbol).IsUnique();

        builder.Property(i => i.Name).IsRequired().HasMaxLength(200);
        builder.Property(i => i.Currency).IsRequired().HasMaxLength(3);
        builder.Property(i => i.AssetClass).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.SeedPrice).HasPrecision(18, 6);
    }
}
