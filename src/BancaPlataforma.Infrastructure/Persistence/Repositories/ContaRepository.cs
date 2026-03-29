using BancaPlataforma.Application.Common.Interfaces;
using BancaPlataforma.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace BancaPlataforma.Infrastructure.Persistence.Repositories;

public sealed class ContaRepository(BancaDbContext context) : IContaRepository
{
    public Task<Conta?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        context.Contas
            .Include(c => c.Transacoes)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Conta?> ObterPorCnpjAsync(string cnpj, CancellationToken ct) =>
        context.Contas
            .Include(c => c.Transacoes)
            .FirstOrDefaultAsync(c => c.Cnpj.Valor == cnpj, ct);

    public Task<bool> ExistePorCnpjAsync(string cnpj, CancellationToken ct) =>
        context.Contas.AnyAsync(c => c.Cnpj.Valor == cnpj, ct);

    public async Task AdicionarAsync(Conta conta, CancellationToken ct) =>
        await context.Contas.AddAsync(conta, ct);

    public void Atualizar(Conta conta)
    {
        // Não faz nada — o EF Core rastreia automaticamente
        // as mudanças feitas na entidade carregada pelo ObterPorIdAsync
    }
}
