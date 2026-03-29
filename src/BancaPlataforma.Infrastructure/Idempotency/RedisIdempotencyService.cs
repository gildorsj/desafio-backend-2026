using BancaPlataforma.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace BancaPlataforma.Infrastructure.Idempotency;

public sealed class RedisIdempotencyService(IDistributedCache cache) : IIdempotencyService
{
    private static readonly TimeSpan Expiracao = TimeSpan.FromDays(7);

    public async Task<bool> ExisteAsync(string key, CancellationToken ct)
    {
        var valor = await cache.GetStringAsync(key, ct);
        return valor is not null;
    }

    public Task RegistrarAsync(string key, CancellationToken ct) =>
        cache.SetStringAsync(key, "1", new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = Expiracao
        }, ct);
}
