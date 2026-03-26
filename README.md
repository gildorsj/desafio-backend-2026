# Desafio Desenvolvedor Backend .NET — Plataforma Bancária

## Definições

- Leia **todo** o conteúdo antes de iniciar e busque entender de fato o desafio proposto.
- Faça um clone desse repositório para iniciar o projeto. Lembre-se de deixar o seu repositório **privado** e compartilhar com a conta do GitHub [MarcosVRSDev](https://github.com/MarcosVRSDev).
- Utilize **.NET 8** ou superior.
- Suba o projeto utilizando **Docker Compose**; a aplicação deve subir completamente com um único `docker compose up`.
- Documente a API com **Swagger/OpenAPI**.

---

## Desafio

Desenvolva uma **Plataforma de Contas Bancárias** seguindo os princípios de **Domain-Driven Design (DDD)** e o padrão **CQRS (Command Query Responsibility Segregation)** com arquitetura orientada a eventos via **RabbitMQ**.

Somente empresas podem abrir contas, identificadas por **CNPJ**.

---

## Endpoints e Contratos

> **Importante:** os campos dos requests abaixo são **obrigatórios**. Qualquer desvio de nomenclatura ou tipo será considerado erro de implementação. O formato e o contrato das respostas ficam a critério do candidato.

### Contas

#### `POST /api/v1/accounts` — Abrir conta

**Request Body** (`application/json`):
```json
{
  "cnpj": "11.222.333/0001-81",
  "agencia": "0001",
  "imagemDocumento": "<Base64 string>"
}
```

#### `GET /api/v1/accounts/{id}` — Obter conta por ID

#### `GET /api/v1/accounts/cnpj/{cnpj}` — Obter conta por CNPJ

#### `PATCH /api/v1/accounts/{id}/status` — Alterar status da conta

**Request Body**:
```json
{
  "status": "Bloqueada"
}
```

#### `DELETE /api/v1/accounts/{id}` — Encerrar conta

- Só é possível encerrar conta com **saldo zero**.
- Altera o status para `Encerrada` (soft delete).

---

### Operações Financeiras

#### `POST /api/v1/accounts/{id}/deposit` — Depósito

**Request Body**:
```json
{
  "idempotencyKey": "chave-unica-gerada-pelo-cliente",
  "valor": 500.00,
  "moeda": "BRL",
  "descricao": "Depósito inicial"
}
```

#### `POST /api/v1/accounts/{id}/withdraw` — Saque

**Request Body**:
```json
{
  "idempotencyKey": "chave-unica-gerada-pelo-cliente",
  "valor": 200.00,
  "moeda": "BRL",
  "descricao": "Saque operacional"
}
```

#### `POST /api/v1/accounts/{id}/transfer` — Transferência

**Request Body**:
```json
{
  "idempotencyKey": "chave-unica-gerada-pelo-cliente",
  "contaDestinoId": "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d",
  "valor": 350.50,
  "moeda": "BRL",
  "descricao": "Pagamento de serviço"
}
```

---

### Consultas (Read Side — MongoDB)

#### `GET /api/v1/accounts/{id}/balance` — Consultar saldo

#### `GET /api/v1/accounts/{id}/statement` — Extrato

**Query Parameters:**

| Parâmetro    | Tipo       | Obrigatório | Descrição                              |
|--------------|------------|-------------|----------------------------------------|
| `dataInicio` | `string`   | Não         | Data inicial (ISO 8601: `YYYY-MM-DD`)  |
| `dataFim`    | `string`   | Não         | Data final (ISO 8601: `YYYY-MM-DD`)    |
| `tipo`       | `string`   | Não         | `Deposito`, `Saque` ou `Transferencia` |
| `pagina`     | `int`      | Não         | Número da página (padrão: `1`)         |
| `tamanhoPagina` | `int`   | Não         | Itens por página (padrão: `20`, máx: `100`) |

---

## Regras de Negócio

1. **CNPJ**: deve ser validado pelo dígito verificador antes de qualquer operação. O CNPJ deve existir e estar ativo na Receita Federal (consultar via [ReceitaWS API](https://developers.receitaws.com.br/#/operations/queryCNPJFree)).
2. **RazaoSocial**: nunca é informada pelo usuário — deve ser obtida exclusivamente via ReceitaWS.
3. **Saldo**: nunca pode ser negativo. Saques e transferências devem verificar saldo disponível antes de publicar o evento.
4. **Idempotência**: operações financeiras devem ser idempotentes — o cliente deve fornecer um `idempotencyKey` único no corpo da requisição; reenvio com a mesma chave não deve gerar duplicidade.
5. **Conta bloqueada ou encerrada**: não aceita novos depósitos, saques ou transferências.
6. **Encerramento**: conta só pode ser encerrada com saldo igual a `0.00`.
7. **Transferência**: ambas as contas (origem e destino) devem estar com status `Ativa`.
8. **Consistência eventual**: como o processamento é assíncrono, o saldo e o extrato no MongoDB podem ter um atraso mínimo em relação à escrita no PostgreSQL.

---

## Requisitos Técnicos

### Stack Obrigatória

| Tecnologia              | Uso                                                     |
|-------------------------|---------------------------------------------------------|
| **.NET 8+**             | Runtime e framework principal                           |
| **ASP.NET Core**        | API REST                                                |
| **MediatR**             | Implementação de CQRS (Commands e Queries via Mediator) |
| **Entity Framework Core** | ORM para o modelo de escrita (PostgreSQL)             |
| **PostgreSQL**          | Banco de dados de escrita (Write Model)                 |
| **RabbitMQ**            | Message broker para Domain Events                       |
| **MongoDB**             | Banco de dados de leitura (Read Model / Projeções)      |
| **FluentValidation**    | Validação de comandos e requests                        |
| **Docker + Docker Compose** | Orquestração de todos os serviços                   |
| **Swagger/OpenAPI**     | Documentação da API                                     |

### Stack Recomendada (diferencial)

| Tecnologia        | Uso                                            |
|-------------------|------------------------------------------------|
| **MassTransit**   | Abstração sobre RabbitMQ (consumers, retries)  |
| **Serilog**       | Logging estruturado                            |
| **Polly**         | Resiliência (retry, circuit breaker) para chamadas externas |
| **Redis**         | Cache de respostas da ReceitaWS e idempotency store |
| **xUnit + Moq**   | Testes unitários e de integração               |
| **HealthChecks**  | Endpoints de saúde (`/health`)                 |

---

## Docker Compose

O `docker-compose.yml` deve orquestrar obrigatoriamente os serviços:

- `api` — ASP.NET Core application
- `worker` — Consumer de eventos RabbitMQ
- `postgres` — Banco de escrita
- `mongodb` — Banco de leitura
- `rabbitmq` — Message broker (com management plugin habilitado)

---

## O que será Avaliado

### Arquitetura e Design
- Aplicação dos princípios de **DDD** (Aggregates, Value Objects, Domain Events, Ubiquitous Language).
- Separação entre **Command Side** e **Query Side** (CQRS).
- Uso de **Domain Events** e **Event-Driven Architecture** com RabbitMQ.
- Consistência eventual entre Write Model (PostgreSQL) e Read Model (MongoDB).

### Qualidade de Código
- Legibilidade, nomenclatura e organização.
- Uso de recursos modernos do **C#**.
- Ausência de code smells.

### Robustez e Resiliência
- Validação dos inputs (FluentValidation).
- Tratamento de erros e respostas padronizadas.
- Idempotência nas operações financeiras.
- Resiliência nas chamadas à ReceitaWS.

### Infraestrutura e DevOps
- `docker compose up` sobe toda a aplicação sem intervenção manual.
- Documentação Swagger completa.

---

## Critérios Eliminatórios

- Não subir via Docker Compose.
- Não implementar CQRS com separação de read/write models.
- Não utilizar RabbitMQ para propagação de Domain Events.
- Não implementar os campos dos requests exatamente como definidos neste documento.

---

## Entrega

1. Deixe o repositório **privado**.
2. Compartilhe com [MarcosVRSDev](https://github.com/MarcosVRSDev).
3. Inclua um `README.md` no repositório com:
   - Instruções de como executar o projeto.
   - Decisões arquiteturais relevantes.
   - Melhorias futuras que implementaria com mais tempo.

Qualquer dúvida pode ser enviada para o e-mail: marcos.rezende@inovamobil.com.br
