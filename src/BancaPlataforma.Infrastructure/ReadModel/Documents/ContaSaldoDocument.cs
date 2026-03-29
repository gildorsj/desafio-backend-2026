using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BancaPlataforma.Infrastructure.ReadModel.Documents;

public sealed class ContaSaldoDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid ContaId { get; set; }

    public decimal Saldo { get; set; }
    public string Moeda { get; set; } = "BRL";
    public DateTime AtualizadoEm { get; set; }
}
