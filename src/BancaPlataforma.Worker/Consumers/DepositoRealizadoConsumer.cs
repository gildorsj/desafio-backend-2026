using BancaPlataforma.Domain.Events;
using BancaPlataforma.Infrastructure.ReadModel.Documents;
using MassTransit;
using MongoDB.Driver;

namespace BancaPlataforma.Worker.Consumers;

public sealed class DepositoRealizadoConsumer(IMongoDatabase database)
    : IConsumer<DepositoRealizadoEvent>
{
    private readonly IMongoCollection<ContaSaldoDocument> _saldos =
        database.GetCollection<ContaSaldoDocument>("saldos");

    private readonly IMongoCollection<TransacaoDocument> _transacoes =
        database.GetCollection<TransacaoDocument>("transacoes");

    public async Task Consume(ConsumeContext<DepositoRealizadoEvent> context)
    {
        var ev = context.Message;

        // Atualiza saldo
        var update = Builders<ContaSaldoDocument>.Update
            .Inc(s => s.Saldo, ev.Valor)
            .Set(s => s.AtualizadoEm, ev.OcorridoEm);

        await _saldos.UpdateOneAsync(
            s => s.ContaId == ev.ContaId,
            update,
            cancellationToken: context.CancellationToken);

        // Insere transação no extrato
        var transacao = new TransacaoDocument
        {
            Id = ev.TransacaoId,
            ContaId = ev.ContaId,
            Tipo = "Deposito",
            Valor = ev.Valor,
            Moeda = ev.Moeda,
            Descricao = ev.Descricao,
            CriadoEm = ev.OcorridoEm
        };

        await _transacoes.InsertOneAsync(transacao, cancellationToken: context.CancellationToken);
    }
}
