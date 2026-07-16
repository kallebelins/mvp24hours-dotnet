# Inventário Nullable Reference Types — .NET 10 (v1)

> Gerado em 2026-07-15  
> Fonte: leitura de `<Nullable>` em todos os `*.csproj` ativos sob `src/` + cruzamento com CS8632 do [baseline 1.1](./baseline-net10-v1.md)  
> Referência: [tasks-net10-v1.md](./tasks-net10-v1.md) §1.3 · ADO Task [#87256](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87256)  
> Dados brutos: [`inventory-nullable-net10-v1.json`](./inventory-nullable-net10-v1.json)

## Resumo

| Dimensão | Situação |
|---|---|
| Projetos ativos (não-backup) | **28** |
| Com `<Nullable>enable</Nullable>` | **11** |
| Sem `<Nullable>` (propriedade ausente) | **17** |
| Nenhum projeto usa `disable` / `warnings` / `annotations` | confirmado |
| `Directory.Build.props` com Nullable centralizado | **não existe** (tarefa 3.1) |
| CS8632 no baseline (Debug) | **3275** — 100% nos 8 projetos da tabela abaixo |

### Conclusões para as Fases 3 e 4

1. **Causa raiz do CS8632:** 7 projetos de produção + 1 de teste usam anotação `?` sem contexto nullable → **3271 + 4 = 3275** avisos.
2. **Checklist Fase 4 (definitivo):** habilitar Nullable nos **17** projetos sem a propriedade (tarefas **4.1** = 8 produção · **4.2** = 9 teste), mesmo nos que hoje não emitem CS8632 — após `Directory.Build.props` (3.1) a propriedade pode ser herdada e removida dos `.csproj` individuais.
3. **Exceção benévola em produção:** `Infrastructure.Caching.Redis` não tem sintaxe NRT nem CS8632 (só NU190x); ainda entra no checklist 4.1 por consistência da solução.
4. **Testes:** só `Application.RabbitMQ.Test` contribui CS8632 hoje (4); os outros 8 sem Nullable estão “limpos” de CS8632, mas devem herdá-lo na 4.2 / 3.1.

---

## Já com `<Nullable>enable</Nullable>` (11) — fora do escopo 4.1/4.2

| Projeto | Kind | Na .sln |
|---|---|---|
| `Mvp24Hours.Application` | production | sim |
| `Mvp24Hours.Core` | production | sim |
| `Mvp24Hours.Infrastructure.Cqrs` | production | sim |
| `Mvp24Hours.Infrastructure.CronJob` | production | sim |
| `Mvp24Hours.Application.Integration.Test` | test | sim |
| `Mvp24Hours.Application.Test` | test | **não** |
| `Mvp24Hours.Core.Test` | test | sim |
| `Mvp24Hours.Infrastructure.Caching.Test` | test | **não** |
| `Mvp24Hours.Infrastructure.Cqrs.Test` | test | sim |
| `Mvp24Hours.Infrastructure.CronJob.Test` | test | sim |
| `Mvp24Hours.Infrastructure.Data.MongoDb.Test` | test | **não** |

Esses projetos já entram no contexto nullable; avisos restantes neles são CS86xx “reais” (e outros códigos), não CS8632.

---

## Sem `<Nullable>enable</Nullable>` (17) — checklist Fase 4

### Produção — tarefa 4.1 (8)

| # | Projeto | Path | CS8632 (baseline) | Usa sintaxe `?` |
|---|---|---|---|---|
| 1 | `Mvp24Hours.Infrastructure` | `src/Mvp24Hours.Infrastructure/` | **898** | sim |
| 2 | `Mvp24Hours.Infrastructure.RabbitMQ` | `src/Mvp24Hours.Infrastructure.RabbitMQ/` | **830** | sim |
| 3 | `Mvp24Hours.Infrastructure.Pipe` | `src/Mvp24Hours.Infrastructure.Pipe/` | **529** | sim |
| 4 | `Mvp24Hours.WebAPI` | `src/Mvp24Hours.WebAPI/` | **339** | sim |
| 5 | `Mvp24Hours.Infrastructure.Caching` | `src/Mvp24Hours.Infrastructure.Caching/` | **295** | sim |
| 6 | `Mvp24Hours.Infrastructure.Data.EFCore` | `src/Mvp24Hours.Infrastructure.Data.EFCore/` | **261** | sim |
| 7 | `Mvp24Hours.Infrastructure.Data.MongoDb` | `src/Mvp24Hours.Infrastructure.Data.MongoDb/` | **119** | sim |
| 8 | `Mvp24Hours.Infrastructure.Caching.Redis` | `src/Mvp24Hours.Infrastructure.Caching.Redis/` | **0** | não |

Soma CS8632 produção: **3271**.

**Ordem sugerida 4.1** (menor → maior volume CS8632, depois Redis): MongoDb → EFCore → Caching → WebAPI → Pipe → RabbitMQ → Infrastructure → Caching.Redis.

### Testes — tarefa 4.2 (9)

| # | Projeto | Path | CS8632 (baseline) | Usa sintaxe `?` |
|---|---|---|---|---|
| 1 | `Mvp24Hours.Application.RabbitMQ.Test` | `src/Tests/Mvp24Hours.Application.RabbitMQ.Test/` | **4** | sim |
| 2 | `Mvp24Hours.Application.MongoDb.Test` | `src/Tests/Mvp24Hours.Application.MongoDb.Test/` | 0 | não |
| 3 | `Mvp24Hours.Application.MySql.Test` | `src/Tests/Mvp24Hours.Application.MySql.Test/` | 0 | não |
| 4 | `Mvp24Hours.Application.Pipe.Test` | `src/Tests/Mvp24Hours.Application.Pipe.Test/` | 0 | não |
| 5 | `Mvp24Hours.Application.PostgreSql.Test` | `src/Tests/Mvp24Hours.Application.PostgreSql.Test/` | 0 | não |
| 6 | `Mvp24Hours.Application.Redis.Test` | `src/Tests/Mvp24Hours.Application.Redis.Test/` | 0 | não |
| 7 | `Mvp24Hours.Application.SQLServer.Test` | `src/Tests/Mvp24Hours.Application.SQLServer.Test/` | 0 | não |
| 8 | `Mvp24Hours.Patterns.Test` | `src/Tests/Mvp24Hours.Patterns.Test/` | 0 | não |
| 9 | `Mvp24Hours.WebAPI.Test` | `src/Tests/Mvp24Hours.WebAPI.Test/` | 0 | não |

Soma CS8632 testes: **4**.

---

## Mapa CS8632 (causa raiz confirmada)

| Projeto | CS8632 | % do total |
|---|---|---|
| Infrastructure | 898 | 27.4% |
| Infrastructure.RabbitMQ | 830 | 25.3% |
| Infrastructure.Pipe | 529 | 16.2% |
| WebAPI | 339 | 10.4% |
| Infrastructure.Caching | 295 | 9.0% |
| Infrastructure.Data.EFCore | 261 | 8.0% |
| Infrastructure.Data.MongoDb | 119 | 3.6% |
| Application.RabbitMQ.Test | 4 | 0.1% |
| **Total** | **3275** | **100%** |

> Após habilitar `<Nullable>enable</Nullable>` (ou herdar via 3.1), esses CS8632 tendem a desaparecer e a revelar/aumentar avisos reais de nulidade (CS8600/CS8602/CS8603/CS8604/CS8618/…) — tratado em 4.3–4.5.

---

## Checklist de consumo (Fases 3–4)

- [ ] 3.1 — `Directory.Build.props` com `<Nullable>enable</Nullable>` (cobre os 17 de uma vez + novos projetos)
- [ ] 4.1 — checklist produção (8 linhas da tabela acima)
- [ ] 4.2 — checklist testes (9 linhas da tabela acima)
- [ ] 4.3–4.5 — corrigir avisos reais pós-enable (especialmente Core + hotspots Infrastructure.*)
