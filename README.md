# Plataforma Bancária — Desafio Backend .NET

API REST para gerenciamento de contas bancárias corporativas (CNPJ), construída com .NET 8, DDD, CQRS e arquitetura orientada a eventos.

---

## Tecnologias

| Tecnologia | Uso |
|---|---|
| .NET 8 / ASP.NET Core | Runtime e API REST |
| Entity Framework Core 8 | ORM — Write Model (PostgreSQL) |
| PostgreSQL | Banco de dados de escrita |
| MongoDB | Banco de dados de leitura (projeções) |
| RabbitMQ + MassTransit 8.x | Message broker — Domain Events |
| Redis | Cache (ReceitaWS) + Idempotência |
| MediatR 14 | Implementação de CQRS |
| FluentValidation 12 | Validação de commands |
| Polly | Resiliência nas chamadas à ReceitaWS |
| Serilog | Logging estruturado |
| Swagger/OpenAPI | Documentação da API |
| Docker + Docker Compose | Orquestração de serviços |

---

## Como executar

### Pré-requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop) instalado e rodando
- Portas 8080, 5432, 27017, 5672, 15672 e 6379 disponíveis

### Subir a aplicação

```bash
docker compose up --build
```

Todos os serviços sobem automaticamente. As migrations do PostgreSQL e os índices do MongoDB são aplicados na inicialização.

### Acessar

| Serviço | URL | Credenciais |
|---|---|---|
| Swagger UI | http://localhost:8080 | — |
| HealthCheck | http://localhost:8080/health | — |
| RabbitMQ Management | http://localhost:15672 | bancauser / bancapass |

---

## Arquitetura

```
src/
├── BancaPlataforma.Domain/          # Entidades, Value Objects, Domain Events
├── BancaPlataforma.Application/     # Commands, Queries, Handlers (CQRS)
├── BancaPlataforma.Infrastructure/  # EF Core, MongoDB, RabbitMQ, Redis, ReceitaWS
├── BancaPlataforma.API/             # Controllers, Swagger, Middleware
└── BancaPlataforma.Worker/          # Consumer RabbitMQ → projeções MongoDB
```

### Fluxo de uma operação financeira

```
Client → API Controller
  → Command Handler (MediatR)
    → Aggregate valida regra de negócio
      → Persiste no PostgreSQL (Write Model)
        → Domain Event publicado via MassTransit → RabbitMQ
          → Worker consome o evento
            → Projeta saldo e extrato no MongoDB (Read Model)
```

### Decisões arquiteturais

**DDD**
- `Conta` é o Aggregate Root. Todo comportamento de negócio (depositar, sacar, transferir, encerrar) está encapsulado no agregado — nenhuma camada externa consegue colocar a entidade em estado inválido.
- `CNPJ` e `Dinheiro` são Value Objects imutáveis com validação embutida.
- Domain Events são levantados dentro do agregado e publicados após a persistência.

**CQRS**
- **Write Side**: comandos operam sobre o PostgreSQL via EF Core. O `DbContext` publica os Domain Events após o `SaveChanges`.
- **Read Side**: queries consultam o MongoDB, que é atualizado de forma assíncrona pelo Worker após consumir os eventos do RabbitMQ.

**Idempotência**
- Operações financeiras exigem um `idempotencyKey` fornecido pelo cliente.
- A chave é armazenada no Redis com TTL de 7 dias.
- Reenvios com a mesma chave retornam sucesso sem duplicar a operação.

**Resiliência**
- Chamadas à ReceitaWS usam Polly com retry exponencial (3 tentativas) e circuit breaker (5 falhas → 30s aberto).
- MassTransit configurado com retry automático nas filas (3 tentativas com intervalos de 500ms, 1s e 2s).

**Consistência eventual**
- O saldo e extrato no MongoDB podem ter atraso mínimo em relação ao PostgreSQL — comportamento esperado e documentado na regra de negócio.

---

## Endpoints

### Contas

| Método | Rota | Descrição |
|---|---|---|
| POST | /api/v1/accounts | Abrir conta |
| GET | /api/v1/accounts/{id} | Obter conta por ID |
| GET | /api/v1/accounts/cnpj/{cnpj} | Obter conta por CNPJ |
| PATCH | /api/v1/accounts/{id}/status | Alterar status |
| DELETE | /api/v1/accounts/{id} | Encerrar conta |

