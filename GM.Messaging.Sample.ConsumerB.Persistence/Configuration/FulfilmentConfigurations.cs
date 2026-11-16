using GM.Messaging.Sample.ConsumerB.Domain.BoundedContext.FulfilmentBoundedContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GM.Messaging.Sample.ConsumerB.Persistence.Configuration;

public class OrderTimelineEntryConfiguration : IEntityTypeConfiguration<OrderTimelineEntry>
{
    public void Configure(EntityTypeBuilder<OrderTimelineEntry> builder)
    {
        builder.ToTable("order_timeline");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Kind).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.HasIndex(x => x.EventId).IsUnique();
        builder.HasIndex(x => new { x.OrderId, x.RecordedAtUtc });
    }
}

public class StockLevelConfiguration : IEntityTypeConfiguration<StockLevel>
{
    public void Configure(EntityTypeBuilder<StockLevel> builder)
    {
        builder.ToTable("stock_levels");
        builder.HasKey(x => new { x.ProductId, x.WarehouseCode });
        builder.Property(x => x.WarehouseCode).HasMaxLength(50);
    }
}

public class WelcomeEmailConfiguration : IEntityTypeConfiguration<WelcomeEmail>
{
    public void Configure(EntityTypeBuilder<WelcomeEmail> builder)
    {
        builder.ToTable("welcome_emails");
        builder.HasKey(x => x.EventId);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(320);
    }
}
