namespace BancaPlataforma.Infrastructure.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
    public DateTime? ProcessadoEm { get; set; }
    public string? Erro { get; set; }
}
