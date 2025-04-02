using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeView.Core.Domain;

namespace TradeView.Infrastructure.Persistence.Configurations;

internal sealed class AlertTriggerConfiguration : IEntityTypeConfiguration<AlertTrigger>
{
    public void Configure(EntityTypeBuilder<AlertTrigger> builder)
    {
        builder.ToTable("AlertTriggers");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TriggeredPrice).HasPrecision(18, 6);
        builder.Property(t => t.Message).IsRequired().HasMaxLength(500);

        builder.HasIndex(t => t.TriggeredAtUtc);

        builder.HasOne(t => t.PriceAlert)
            .WithMany(a => a.Triggers)
            .HasForeignKey(t => t.PriceAlertId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
