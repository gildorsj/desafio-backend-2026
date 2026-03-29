using BancaPlataforma.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BancaPlataforma.Infrastructure.Persistence.Configurations;

public sealed class TransacaoConfiguration : IEntityTypeConfiguration<Transacao>
{
    public void Configure(EntityTypeBuilder<Transacao> builder)
    {
        builder.ToTable("transacoes");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");

        builder.Property(t => t.ContaId).HasColumnName("conta_id").IsRequired();

        builder.ComplexProperty(t => t.Valor, v =>
        {
            v.Property(x => x.Valor)
                .HasColumnName("valor")
                .HasPrecision(18, 2)
                .IsRequired();
            v.Property(x => x.Moeda)
                .HasColumnName("moeda")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(t => t.Tipo)
            .HasColumnName("tipo")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Descricao)
            .HasColumnName("descricao")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(t => t.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(t => t.IdempotencyKey).IsUnique();

        builder.Property(t => t.ContaDestinoId).HasColumnName("conta_destino_id");
        builder.Property(t => t.CriadoEm).HasColumnName("criado_em").IsRequired();
    }
}