### Operações financeiras

| Método | Rota | Descrição |
|---|---|---|
| POST | /api/v1/accounts/{id}/deposit | Depósito |
| POST | /api/v1/accounts/{id}/withdraw | Saque |
| POST | /api/v1/accounts/{id}/transfer | Transferência |

### Consultas (Read Model — MongoDB)

| Método | Rota | Descrição |
|---|---|---|
| GET | /api/v1/accounts/{id}/balance | Saldo |
| GET | /api/v1/accounts/{id}/statement | Extrato paginado |

---

## Testes

```
tests/
├── BancaPlataforma.UnitTests/        # xUnit + Moq + FluentAssertions
└── BancaPlataforma.IntegrationTests/ # xUnit + Testcontainers + WebApplicationFactory
```

### Testes unitários

Cobrem as camadas de **Domain** e **Application** de forma isolada, sem dependências externas.

**Domain — Value Objects e Aggregate**

| Classe de teste | Casos cobertos |
|---|---|
| `CnpjTests` | CNPJ válido/inválido, normalização de dígitos, formatação, igualdade por valor |
| `DinheiroTests` | Criação, soma de mesma moeda, subtração com saldo insuficiente, moedas diferentes |
| `ContaTests` | Abertura, depósito, saque, transferência, encerramento, bloqueio, regras de negócio |

**Application — Command Handlers**

| Classe de teste | Casos cobertos |
|---|---|
| `AbrirContaCommandHandlerTests` | CNPJ válido cria conta, CNPJ duplicado, ReceitaWS indisponível, CNPJ inativo |
| `DepositarCommandHandlerTests` | Depósito com nova chave, idempotência duplicada, conta não encontrada |
| `SacarCommandHandlerTests` | Saque com nova chave, idempotência duplicada, saldo insuficiente, conta não encontrada |
| `TransferirCommandHandlerTests` | Transferência válida, idempotência duplicada, conta origem/destino não encontrada |
| `AlterarStatusContaCommandHandlerTests` | Alteração válida, conta não encontrada, status inválido |
| `EncerrarContaCommandHandlerTests` | Encerramento com saldo zero, encerramento com saldo, conta não encontrada |

#### Executar testes unitários

```bash
dotnet test tests/BancaPlataforma.UnitTests
```

### Testes de integração

Usam **Testcontainers** para subir instâncias reais de PostgreSQL, MongoDB e RabbitMQ em Docker durante a execução. A API é inicializada via `WebApplicationFactory<Program>` com as connection strings substituídas pelos containers de teste.

**Requer Docker em execução.**

| Classe de teste | Casos cobertos |
|---|---|
| `AccountsEndpointTests` | GET /health, POST /accounts (CNPJ inválido, campos faltando), GET por ID/CNPJ inexistentes, PATCH status inválido/conta inexistente, DELETE conta inexistente, POST deposit/withdraw/transfer em conta inexistente, GET balance/statement |

#### Executar testes de integração

```bash
dotnet test tests/BancaPlataforma.IntegrationTests
```

#### Executar todos os testes

```bash
dotnet test
```

---

## Regras de negócio implementadas

- CNPJ validado por dígito verificador e consultado na Receita Federal (ReceitaWS)
- Razão Social obtida exclusivamente via ReceitaWS — nunca informada pelo usuário
- Saldo nunca negativo — validado no agregado antes de publicar o evento
- Conta bloqueada ou encerrada não aceita movimentações
- Encerramento apenas com saldo zero (soft delete)
- Transferência exige ambas as contas com status `Ativa`
- Idempotência em todas as operações financeiras via Redis

---

## Melhorias futuras

- **Autenticação e autorização** — JWT com escopos por operação
- **Outbox Pattern** — garantir entrega dos Domain Events mesmo em caso de falha do RabbitMQ antes da publicação
- **Dead Letter Queue** — fila de mensagens com falha para reprocessamento manual
- **Paginação no GET /accounts** — listagem de contas com filtros
- **Audit log** — rastreabilidade completa de todas as operações
- **Rate limiting** — proteção contra abuso dos endpoints financeiros
- **Compressão de resposta** — Brotli/Gzip para reduzir payload
- **Cache de leitura** — Redis para cachear consultas de saldo frequentes
