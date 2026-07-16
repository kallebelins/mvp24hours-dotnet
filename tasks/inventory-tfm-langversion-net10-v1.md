# Inventário TargetFramework × LangVersion — .NET 10 (v1)

> Gerado em 2026-07-15  
> Fonte: leitura de todos os `*.csproj` sob `src/` + `dotnet sln src/Mvp24Hours.sln list`  
> Referência: [tasks-net10-v1.md](./tasks-net10-v1.md) §1.2 · ADO Task [#87255](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87255)  
> Dados brutos: [`inventory-tfm-langversion-net10-v1.json`](./inventory-tfm-langversion-net10-v1.json)

## Resumo

| Dimensão | Situação |
|---|---|
| Projetos ativos (não-backup) | **28** |
| Na solução `Mvp24Hours.sln` | **25** |
| Fora da `.sln` (mas presentes no disco) | **3** (todos em `net9.0`) |
| `.csproj` de backup (não na `.sln`) | **4** (`netcoreapp3.1` / LangVersion `9.0`) |
| TFM produção | **12/12** → `net10.0` |
| TFM testes ativos | **13** → `net10.0` · **3** → `net9.0` |
| `LangVersion` (ativos) | `12.0` (18) · `latest` (5) · ausente (4) · `13.0` (1) |

### Conclusões para as Fases 3 e 4

1. **Migração `net10.0` incompleta:** 3 projetos de teste permanecem em `net9.0` e, além disso, **não estão listados na `.sln`** — por isso a build da solução (baseline 1.1) não os compila.
2. **`LangVersion` inconsistente:** produção unificada em `12.0`; testes misturam `12.0`, `latest`, `13.0` e ausente (default do SDK 10 = C# 14).
3. **Alvo da tarefa 3.1 / 3.4:** centralizar `net10.0` + `LangVersion=latest` em `Directory.Build.props` e alinhar (ou incluir na `.sln`) os 3 projetos órfãos em `net9.0`.
4. **Backups:** 4 arquivos `Mvp24Hours - Backup.*.csproj` em `netcoreapp3.1` — candidatos à remoção na tarefa 3.5.

---

## Produção (12) — todos `net10.0` / `LangVersion=12.0`

| Projeto | TargetFramework | LangVersion | Na .sln |
|---|---|---|---|
| `Mvp24Hours.Application` | net10.0 | 12.0 | sim |
| `Mvp24Hours.Core` | net10.0 | 12.0 | sim |
| `Mvp24Hours.Infrastructure` | net10.0 | 12.0 | sim |
| `Mvp24Hours.Infrastructure.Caching` | net10.0 | 12.0 | sim |
| `Mvp24Hours.Infrastructure.Caching.Redis` | net10.0 | 12.0 | sim |
| `Mvp24Hours.Infrastructure.Cqrs` | net10.0 | 12.0 | sim |
| `Mvp24Hours.Infrastructure.CronJob` | net10.0 | 12.0 | sim |
| `Mvp24Hours.Infrastructure.Data.EFCore` | net10.0 | 12.0 | sim |
| `Mvp24Hours.Infrastructure.Data.MongoDb` | net10.0 | 12.0 | sim |
| `Mvp24Hours.Infrastructure.Pipe` | net10.0 | 12.0 | sim |
| `Mvp24Hours.Infrastructure.RabbitMQ` | net10.0 | 12.0 | sim |
| `Mvp24Hours.WebAPI` | net10.0 | 12.0 | sim |

---

## Testes (16 ativos)

| Projeto | TargetFramework | LangVersion | Na .sln |
|---|---|---|---|
| `Mvp24Hours.Application.Integration.Test` | net10.0 | 12.0 | sim |
| `Mvp24Hours.Application.MongoDb.Test` | net10.0 | 12.0 | sim |
| `Mvp24Hours.Application.MySql.Test` | net10.0 | 12.0 | sim |
| `Mvp24Hours.Application.Pipe.Test` | net10.0 | latest | sim |
| `Mvp24Hours.Application.PostgreSql.Test` | net10.0 | 12.0 | sim |
| `Mvp24Hours.Application.RabbitMQ.Test` | net10.0 | *(ausente)* | sim |
| `Mvp24Hours.Application.Redis.Test` | net10.0 | 12.0 | sim |
| `Mvp24Hours.Application.SQLServer.Test` | net10.0 | 12.0 | sim |
| `Mvp24Hours.Application.Test` | **net9.0** | *(ausente)* | **não** |
| `Mvp24Hours.Core.Test` | net10.0 | latest | sim |
| `Mvp24Hours.Infrastructure.Caching.Test` | **net9.0** | latest | **não** |
| `Mvp24Hours.Infrastructure.Cqrs.Test` | net10.0 | latest | sim |
| `Mvp24Hours.Infrastructure.CronJob.Test` | net10.0 | latest | sim |
| `Mvp24Hours.Infrastructure.Data.MongoDb.Test` | **net9.0** | **13.0** | **não** |
| `Mvp24Hours.Patterns.Test` | net10.0 | *(ausente)* | sim |
| `Mvp24Hours.WebAPI.Test` | net10.0 | *(ausente)* | sim |

### Divergências confirmadas (`net9.0`)

| Projeto | TFM atual | LangVersion | Ação prevista |
|---|---|---|---|
| `src/Tests/Mvp24Hours.Application.Test/Mvp24Hours.Application.Test.csproj` | net9.0 | ausente | → net10.0 (tarefa 3.4); decidir inclusão na `.sln` |
| `src/Tests/Mvp24Hours.Infrastructure.Caching.Test/Mvp24Hours.Infrastructure.Caching.Test.csproj` | net9.0 | latest | → net10.0 (tarefa 3.4); decidir inclusão na `.sln` |
| `src/Tests/Mvp24Hours.Infrastructure.Data.MongoDb.Test/Mvp24Hours.Infrastructure.Data.MongoDb.Test.csproj` | net9.0 | 13.0 | → net10.0 + LangVersion padrão (tarefa 3.4); decidir inclusão na `.sln` |

---

## Backups (4) — não referenciados na solução

| Arquivo | TargetFramework | LangVersion |
|---|---|---|
| `src/Mvp24Hours.Infrastructure/Mvp24Hours - Backup.Infrastructure.csproj` | netcoreapp3.1 | 9.0 |
| `src/Mvp24Hours.Infrastructure.Pipe/Mvp24Hours - Backup.Infrastructure.Pipe.csproj` | netcoreapp3.1 | 9.0 |
| `src/Mvp24Hours.Infrastructure.RabbitMQ/Mvp24Hours - Backup.Infrastructure.RabbitMQ.csproj` | netcoreapp3.1 | 9.0 |
| `src/Mvp24Hours.WebAPI/Mvp24Hours - Backup.WebAPI.csproj` | netcoreapp3.1 | 9.0 |

Remoção: tarefa **3.5**.

---

## Distribuição `LangVersion` (somente projetos ativos)

| LangVersion | Qtd | Onde |
|---|---|---|
| `12.0` | 18 | Toda a produção (12) + 6 testes |
| `latest` | 5 | Pipe.Test, Core.Test, Caching.Test, Cqrs.Test, CronJob.Test |
| *(ausente)* | 4 | Application.Test, RabbitMQ.Test, Patterns.Test, WebAPI.Test |
| `13.0` | 1 | Infrastructure.Data.MongoDb.Test |

No SDK .NET 10, `LangVersion` ausente ou `latest` equivale a **C# 14**. Os projetos com `12.0` / `13.0` ficam deliberadamente atrás do default do SDK.

---

## Checklist de consumo (Fases 3–4)

- [ ] 3.1 — `Directory.Build.props` com `net10.0` + `LangVersion=latest` (eliminar duplicatas abaixo)
- [ ] 3.4 — alinhar 3 projetos `net9.0` → `net10.0` (e avaliar `dotnet sln add` dos órfãos)
- [ ] 3.5 — deletar 4 backups `netcoreapp3.1`
- [ ] 4.x — inventário Nullable é a tarefa **1.3** (não misturar com este arquivo)
