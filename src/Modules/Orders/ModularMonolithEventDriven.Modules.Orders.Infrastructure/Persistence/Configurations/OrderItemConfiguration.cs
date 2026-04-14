using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ModularMonolithEventDriven.Modules.Orders.Domain;

namespace ModularMonolithEventDriven.Modules.Orders.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.ProductName).IsRequired().HasMaxLength(200);
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
    }
}
