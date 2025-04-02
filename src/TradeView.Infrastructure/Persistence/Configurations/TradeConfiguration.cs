using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeView.Core.Domain;

namespace TradeView.Infrastructure.Persistence.Configurations;

internal sealed class TradeConfiguration : IEntityTypeConfiguration<Trade>
{
    public void Configure(EntityTypeBuilder<Trade> builder)
    {
        builder.ToTable("Trades");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Side).HasConversion<string>().HasMaxLength(4);
        builder.Property(t => t.Quantity).HasPrecision(18, 6);
        builder.Property(t => t.Price).HasPrecision(18, 6);
        builder.Property(t => t.Fees).HasPrecision(18, 6);
        builder.Property(t => t.Notes).HasMaxLength(500);

        builder.HasIndex(t => new { t.InstrumentId, t.ExecutedAtUtc });

        builder.HasOne(t => t.Instrument)
            .WithMany()
            .HasForeignKey(t => t.InstrumentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
