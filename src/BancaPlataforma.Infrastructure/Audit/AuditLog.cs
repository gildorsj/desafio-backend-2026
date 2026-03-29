namespace BancaPlataforma.Infrastructure.Audit;

public sealed class AuditLog
{
    public Guid Id { get; set; }
    public string Tabela { get; set; } = string.Empty;
    public string EntidadeId { get; set; } = string.Empty;
    public string Operacao { get; set; } = string.Empty;
    public string? ValoresAntigos { get; set; }
    public string? ValoresNovos { get; set; }
    public DateTime OcorridoEm { get; set; }
}
