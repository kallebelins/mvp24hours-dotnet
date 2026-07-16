# Inventário de projetos de teste (tarefa 9.1)

> Data: 16/07/2026 · Task ADO [#87314](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87314)

## Resumo

| Grupo | Projetos | Docker? | Escopo 9.x |
|---|---|---|---|
| **A — Unitário / in-process** | 9 | Não | **9.2** |
| **B — EF Core InMemory (default)** | 3 | Não (default) | **9.2** (com `InMemory`) |
| **C — Testcontainers** | 4 | **Sim** | **9.3** |
| **Total** | **16** (não 15) | — | — |

> A lista da tarefa citava 15 projetos; existe também `Mvp24Hours.Infrastructure.Data.MongoDb.Test` (unitário, Moq/FluentAssertions — sem container Mongo real).

Contagens = ocorrências de `[Fact]`/`[Theory]` no código (Theories com vários `InlineData` expandem em runtime; ex.: Core.Test já rodou **788** casos na auditoria prévia).

---

## Docker no ambiente (16/07/2026)

| Item | Valor |
|---|---|
| Client / Engine | **29.5.3** |
| Desktop | **4.79.0** (230596) |
| Context | `desktop-linux` |
| Smoke test | `docker pull/run hello-world` → **OK** |
| Containers running | 0 |
| Images locais | 254 |

**Mudança vs. auditoria inicial:** na auditoria prévia `docker version` falhava ao conectar no daemon. Neste ambiente o daemon está ativo — a **9.3** pode seguir sem bloqueio de provisionamento local.

---

## Grupo A — Unitário / in-process (9.2)

Sem Testcontainers, sem conexão a serviço externo real.

| Projeto | ≈ Fact+Theory | Notas |
|---|---:|---|
| `Mvp24Hours.Core.Test` | 649 | Baseline auditoria: 788/788 aprovados |
| `Mvp24Hours.Application.Test` | 226 | Moq / FluentAssertions |
| `Mvp24Hours.Infrastructure.Cqrs.Test` | 347 | In-memory / Moq |
| `Mvp24Hours.Infrastructure.Data.MongoDb.Test` | 133 | Testa options/policies; **não** sobe Mongo |
| `Mvp24Hours.Infrastructure.CronJob.Test` | 91 | `TimeProvider` fake / Moq |
| `Mvp24Hours.Application.Pipe.Test` | 78 | DI in-process |
| `Mvp24Hours.Infrastructure.Caching.Test` | 38 | Memory cache |
| `Mvp24Hours.Patterns.Test` | 20 | **WireMock.Net** (HTTP mock in-process) |
| `Mvp24Hours.WebAPI.Test` | 5 | `Microsoft.AspNetCore.TestHost` |
| **Subtotal A** | **~1587** | |

---

## Grupo B — EF Core com `InMemory` por default (9.2)

`DefineConstants` inclui `InMemory`. Startup usa `#if InMemory` → `UseInMemoryDatabase`. Sem Docker no build padrão.

| Projeto | ≈ Fact+Theory | Provider real (ifdef off) | Connection string (`appsettings.json`) |
|---|---:|---|---|
| `Mvp24Hours.Application.SQLServer.Test` | 236 | `UseSqlServer` | `Data Source=.,1433;...sa...` |
| `Mvp24Hours.Application.MySql.Test` | 96 | `UseMySQL` | `server=localhost;...root...` |
| `Mvp24Hours.Application.PostgreSql.Test` | 96 | `UseNpgsql` | `Host=localhost;Port=5432;...` |
| **Subtotal B** | **~428** | | |

Para exercitar o provider real: remover `InMemory` de `DefineConstants` **e** ter SQL Server / MySQL / PostgreSQL locais — fora do caminho default de CI/9.2. Alguns facts em `Test7BulkOperations` já estão `[Fact(Skip=...)]` por limitação do InMemory.

---

## Grupo C — Testcontainers / Docker (9.3)

| Projeto | ≈ Fact+Theory | Pacote / imagem | Detalhe |
|---|---:|---|---|
| `Mvp24Hours.Application.Integration.Test` | 59 | `Testcontainers.MsSql` → `mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04` | Fixture `SqlServerContainerFixture`. Pacote `Testcontainers.PostgreSql` referenciado mas **sem uso** no código (residual). `ValidationIntegrationTest` é unitário puro (FluentValidation) no mesmo projeto. |
| `Mvp24Hours.Application.MongoDb.Test` | 11 | `Testcontainers.MongoDb` → `mongo:6.0` | `MongoDbBuilder` em Command/Query |
| `Mvp24Hours.Application.Redis.Test` | 24 | `Testcontainers.Redis` → `redis:3.2.5-alpine` | 4 classes `IAsyncLifetime` |
| `Mvp24Hours.Application.RabbitMQ.Test` | 6 | `Testcontainers.RabbitMq` → `rabbitmq:3.13-management` | MassTransit + container |
| **Subtotal C** | **~100** | | Construtores já migrados na **5.8** |

---

## Mapa para 9.2 / 9.3 / 9.4

```
dotnet test (sem Docker)  →  Grupos A + B   ≈ 2015 atributos
dotnet test (com Docker)  →  Grupo C        ≈ 100 atributos
```

Sugestão para **9.4** (`Trait`/`Category`):

- `Category=Unit` → A + B (e, se desejado, `ValidationIntegrationTest` no Integration)
- `Category=Integration` → classes que sobem Testcontainers no Integration / MongoDb / Redis / RabbitMQ

CI atual (`.github/workflows/ci.yml`) roda `dotnet test` na solução inteira sem filtro — em runners **sem** Docker o Grupo C falha na subida do container.
