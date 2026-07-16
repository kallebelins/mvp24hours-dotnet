# Execução da suíte unitária / InMemory (tarefa 9.2)

> Data: 16/07/2026 · Task ADO [#87315](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87315)  
> Inventário de escopo: [`test-inventory-net10-v1.md`](./test-inventory-net10-v1.md) (Grupos **A** + **B**)

## Comando

```powershell
# 9 projetos Grupo A + 3 projetos Grupo B (sem Testcontainers)
dotnet test <csproj> -c Debug --nologo --logger "trx;LogFileName=<name>.trx"
```

Projetos do Grupo **C** (Integration / MongoDb / Redis / RabbitMQ com Testcontainers) **excluídos** — ficam para a tarefa **9.3**.

Logs/TRX locais (gitignored): `tasks/test-results-9.2/` · resumo máquina: [`test-unit-run-net10-v1.json`](./test-unit-run-net10-v1.json).

---

## Resultado consolidado

| Métrica | Valor |
|---|---:|
| Projetos | **12**/12 exit 0 |
| Aprovados | **2188** |
| Falhas | **0** |
| Ignorados | **4** |
| Total | **2192** |
| Tempo wall-clock (script sequencial) | ~260 s |

---

## Grupo A — Unitário / in-process

| Projeto | Aprovado | Falha | Ignorado | Total | Duração (teste) | Status |
|---|---:|---:|---:|---:|---|---|
| `Mvp24Hours.Core.Test` | 788 | 0 | 0 | 788 | ~2 s | Aprovado |
| `Mvp24Hours.Application.Test` | 264 | 0 | 0 | 264 | ~396 ms | Aprovado |
| `Mvp24Hours.Infrastructure.Cqrs.Test` | 347 | 0 | 0 | 347 | ~429 ms | Aprovado |
| `Mvp24Hours.Infrastructure.Data.MongoDb.Test` | 133 | 0 | 0 | 133 | ~737 ms | Aprovado |
| `Mvp24Hours.Infrastructure.CronJob.Test` | 91 | 0 | 0 | 91 | ~2 min | Aprovado |
| `Mvp24Hours.Application.Pipe.Test` | 78 | 0 | 0 | 78 | ~249 ms | Aprovado |
| `Mvp24Hours.Infrastructure.Caching.Test` | 38 | 0 | 0 | 38 | ~665 ms | Aprovado |
| `Mvp24Hours.Patterns.Test` | 20 | 0 | 0 | 20 | ~2 s | Aprovado |
| `Mvp24Hours.WebAPI.Test` | 5 | 0 | 0 | 5 | ~659 ms | Aprovado |
| **Subtotal A** | **1764** | **0** | **0** | **1764** | | |

> Nota: `Application.Test` reportou **264** casos (atributos Fact/Theory no inventário ~226 — Theories com `InlineData` expandem em runtime).

---

## Grupo B — EF Core InMemory (default)

| Projeto | Aprovado | Falha | Ignorado | Total | Duração (teste) | Status |
|---|---:|---:|---:|---:|---|---|
| `Mvp24Hours.Application.SQLServer.Test` | 232 | 0 | 4 | 236 | ~1 s | Aprovado |
| `Mvp24Hours.Application.MySql.Test` | 96 | 0 | 0 | 96 | ~1 s | Aprovado |
| `Mvp24Hours.Application.PostgreSql.Test` | 96 | 0 | 0 | 96 | ~1 s | Aprovado |
| **Subtotal B** | **424** | **0** | **4** | **428** | | |

### Ignorados (esperados — InMemory)

Em `SQLServer.Test` / `Test7BulkOperations.cs`:

| Motivo | Qtd |
|---|---:|
| `ExecuteUpdateAsync is not supported by InMemory provider` | 1 |
| `ExecuteDeleteAsync is not supported by InMemory provider` | 3 |

Alinhado ao inventário 9.1: esses facts só fazem sentido com provider real (fora do caminho default de 9.2).

---

## Conclusão

Suíte **A + B** verde em `net10.0` / Debug: **2188** aprovados, **0** falhas, **4** skips documentados. Pronto para **9.3** (Testcontainers) e depois **9.4**/`Trait` + relatório **9.5**.
