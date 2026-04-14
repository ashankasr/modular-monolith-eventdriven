using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ModularMonolithEventDriven.Modules.Payments.Domain;

namespace ModularMonolithEventDriven.Modules.Payments.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.CustomerId).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Status).HasConversion<string>();
        builder.HasIndex(p => p.OrderId).IsUnique();
    }
}
