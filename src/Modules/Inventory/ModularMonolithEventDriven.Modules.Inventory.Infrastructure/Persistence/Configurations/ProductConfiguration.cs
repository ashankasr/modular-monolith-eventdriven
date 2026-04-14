using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ModularMonolithEventDriven.Modules.Inventory.Domain;

namespace ModularMonolithEventDriven.Modules.Inventory.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Sku).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Price).HasPrecision(18, 2);
        builder.HasIndex(p => p.Sku).IsUnique();
    }
}
