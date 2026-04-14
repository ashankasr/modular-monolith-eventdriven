using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ModularMonolithEventDriven.Common.Infrastructure.Outbox;

namespace ModularMonolithEventDriven.Common.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Type).IsRequired().HasMaxLength(500);
        builder.Property(m => m.Content).IsRequired();
        builder.HasIndex(m => m.ProcessedOnUtc);
    }
}
