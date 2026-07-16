# Decisão CPM — Central Package Management (tarefa 3.3)

> Data: 15/07/2026 · Task ADO [#87283](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87283)

## Decisão

**Adotar** Central Package Management nesta rodada.

## Motivos

- 238 `PackageReference` espalhados em 28 `.csproj` ativos, com **11 pacotes em versões conflitantes** (ex.: `coverlet.collector` 6.0.x vs 10.0.1; `xunit` 2.6.6 / 2.9.2 / 2.9.3; `Microsoft.Extensions.*` 10.0.1 vs 10.0.9).
- A Fase 7 (vulnerabilidades NU1903 / bumps coordenados) fica drasticamente mais simples com um único `PackageVersion`.
- Alinha com a padronização já feita em 3.1 (`Directory.Build.props`).

## O que foi feito

| Artefato | Papel |
|---|---|
| [`src/Directory.Packages.props`](../src/Directory.Packages.props) | `ManagePackageVersionsCentrally=true` + 81 `PackageVersion` |
| 28 `.csproj` ativos | `PackageReference` sem atributo `Version` (mantidos `PrivateAssets` / `IncludeAssets`) |
| [`NuGet.Config`](../NuGet.Config) (raiz) | Fonte única `nuget.org` + package source mapping (elimina NU1507; evita NU1900 do feed privado inacessível no restore deste repo) |

**Não migrados** (escopo da 3.5): os 4 `Mvp24Hours - Backup.*.csproj`.

## Critério de unificação de conflitos

Preferência por versão **majoritária** entre os projetos da solution; empate → maior version. Ajuste manual: `Microsoft.Extensions.Options` forceado para **10.0.9** (alinhamento com o restante de `Microsoft.Extensions.*` 10.0.9, apesar de 2 testes órfãos ainda pedirem 10.0.1).

| Pacote | Antes | Central |
|---|---|---|
| coverlet.collector | 10.0.1 / 6.0.0 / 6.0.2 / 6.0.4 | 10.0.1 |
| FluentAssertions | 8.10.0 / 8.3.0 / 7.0.0 / 6.12.0 | 8.10.0 |
| Microsoft.Extensions.Caching.Memory | 10.0.9 / 10.0.1 | 10.0.9 |
| Microsoft.Extensions.DependencyInjection | 10.0.9 / 10.0.1 | 10.0.9 |
| Microsoft.Extensions.Logging | 10.0.9 / 10.0.1 | 10.0.9 |
| Microsoft.Extensions.Logging.Abstractions | 10.0.9 / 10.0.1 | 10.0.9 |
| Microsoft.Extensions.Options | 10.0.9 / 10.0.1 | **10.0.9** |
| Microsoft.NET.Test.Sdk | 18.7.0 / 17.12.0 / 17.9.0 | 18.7.0 |
| Moq | 4.20.72 / 4.20.70 | 4.20.72 |
| xunit | 2.9.3 / 2.9.2 / 2.6.6 | 2.9.3 |
| xunit.runner.visualstudio | 3.1.5 / 3.0.0 / 2.8.2 / 2.5.6 | 3.1.5 |

## Observações

- `CentralPackageTransitivePinningEnabled` ficou **false**: com `true`, `RabbitMQ.Client` 6.8.1 (direto em `Infrastructure.RabbitMQ`) forçava downgrade contra `MassTransit.RabbitMQ` (≥ 7.2.1) → NU1109. Revisar pinagem transitiva na Fase 7 ao subir `RabbitMQ.Client`.
- Overrides pontuais: usar `VersionOverride` no `.csproj` apenas com justificativa explícita.
- Build pós-adoção (`dotnet build src/Mvp24Hours.sln -c Debug`): **0 erro(s)**; avisos CS* no mesmo patamar pós-3.1 (~2400).

## Follow-ups

- 3.4: alinhar testes órfãos `net9.0` (já consumirão as versões centrais modernas).
- 7.1: **feito** — pin `System.Security.Cryptography.Xml` 10.0.10 (`PackageVersion` + `PackageReference` em Infrastructure). `CentralPackageTransitivePinningEnabled` segue `false` (NU1109 RabbitMQ); reabrir ao bump de `RabbitMQ.Client` ≥7.2.1.
- 7.2: remoção NU1510 (pacotes redundantes) — **exceto** o pin de `Cryptography.Xml` em Infrastructure (necessário enquanto WCF não atualizar a transitiva).
