using BancaPlataforma.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BancaPlataforma.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Tipo).IsRequired().HasMaxLength(500);
        builder.Property(o => o.Payload).IsRequired();
        builder.Property(o => o.CriadoEm).IsRequired();
        builder.HasIndex(o => o.ProcessadoEm);
    }
}
