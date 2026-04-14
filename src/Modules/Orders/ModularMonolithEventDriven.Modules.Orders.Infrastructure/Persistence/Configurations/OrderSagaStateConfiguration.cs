using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ModularMonolithEventDriven.Modules.Orders.Application.Saga;

namespace ModularMonolithEventDriven.Modules.Orders.Infrastructure.Persistence.Configurations;

public sealed class OrderSagaStateConfiguration : IEntityTypeConfiguration<OrderSagaState>
{
    public void Configure(EntityTypeBuilder<OrderSagaState> builder)
    {
        builder.ToTable("OrderSagaState");
        builder.HasKey(s => s.CorrelationId);
        builder.Property(s => s.CurrentState).IsRequired().HasMaxLength(64);
        builder.Property(s => s.CustomerId).IsRequired().HasMaxLength(100);
        builder.Property(s => s.CustomerEmail).IsRequired().HasMaxLength(200);
        builder.Property(s => s.TotalAmount).HasPrecision(18, 2);
        builder.Property(s => s.FailureReason).HasMaxLength(500);
    }
}
