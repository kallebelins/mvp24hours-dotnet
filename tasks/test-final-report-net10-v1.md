# Relatório final de testes — TRX + cobertura (tarefa 9.5)

> Data: 16/07/2026 · Task ADO [#87318](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87318)  
> Consolida as execuções das tarefas **9.2** (unitário/InMemory) e **9.3** (Testcontainers), com reexecução unificada + coletores alinhados ao CI.  
> Resumo máquina: [`test-final-report-net10-v1.json`](./test-final-report-net10-v1.json)  
> Artefatos locais (gitignored): `tasks/test-results-9.5/**/*.trx` e `**/coverage.cobertura.xml`

## Comando (espelha CI)

```powershell
dotnet test <csproj> -c Debug --nologo `
  --logger "trx;LogFileName=<name>.trx" `
  --results-directory tasks/test-results-9.5/<name> `
  --collect:"XPlat Code Coverage" `
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
```

Referência CI: `.github/workflows/ci.yml` step `🧪 Run tests` (`--logger trx --collect:"XPlat Code Coverage"`).

## Resultado consolidado (16 projetos)

| Métrica | Valor |
|---|---:|
| Projetos | **16**/16 com **0** falhas |
| Aprovados | **2298** |
| Falhas | **0** |
| Ignorados | **4** |
| Total | **2302** |
| Cobertura local com hits | **3** projetos (line-rate agregado ~**28,4%** entre os com hits) |

Equivalência com 9.2 + 9.3: **2188 + 110 = 2298** aprovados (após correção pontual abaixo).

---

## Por projeto

### Grupo A — Unitário / in-process

| Projeto | Aprovado | Falha | Ignorado | Total | Cobertura (local) |
|---|---:|---:|---:|---:|---|
| `Mvp24Hours.Core.Test` | 788 | 0 | 0 | 788 | empty |
| `Mvp24Hours.Application.Test` | 264 | 0 | 0 | 264 | empty |
| `Mvp24Hours.Infrastructure.Cqrs.Test` | 347 | 0 | 0 | 347 | ~28,2% (hits) |
| `Mvp24Hours.Infrastructure.Data.MongoDb.Test` | 133 | 0 | 0 | 133 | instrumented / 0 hits |
| `Mvp24Hours.Infrastructure.CronJob.Test` | 91 | 0 | 0 | 91 | ~28,8% (hits) |
| `Mvp24Hours.Application.Pipe.Test` | 78 | 0 | 0 | 78 | empty |
| `Mvp24Hours.Infrastructure.Caching.Test` | 38 | 0 | 0 | 38 | empty |
| `Mvp24Hours.Patterns.Test` | 20 | 0 | 0 | 20 | empty |
| `Mvp24Hours.WebAPI.Test` | 5 | 0 | 0 | 5 | instrumented / 0 hits |
| **Subtotal A** | **1764** | **0** | **0** | **1764** | |

### Grupo B — EF Core InMemory

| Projeto | Aprovado | Falha | Ignorado | Total | Cobertura (local) |
|---|---:|---:|---:|---:|---|
| `Mvp24Hours.Application.SQLServer.Test` | 232 | 0 | 4 | 236 | empty |
| `Mvp24Hours.Application.MySql.Test` | 96 | 0 | 0 | 96 | empty |
| `Mvp24Hours.Application.PostgreSql.Test` | 96 | 0 | 0 | 96 | empty |
| **Subtotal B** | **424** | **0** | **4** | **428** | |

### Grupo C — Testcontainers / integração

| Projeto | Aprovado | Falha | Ignorado | Total | Cobertura (local) |
|---|---:|---:|---:|---:|---|
| `Mvp24Hours.Application.Integration.Test` | 69 | 0 | 0 | 69 | empty |
| `Mvp24Hours.Application.MongoDb.Test` | 11 | 0 | 0 | 11 | instrumented / 0 hits |
| `Mvp24Hours.Application.Redis.Test` | 24 | 0 | 0 | 24 | ~40,9% (Caching.Redis) |
| `Mvp24Hours.Application.RabbitMQ.Test` | 6 | 0 | 0 | 6 | instrumented / 0 hits |
| **Subtotal C** | **110** | **0** | **0** | **110** | |

### Ignorados (esperados — InMemory)

Em `SQLServer.Test` / `Test7BulkOperations.cs` (mesmo da 9.2):

| Motivo | Qtd |
|---|---:|
| `ExecuteUpdateAsync is not supported by InMemory provider` | 1 |
| `ExecuteDeleteAsync is not supported by InMemory provider` | 3 |

---

## Correção aplicada durante a 9.5

**6 falhas iniciais em `ConvertExtensionsTest`** (RemoveDiacritics / ReplaceSpecialChar): `InlineData` com diacríticos estava em UTF-8 **duplamente codificado** (mojibake `CafÃ©` em vez de `Café`), o que quebrava as asserções sob a rodada com collector.

**Correção:** reescritos os literais com escapes Unicode (`\u00e1`, `\u00e9`, `\u00fc`, …) em `ConvertExtensionsTest.cs` — encoding-safe e estável em Windows. Após o fix: **788/788** em `Core.Test`.

Alinha também o residual reportado na 9.4 (`Category=Unit` com 6 falhas “preexistentes” nesse arquivo).

---

## Cobertura (coverlet / XPlat)

- Coletor idêntico ao CI em todos os 16 projetos; artefatos `.trx` + `coverage.cobertura.xml` gerados em `tasks/test-results-9.5/` (não versionados).
- Vários projetos emitem `cobertura` **vazio** (`lines-valid=0`) com as settings default do collector — comportamento conhecido sem `Include`/`Exclude` finos; o job CI sobe o mesmo output ao Codecov.
- Hits locais significativos: `Infrastructure.Cqrs.Test`, `Infrastructure.CronJob.Test`, `Application.Redis.Test` (pacote `Caching.Redis`).
- Tendência contínua de cobertura: **Codecov** no workflow (`codecov-action` em `ubuntu-latest`), não o dump local.

## Higiene de repositório

`.gitignore` passou a expor explicitamente as evidências versionáveis (já documentado na 8.2, mas ausente no arquivo):

```gitignore
tasks/*
!tasks/*.md
!tasks/*.json
tasks/test-results*/
**/TestResults/
*.trx
coverage.cobertura.xml
```

---

## Conclusão

Suíte completa **A + B + C** verde em `net10.0` / Debug: **2298** aprovados, **0** falhas, **4** skips documentados. Evidência TRX + cobertura coletada para anexo ao PR final da modernização. Pronto para **Fase 10** (zero warnings / format / TreatWarningsAsErrors / CHANGELOG).
