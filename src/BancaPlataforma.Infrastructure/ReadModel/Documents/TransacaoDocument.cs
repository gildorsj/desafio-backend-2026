using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BancaPlataforma.Infrastructure.ReadModel.Documents;

public sealed class TransacaoDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid ContaId { get; set; }

    public string Tipo { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string Moeda { get; set; } = "BRL";
    public string Descricao { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public Guid? ContaDestinoId { get; set; }

    public DateTime CriadoEm { get; set; }
}
