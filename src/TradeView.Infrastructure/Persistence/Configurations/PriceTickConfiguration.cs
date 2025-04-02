using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeView.Core.Domain;

namespace TradeView.Infrastructure.Persistence.Configurations;

internal sealed class PriceTickConfiguration : IEntityTypeConfiguration<PriceTick>
{
    public void Configure(EntityTypeBuilder<PriceTick> builder)
    {
        builder.ToTable("PriceTicks");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Price).HasPrecision(18, 6);
        builder.Property(t => t.Volume).HasPrecision(18, 6);

        // History queries always filter by instrument and time range.
        builder.HasIndex(t => new { t.InstrumentId, t.TimestampUtc });

        builder.HasOne(t => t.Instrument)
            .WithMany(i => i.Ticks)
            .HasForeignKey(t => t.InstrumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
