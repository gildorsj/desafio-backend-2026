using BancaPlataforma.Domain.Events;
using BancaPlataforma.Infrastructure.ReadModel.Documents;
using MassTransit;
using MongoDB.Driver;

namespace BancaPlataforma.Worker.Consumers;

public sealed class TransferenciaRealizadaConsumer(IMongoDatabase database)
    : IConsumer<TransferenciaRealizadaEvent>
{
    private readonly IMongoCollection<ContaSaldoDocument> _saldos =
        database.GetCollection<ContaSaldoDocument>("saldos");

    private readonly IMongoCollection<TransacaoDocument> _transacoes =
        database.GetCollection<TransacaoDocument>("transacoes");

    public async Task Consume(ConsumeContext<TransferenciaRealizadaEvent> context)
    {
        var ev = context.Message;

        // Débito na origem
        var debito = Builders<ContaSaldoDocument>.Update
            .Inc(s => s.Saldo, -ev.Valor)
            .Set(s => s.AtualizadoEm, ev.OcorridoEm);

        await _saldos.UpdateOneAsync(
            s => s.ContaId == ev.ContaOrigemId,
            debito,
            cancellationToken: context.CancellationToken);

        // Crédito no destino
        var credito = Builders<ContaSaldoDocument>.Update
            .Inc(s => s.Saldo, ev.Valor)
            .Set(s => s.AtualizadoEm, ev.OcorridoEm);

        await _saldos.UpdateOneAsync(
            s => s.ContaId == ev.ContaDestinoId,
            credito,
            cancellationToken: context.CancellationToken);

        // Transação na origem
        await _transacoes.InsertOneAsync(new TransacaoDocument
        {
            Id = ev.TransacaoId,
            ContaId = ev.ContaOrigemId,
            Tipo = "Transferencia",
            Valor = ev.Valor,
            Moeda = ev.Moeda,
            Descricao = ev.Descricao,
            ContaDestinoId = ev.ContaDestinoId,
            CriadoEm = ev.OcorridoEm
        }, cancellationToken: context.CancellationToken);

        // Transação no destino (entrada)
        await _transacoes.InsertOneAsync(new TransacaoDocument
        {
            Id = Guid.NewGuid(),
            ContaId = ev.ContaDestinoId,
            Tipo = "Transferencia",
            Valor = ev.Valor,
            Moeda = ev.Moeda,
            Descricao = ev.Descricao,
            ContaDestinoId = ev.ContaDestinoId,
            CriadoEm = ev.OcorridoEm
        }, cancellationToken: context.CancellationToken);
    }
}