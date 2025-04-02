using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeView.Core.Domain;

namespace TradeView.Infrastructure.Persistence.Configurations;

internal sealed class PriceAlertConfiguration : IEntityTypeConfiguration<PriceAlert>
{
    public void Configure(EntityTypeBuilder<PriceAlert> builder)
    {
        builder.ToTable("PriceAlerts");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Condition).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Threshold).HasPrecision(18, 6);
        builder.Property(a => a.ReferencePrice).HasPrecision(18, 6);
        builder.Property(a => a.Note).HasMaxLength(500);

        builder.HasIndex(a => new { a.InstrumentId, a.IsActive });

        builder.HasOne(a => a.Instrument)
            .WithMany()
            .HasForeignKey(a => a.InstrumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
