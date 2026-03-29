using BancaPlataforma.Domain.Events;
using BancaPlataforma.Infrastructure.ReadModel.Documents;
using MassTransit;
using MongoDB.Driver;

namespace BancaPlataforma.Worker.Consumers;

public sealed class ContaAbertaConsumer(IMongoDatabase database)
    : IConsumer<ContaAbertaEvent>
{
    private readonly IMongoCollection<ContaSaldoDocument> _saldos =
        database.GetCollection<ContaSaldoDocument>("saldos");

    public async Task Consume(ConsumeContext<ContaAbertaEvent> context)
    {
        var ev = context.Message;

        var doc = new ContaSaldoDocument
        {
            ContaId = ev.ContaId,
            Saldo = 0,
            Moeda = "BRL",
            AtualizadoEm = ev.OcorridoEm
        };

        await _saldos.ReplaceOneAsync(
            s => s.ContaId == ev.ContaId,
            doc,
            new ReplaceOptions { IsUpsert = true },
            context.CancellationToken);
    }
}
