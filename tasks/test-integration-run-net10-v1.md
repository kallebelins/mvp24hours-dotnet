# Execução da suíte Testcontainers / integração (tarefa 9.3)

> Data: 16/07/2026 · Task ADO [#87316](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87316)  
> Inventário de escopo: [`test-inventory-net10-v1.md`](./test-inventory-net10-v1.md) (Grupo **C**)  
> Construtores Testcontainers já migrados na **5.8**.

## Ambiente

| Item | Valor |
|---|---|
| Docker Desktop | **4.79.0** / Engine **29.5.3** |
| Context | `desktop-linux` |
| Imagens pré-puxadas | `mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04`, `mongo:6.0`, `redis:3.2.5-alpine`, `rabbitmq:3.13-management` |

## Comando

```powershell
dotnet test <csproj> -c Debug --nologo --logger "trx;LogFileName=<name>.trx" --results-directory tasks/test-results-9.3
```

Logs/TRX locais (gitignored): `tasks/test-results-9.3/` · resumo máquina: [`test-integration-run-net10-v1.json`](./test-integration-run-net10-v1.json).

---

## Resultado consolidado

| Métrica | Valor |
|---|---:|
| Projetos | **4**/4 exit 0 |
| Aprovados | **110** |
| Falhas | **0** |
| Ignorados | **0** |
| Total | **110** |
| Tempo wall-clock (1ª rodada + retry RabbitMQ) | ~238 s + ~95 s |

---

## Grupo C — Testcontainers

| Projeto | Aprovado | Falha | Ignorado | Total | Duração (teste) | Status |
|---|---:|---:|---:|---:|---|---|
| `Mvp24Hours.Application.Integration.Test` | 69 | 0 | 0 | 69 | ~3 s | Aprovado |
| `Mvp24Hours.Application.MongoDb.Test` | 11 | 0 | 0 | 11 | ~37 s | Aprovado |
| `Mvp24Hours.Application.Redis.Test` | 24 | 0 | 0 | 24 | ~39 s | Aprovado |
| `Mvp24Hours.Application.RabbitMQ.Test` | 6 | 0 | 0 | 6 | ~71 s | Aprovado |
| **Subtotal C** | **110** | **0** | **0** | **110** | | |

> Nota: `Integration.Test` reportou **69** casos (inventário ~59 atributos Fact/Theory — Theories/`InlineData` expandem em runtime).

---

## Correção aplicada durante a 9.3

**Falha inicial (RabbitMQ 0/6):** `TypeLoadException: Could not load type 'RabbitMQ.Client.IModel' from assembly 'RabbitMQ.Client, Version=7.0.0.0'`.

**Causa:** `Mvp24Hours.Application.RabbitMQ.Test` referenciava `MassTransit` / `MassTransit.RabbitMQ` **9.1.2** (não usados em nenhum `.cs`), que puxavam `RabbitMQ.Client` **7.2.1**. Em runtime, a 7.x sobrescrevia a **6.8.1** exigida por `Infrastructure.RabbitMQ` (`IModel` removido na 7.x).

**Correção:**
- Removidos `PackageReference` MassTransit* do `.csproj` de teste.
- Removidos `PackageVersion` órfãos MassTransit* de `Directory.Packages.props`.
- Após o fix: `RabbitMQ.Client` resolve para **6.8.1** · **6/6** aprovados.

Migração completa de `Infrastructure.RabbitMQ` para RabbitMQ.Client ≥7 permanece fora do escopo desta fase.

---

## Conclusão

Suíte **Grupo C** verde em `net10.0` / Debug com Docker local: **110** aprovados, **0** falhas. Pronto para **9.4** (`Trait`/`Category`) e consolidação **9.5**.
