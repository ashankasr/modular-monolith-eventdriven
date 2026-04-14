using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ModularMonolithEventDriven.Modules.Orders.Domain;

namespace ModularMonolithEventDriven.Modules.Orders.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.CustomerId).IsRequired().HasMaxLength(100);
        builder.Property(o => o.CustomerEmail).IsRequired().HasMaxLength(200);
        builder.Property(o => o.Status).HasConversion<string>().IsRequired();
        builder.Property(o => o.TotalAmount).HasPrecision(18, 2);
        builder.Property(o => o.FailureReason).HasMaxLength(500);
        builder.HasMany(o => o.Items)
               .WithOne()
               .HasForeignKey(i => i.OrderId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
