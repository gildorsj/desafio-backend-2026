using BancaPlataforma.Domain.Events;
using BancaPlataforma.Infrastructure.ReadModel.Documents;
using MassTransit;
using MongoDB.Driver;

namespace BancaPlataforma.Worker.Consumers;

public sealed class StatusContaAlteradoConsumer(IMongoDatabase database)
    : IConsumer<StatusContaAlteradoEvent>
{
    private readonly IMongoCollection<ContaSaldoDocument> _saldos =
        database.GetCollection<ContaSaldoDocument>("saldos");

    public async Task Consume(ConsumeContext<StatusContaAlteradoEvent> context)
    {
        var ev = context.Message;

        var update = Builders<ContaSaldoDocument>.Update
            .Set(s => s.AtualizadoEm, ev.OcorridoEm);

        await _saldos.UpdateOneAsync(
            s => s.ContaId == ev.ContaId,
            update,
            cancellationToken: context.CancellationToken);
    }
}
