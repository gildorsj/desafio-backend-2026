# Fix: DbUpdateConcurrencyException on Financial Operations

## Context

Financial operations (Depositar, Sacar, Transferir) are crashing with `DbUpdateConcurrencyException`, returning HTTP 500. Two distinct concurrency bugs exist:

**Bug 1 (immediate crash)**: TOCTOU race condition in idempotency service. Two concurrent requests with the same `IdempotencyKey` both pass the Redis pre-check (key not yet registered). Both proceed to INSERT a `Transacao` with the same unique `idempotency_key`. The unique constraint violation (Postgres 23505) aborts the entire transaction batch. Npgsql then sees 0 rows affected for the `UPDATE contas` command (because the batch was already aborted), and throws `DbUpdateConcurrencyException` — even though the root cause is a duplicate-key insertion, not a real concurrency conflict.

**Bug 2 (silent data corruption)**: Without a concurrency token on `Conta`, two concurrent requests with **different** idempotency keys on the same account (e.g., two simultaneous deposits) cause a lost update. Both load `saldo = 100`, both apply their delta locally, and the second `SaveChangesAsync` silently overwrites the first. Expected balance: 200; actual balance: 150.

## Solution

### 1. Add DB-level idempotency pre-check to all financial handlers

Before loading the `Conta`, check whether a `Transacao` with the given `idempotency_key` already exists in PostgreSQL. This is the ground truth — the unique index on `transacoes.idempotency_key` is the final arbiter. This eliminates the TOCTOU window that causes the constraint collision, because the check happens at the same DB tier as the INSERT.

Add `ExisteTransacaoPorIdempotencyKeyAsync(string key, CancellationToken ct)` to `IContaRepository` and implement it in `ContaRepository`.

Updated handler flow:
```
if (Redis.Existe(key)) return;          // fast path
if (DB.ExisteTransacao(key)) {          // ground truth
    Redis.Registrar(key);               // sync cache
    return;
}
conta = Repo.ObterPorId(...)
conta.Depositar(...)
unitOfWork.CommitAsync()                // constraint collision can no longer happen
Redis.Registrar(key)
```

### 2. Add `xmin` optimistic concurrency token to `Conta`

Use Npgsql's `UseXminAsConcurrencyToken()` on the `Conta` entity configuration. This maps PostgreSQL's built-in `xmin` system column (the transaction ID of the last row modification) as a concurrency token. EF Core generates `UPDATE contas SET ... WHERE id = @id AND xmin = @originalXmin`. If another transaction modified the row in between, `xmin` changes, 0 rows are affected, and `DbUpdateConcurrencyException` is thrown — correctly this time. No DDL migration is needed (the column always exists in Postgres), but a model snapshot migration is required.

### 3. Handle `DbUpdateConcurrencyException` in ExceptionHandlingMiddleware

Add an explicit catch for `DbUpdateConcurrencyException` that returns **HTTP 409 Conflict** with an actionable message ("Conflito de concorrência, tente novamente."). This prevents it from falling through to the generic 500 handler. Clients that receive 409 on a financial operation should retry.

## Critical Files

| File | Change |
|---|---|
| `src/BancaPlataforma.Application/Common/Interfaces/IContaRepository.cs` | Add `ExisteTransacaoPorIdempotencyKeyAsync` |
| `src/BancaPlataforma.Infrastructure/Persistence/Repositories/ContaRepository.cs` | Implement the above |
| `src/BancaPlataforma.Infrastructure/Persistence/Configurations/ContaConfiguration.cs` | Add `builder.UseXminAsConcurrencyToken()` |
| `src/BancaPlataforma.Application/Accounts/Commands/Depositar/DepositarCommandHandler.cs` | Add DB idempotency pre-check |
| `src/BancaPlataforma.Application/Accounts/Commands/Sacar/SacarCommandHandler.cs` | Add DB idempotency pre-check |
| `src/BancaPlataforma.Application/Accounts/Commands/Transferir/TransferirCommandHandler.cs` | Add DB idempotency pre-check |
| `src/BancaPlataforma.API/Middleware/ExceptionHandlingMiddleware.cs` | Catch `DbUpdateConcurrencyException` → 409 |

### New files
- New EF Core migration (model snapshot update for `xmin` token, no SQL changes)

## Implementation Details

### IContaRepository.cs
```csharp
Task<bool> ExisteTransacaoPorIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default);
```

### ContaRepository.cs
```csharp
public Task<bool> ExisteTransacaoPorIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct) =>
    context.Transacoes.AnyAsync(t => t.IdempotencyKey == idempotencyKey, ct);
```
Note: `context.Transacoes` requires adding `public DbSet<Transacao> Transacoes => Set<Transacao>();` to `BancaDbContext` — it already exists.

### ContaConfiguration.cs
```csharp
// After existing builder calls:
builder.UseXminAsConcurrencyToken();
```
This is an Npgsql extension method on `EntityTypeBuilder<Conta>`.

### DepositarCommandHandler.cs (same pattern for Sacar)
```csharp
public async Task Handle(DepositarCommand request, CancellationToken ct)
{
    if (await idempotency.ExisteAsync(request.IdempotencyKey, ct))
        return;
 
    if (await repository.ExisteTransacaoPorIdempotencyKeyAsync(request.IdempotencyKey, ct))
    {
        await idempotency.RegistrarAsync(request.IdempotencyKey, ct);
        return;
    }
 
    var conta = await repository.ObterPorIdAsync(request.ContaId, ct)
                ?? throw new DomainException("Conta não encontrada.");
 
    conta.Depositar(request.Valor, request.Moeda, request.Descricao, request.IdempotencyKey);
 
    repository.Atualizar(conta);
    await unitOfWork.CommitAsync(ct);
    await idempotency.RegistrarAsync(request.IdempotencyKey, ct);
}
```

### TransferirCommandHandler.cs
Same DB idempotency pre-check added before loading both accounts.

### ExceptionHandlingMiddleware.cs
```csharp
catch (DbUpdateConcurrencyException ex)
{
    logger.LogWarning(ex, "Conflito de concorrência detectado");
    await EscreverRespostaAsync(context, StatusCodes.Status409Conflict,
        "Conflito de concorrência, tente novamente.");
}
```
(Added before the generic `Exception` catch.)

## Verification

1. Run the application and send two concurrent `POST /api/v1/accounts/{id}/deposit` requests with the **same** `IdempotencyKey` at the same time — should return 204 for first, 204 for second (idempotent, no 500).
2. Send two concurrent deposits with **different** idempotency keys — one may return 409 (xmin conflict); client retries and gets 204. Final balance should reflect both deposits.
3. Run existing tests: `dotnet test`.
4. Run the migration: `dotnet ef migrations add AddXminConcurrencyToken --project src/BancaPlataforma.Infrastructure --startup-project src/BancaPlataforma.API`.
5. Apply: `dotnet ef database update`.