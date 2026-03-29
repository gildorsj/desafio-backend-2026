using BancaPlataforma.Domain.Aggregates;
using BancaPlataforma.Domain.Enums;
using BancaPlataforma.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BancaPlataforma.Infrastructure.Persistence.Configurations;

public sealed class ContaConfiguration : IEntityTypeConfiguration<Conta>
{
    public void Configure(EntityTypeBuilder<Conta> builder)
    {
        builder.ToTable("contas");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");

        // Value Object: CNPJ
        builder.OwnsOne(c => c.Cnpj, cnpj =>
        {
            cnpj.Property(c => c.Valor)
                .HasColumnName("cnpj")
                .HasMaxLength(14)
                .IsRequired();
            cnpj.HasIndex(c => c.Valor).IsUnique();
        });

        // Value Object: Saldo
        builder.OwnsOne(c => c.Saldo, saldo =>
        {
            saldo.Property(s => s.Valor)
                .HasColumnName("saldo")
                .HasPrecision(18, 2)
                .IsRequired();
            saldo.Property(s => s.Moeda)
                .HasColumnName("moeda")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(c => c.RazaoSocial)
            .HasColumnName("razao_social")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(c => c.Agencia)
            .HasColumnName("agencia")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(c => c.ImagemDocumentoBase64)
            .HasColumnName("imagem_documento_base64")
            .IsRequired();

        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        builder.Property(c => c.AtualizadoEm)
            .HasColumnName("atualizado_em");

        builder.HasMany(c => c.Transacoes)
            .WithOne()
            .HasForeignKey(t => t.ContaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}