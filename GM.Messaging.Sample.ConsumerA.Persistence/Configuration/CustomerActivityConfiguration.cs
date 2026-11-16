using GM.Messaging.Sample.ConsumerA.Domain.BoundedContext.ActivityBoundedContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GM.Messaging.Sample.ConsumerA.Persistence.Configuration;

public class CustomerActivityConfiguration : IEntityTypeConfiguration<CustomerActivity>
{
    public void Configure(EntityTypeBuilder<CustomerActivity> builder)
    {
        builder.ToTable("customer_activities");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Kind).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Reference).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        // A second activity for the same event would mean the inbox let a duplicate through.
        builder.HasIndex(x => x.EventId).IsUnique();
        builder.HasIndex(x => x.CustomerId);
    }
}
