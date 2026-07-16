# NU1510 — PackageReference redundantes (tarefa 7.2)

> Data: 15/07/2026 · Task ADO [#87306](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87306)

## Contexto

Com `FrameworkReference` `Microsoft.AspNetCore.App` (e pruning padrão do .NET 10), o NuGet emite **NU1510** quando há `PackageReference` direto a um pacote já fornecido pelo SDK/shared framework — a referência impede o prune e é desnecessária para a compilação do projeto.

Baseline (restore da solução): **17** avisos NU1510 únicos (6 em `Core` + 11 em `Infrastructure`).

## Removidos

### `Mvp24Hours.Core`

| PackageReference removido |
|---|
| `Microsoft.Extensions.Configuration.Binder` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` |
| `Microsoft.Extensions.Logging.Abstractions` |
| `Microsoft.Extensions.Logging.Configuration` |
| `Microsoft.Extensions.Options.DataAnnotations` |
| `System.Threading.RateLimiting` |

Mantidos: `AutoMapper`, `Newtonsoft.Json`, `FluentValidation`, `OpenTelemetry` + `FrameworkReference` AspNetCore.App.

### `Mvp24Hours.Infrastructure`

| PackageReference removido |
|---|
| `Microsoft.Extensions.Caching.Memory` |
| `Microsoft.Extensions.Configuration` |
| `Microsoft.Extensions.Configuration.Binder` |
| `Microsoft.Extensions.Configuration.FileExtensions` |
| `Microsoft.Extensions.Configuration.Json` |
| `Microsoft.Extensions.DependencyInjection` |
| `Microsoft.Extensions.Diagnostics.HealthChecks` |
| `Microsoft.Extensions.Hosting.Abstractions` |
| `Microsoft.Extensions.Http` |
| `Microsoft.Extensions.Logging.Abstractions` |

Mantidos (não cobertos / necessários): `Http.Polly`, `Http.Resilience`, `Polly`, clients (Sql/Npgsql/Redis/…), ServiceModel, secrets providers, etc.

## CPM (`Directory.Packages.props`)

Removidos `PackageVersion` órfãos (sem `PackageReference` restante na solução):

- `Microsoft.Extensions.Configuration.FileExtensions`
- `Microsoft.Extensions.Configuration.Json`
- `Microsoft.Extensions.Logging.Configuration`
- `Microsoft.Extensions.Options.DataAnnotations`
- `System.Threading.RateLimiting`

Demais `Microsoft.Extensions.*` permanecem — ainda referenciados por outros projetos (Caching, Cqrs, testes, Application, WebAPI, …).

## Residual intencional

| Projeto | Pacote | Motivo |
|---|---|---|
| `Mvp24Hours.Infrastructure` | `System.Security.Cryptography.Xml` | Pin de segurança da tarefa **7.1** (NU1903 GHSA-37gx / GHSA-w3x6). Consumidores sem AspNetCore.App precisam da versão patchada **10.0.10**. |

## Validação (15/07/2026)

- `dotnet restore src/Mvp24Hours.sln` → **1** NU1510 único (`Cryptography.Xml` no Infrastructure).
- `dotnet build src/Mvp24Hours.sln -c Debug` → **0** erro(s).
- `dotnet list … package --vulnerable --include-transitive` → sem pacotes vulneráveis (confirmado na 7.1; pin mantido).

## Docs

- https://learn.microsoft.com/nuget/reference/errors-and-warnings/nu1510
- https://learn.microsoft.com/dotnet/core/compatibility/sdk/8.0/implicit-package-references
