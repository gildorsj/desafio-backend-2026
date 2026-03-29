using BancaPlataforma.Infrastructure.ReadModel.Documents;
using MongoDB.Driver;

namespace BancaPlataforma.Worker;

public static class MongoDbIndexes
{
    public static async Task CriarAsync(IMongoDatabase database)
    {
        var transacoes = database.GetCollection<TransacaoDocument>("transacoes");

        // Índice composto por ContaId + CriadoEm (para consultas de extrato)
        var indexContaIdData = Builders<TransacaoDocument>.IndexKeys
            .Ascending(t => t.ContaId)
            .Descending(t => t.CriadoEm);

        await transacoes.Indexes.CreateOneAsync(
            new CreateIndexModel<TransacaoDocument>(
                indexContaIdData,
                new CreateIndexOptions { Name = "idx_contaid_criadoem" }));

        // Índice por tipo (para filtro no extrato)
        var indexTipo = Builders<TransacaoDocument>.IndexKeys
            .Ascending(t => t.Tipo);

        await transacoes.Indexes.CreateOneAsync(
            new CreateIndexModel<TransacaoDocument>(
                indexTipo,
                new CreateIndexOptions { Name = "idx_tipo" }));
    }
}
