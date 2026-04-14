using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ModularMonolithEventDriven.Modules.Inventory.Domain;

namespace ModularMonolithEventDriven.Modules.Inventory.Infrastructure.Persistence.Configurations;

public sealed class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable("StockReservations");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Status).HasConversion<string>();
        builder.OwnsMany(r => r.Items, ib =>
        {
            ib.ToTable("StockReservationItems");
            ib.WithOwner().HasForeignKey("ReservationId");
            ib.Property(i => i.ProductId);
            ib.Property(i => i.Quantity);
        });
    }
}
