using BancaPlataforma.Application.Accounts.Queries;
using BancaPlataforma.Application.Common.Interfaces;
using BancaPlataforma.Infrastructure.ReadModel.Documents;
using MongoDB.Driver;

namespace BancaPlataforma.Infrastructure.ReadModel.Repositories;

public sealed class ContaReadRepository(IMongoDatabase database) : IContaReadRepository
{
    private readonly IMongoCollection<ContaSaldoDocument> _saldos =
        database.GetCollection<ContaSaldoDocument>("saldos");

    private readonly IMongoCollection<TransacaoDocument> _transacoes =
        database.GetCollection<TransacaoDocument>("transacoes");

    public async Task<SaldoDto?> ObterSaldoAsync(Guid contaId, CancellationToken ct)
    {
        var doc = await _saldos
            .Find(s => s.ContaId == contaId)
            .FirstOrDefaultAsync(ct);

        return doc is null ? null
            : new SaldoDto(doc.ContaId, doc.Saldo, doc.Moeda, doc.AtualizadoEm);
    }

    public async Task<ExtratoDto?> ObterExtratoAsync(ExtratoQuery query, CancellationToken ct)
    {
        var filter = Builders<TransacaoDocument>.Filter.Eq(t => t.ContaId, query.ContaId);

        if (query.DataInicio.HasValue)
            filter &= Builders<TransacaoDocument>.Filter.Gte(t => t.CriadoEm, query.DataInicio.Value);

        if (query.DataFim.HasValue)
            filter &= Builders<TransacaoDocument>.Filter.Lte(t => t.CriadoEm, query.DataFim.Value.AddDays(1));

        if (!string.IsNullOrWhiteSpace(query.Tipo))
            filter &= Builders<TransacaoDocument>.Filter.Eq(t => t.Tipo, query.Tipo);

        var tamanhoPagina = Math.Min(query.TamanhoPagina, 100);
        var skip = (query.Pagina - 1) * tamanhoPagina;

        var total = await _transacoes.CountDocumentsAsync(filter, cancellationToken: ct);

        var docs = await _transacoes
            .Find(filter)
            .SortByDescending(t => t.CriadoEm)
            .Skip(skip)
            .Limit(tamanhoPagina)
            .ToListAsync(ct);

        var transacoes = docs.Select(d => new TransacaoDto(
            d.Id, d.Tipo, d.Valor, d.Moeda, d.Descricao, d.ContaDestinoId, d.CriadoEm))
            .ToList();

        return new ExtratoDto(query.ContaId, transacoes, query.Pagina, tamanhoPagina, (int)total);
    }
}