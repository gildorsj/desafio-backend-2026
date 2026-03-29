using BancaPlataforma.Infrastructure.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BancaPlataforma.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Tabela)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.EntidadeId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Operacao)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(a => a.ValoresAntigos)
            .HasColumnType("jsonb");

        builder.Property(a => a.ValoresNovos)
            .HasColumnType("jsonb");

        builder.Property(a => a.OcorridoEm)
            .IsRequired();

        builder.HasIndex(a => a.EntidadeId);
        builder.HasIndex(a => a.OcorridoEm);
    }
}
