# Baseline de build/warnings — .NET 10 (v1)

> Gerado em 2026-07-15 09:59:58 -03:00  
> SDK: `10.0.301`  
> Comando: `dotnet build src/Mvp24Hours.sln -c <Config> -v:q --no-incremental -flp:LogFile=build-baseline-<config>.log;Verbosity=minimal`  
> Logs brutos (fora do git, cobertos por `*.log`): `build-baseline-debug.log`, `build-baseline-release.log`  
> Referência: [tasks-net10-v1.md](./tasks-net10-v1.md) §1.1 · ADO Task [#87254](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87254)

## Totais oficiais (MSBuild)

| Configuração | Erros | Avisos | Observação |
|---|---|---|---|
| Debug | 0 | 4235 | Contagem de linhas `warning` no file logger (= total MSBuild da auditoria) |
| Release | 0 | 4235 | Idêntico ao Debug nesta baseline |

> **Como reexecutar:** ao final de cada fase, repetir o comando acima (Debug e Release) e comparar a coluna "Avisos" e as tabelas abaixo com este arquivo. Objetivo da Fase 10: reduzir avisos a zero (ou residual documentado, ex. NU1900 de feed privado).

## Avisos por código (Debug = Release)

| Código | Ocorrências |
|---|---|
| CS8632 | 3275 |
| CS8604 | 219 |
| CS8625 | 151 |
| NU1903 | 102 |
| LOGGEN002 | 93 |
| CS8603 | 83 |
| CS0618 | 63 |
| NU1900 | 62 |
| NU1510 | 48 |
| CS8600 | 37 |
| CS8618 | 35 |
| CS8765 | 15 |
| CS8602 | 9 |
| SYSLIB0057 | 9 |
| CS8619 | 5 |
| CS8601 | 5 |
| ASPDEPR006 | 4 |
| CS0168 | 4 |
| CS8767 | 4 |
| CS0108 | 3 |
| CS1718 | 2 |
| SYSLIB0014 | 2 |
| SYSLIB0060 | 1 |
| xUnit1031 | 1 |
| CS8609 | 1 |
| CA2022 | 1 |
| CS0219 | 1 |
| **Total** | **4235** |

### Agrupamento por família (para acompanhamento)

| Família | Códigos | Total |
|---|---|---|
| Nullable fora de contexto | CS8632 | 3275 |
| Nullable real | CS8600, CS8601, CS8602, CS8603, CS8604, CS8618, CS8619, CS8625 | 544 |
| Nullable override/interface | CS8765, CS8767, CS8609 | 20 |
| API obsoleta (lib/terceiros) | CS0618 | 63 |
| API obsoleta (BCL) | SYSLIB0057, SYSLIB0014, SYSLIB0060 | 12 |
| NuGet vulnerabilidade | NU1903 | 102 |
| NuGet audit feed | NU1900 | 62 |
| PackageReference redundante | NU1510 | 48 |
| LoggerMessage | LOGGEN002 | 93 |
| Qualidade diversa | CS0168, CS0219, CS1718, CS0108, CA2022, xUnit1031, ASPDEPR006 | 16 |

## Avisos por projeto (Debug = Release)

| Projeto | Ocorrências |
|---|---|
| Mvp24Hours.Infrastructure | 959 |
| Mvp24Hours.Infrastructure.RabbitMQ | 869 |
| Mvp24Hours.Infrastructure.Pipe | 562 |
| Mvp24Hours.Core | 383 |
| Mvp24Hours.WebAPI | 376 |
| Mvp24Hours.Infrastructure.Data.EFCore | 318 |
| Mvp24Hours.Infrastructure.Caching | 305 |
| Mvp24Hours.Application | 157 |
| Mvp24Hours.Infrastructure.Data.MongoDb | 136 |
| Mvp24Hours.Core.Test | 42 |
| Mvp24Hours.Infrastructure.Cqrs.Test | 16 |
| Mvp24Hours.Infrastructure.Cqrs | 14 |
| Mvp24Hours.Application.RabbitMQ.Test | 11 |
| Mvp24Hours.Application.Redis.Test | 10 |
| Mvp24Hours.Infrastructure.Caching.Redis | 9 |
| Mvp24Hours.Infrastructure.CronJob | 9 |
| Mvp24Hours.Application.MongoDb.Test | 8 |
| Mvp24Hours.Infrastructure.CronJob.Test | 8 |
| Mvp24Hours.Application.Integration.Test | 7 |
| Mvp24Hours.Application.MySql.Test | 6 |
| Mvp24Hours.WebAPI.Test | 6 |
| Mvp24Hours.Application.Pipe.Test | 6 |
| Mvp24Hours.Application.SQLServer.Test | 6 |
| Mvp24Hours.Application.PostgreSql.Test | 6 |
| Mvp24Hours.Patterns.Test | 6 |
| **Total** | **4235** |

## Notas

1. Contagens vêm do **file logger** do MSBuild (`-flp`). Redirecionar `stdout+stderr` (`2>&1` / `Tee-Object`) duplica cada aviso no log e não deve ser usado para métricas.
2. O total **4235** inclui **47** emissões repetidas com texto idêntico no próprio MSBuild (assinaturas únicas ≈ 4188); manter o total do MSBuild como métrica oficial. Contagens NuGet (`NU1903`/`NU1900`/`NU1510`) ficam um pouco acima do resumo aproximado da auditoria por essas reemissões.
3. Códigos adicionais vs. resumo inicial da auditoria: `LOGGEN002` (93) e `ASPDEPR006` (4) — incluir no acompanhamento das fases.
4. Reexecução esperada ao final de cada fase (1–10); atualizar este arquivo (ou anexar delta) quando houver redução material.

