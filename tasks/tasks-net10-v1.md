# Tasks — Estabilização pós-migração .NET 10 (warnings, testes e padrões C#)

> Gerado em 15/07/2026 a partir de uma auditoria real da solução (`dotnet build src/Mvp24Hours.sln -c Debug`).
> **Baseline atual:** build com **0 erro(s)** e **4235 aviso(s)** (MSBuild, pt-BR). Distribuição aproximada dos avisos únicos por código:
>
> | Código | Ocorrências | Significado resumido |
> |---|---|---|
> | CS8632 | ~3275 | Anotação `?` usada fora de contexto `#nullable` (Nullable não habilitado no `.csproj`) |
> | CS8604/CS8603/CS8600/CS8602/CS8601/CS8618/CS8619/CS8625 | ~543 | Avisos reais de Nullable Reference Types (possível null) |
> | CS8765/CS8767/CS8609 | 20 | Incompatibilidade de nulidade em overrides/implementações de interface |
> | NU1903 | 84 | Vulnerabilidade conhecida em `System.Security.Cryptography.Xml` 10.0.0 |
> | NU1900 | 50 | Falha ao consultar índice de vulnerabilidades (feed privado inacessível) |
> | NU1510 | 32 | `PackageReference` redundante (não será "podado") |
> | CS0618 | 63 | Uso de API obsoleta (própria da lib ou de terceiros) |
> | SYSLIB0057/SYSLIB0014/SYSLIB0060 | 12 | APIs do BCL marcadas obsoletas (certificados, `ServicePointManager`, `Rfc2898DeriveBytes`) |
> | CS0168/CS0219/CS1718/CS0108/CA2022/xUnit1031 | 12 | Qualidade de código diversa (variável não usada, comparação redundante, hiding, leitura de stream, teste assíncrono bloqueante) |
>
> **Achados críticos adicionais:**
> - `.github/workflows/ci.yml` e `.github/workflows/codeql-analysis.yml` já usam SDK **`10.0.x`** (tarefas 2.1 e 2.2).
> - O job `code-quality` do CI executa `dotnet build /p:TreatWarningsAsErrors=true`, ou seja, **o CI falhará automaticamente** enquanto os avisos acima não forem tratados.
> - 17 projetos (8 de produção + 9 de teste) não têm `<Nullable>enable</Nullable>`, apesar de usarem `?` no código — causa raiz do CS8632.
> - 3 projetos de teste ainda estão em `net9.0` (divergente do restante, já em `net10.0`).
> - `LangVersion` está inconsistente entre projetos (`12.0`, `13.0`, `latest`, ausente) — não há um padrão de linguagem C# definido para a solução.
> - Existem 4 arquivos de projeto duplicados "`Mvp24Hours - Backup.*.csproj`" (não referenciados na `.sln`) e um `build-webapi-errors.txt` de 554KB versionado por engano na raiz do repositório.
> - Testes de integração usam Testcontainers (Docker) para MongoDB, SQL Server, MySQL, PostgreSQL, Redis e RabbitMQ — não há Docker disponível neste ambiente de análise, o que deve ser considerado ao planejar a execução completa da suíte.
>
> **Convenção de status:** `[ ]` pendente · `[x]` concluído · `[~]` em andamento/bloqueado (explicar no PR).

---

## FASE 1 — Diagnóstico e Baseline

> **ADO:** US [#87253](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87253) · Feature [#87242](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87242)

[x] 1.1 - Gerar e versionar o baseline de build/warnings da solução — Task [#87254](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87254)
- Executar `dotnet build src/Mvp24Hours.sln -c Debug -v:m` (e também `-c Release`) direcionando a saída para um log temporário (ex.: `build-baseline.log`, fora do controle de versão — ver tarefa 8.2). Extrair a contagem de avisos por código (`CSxxxx`, `NUxxxx`, `SYSLIBxxxx`, `CAxxxx`, `xUnitxxxx`) e por projeto, para servir de métrica objetiva de "warnings restantes" ao longo das próximas fases. Esse baseline deve ser reexecutado ao final de cada fase para medir o progresso (redução de avisos) e confirmar que nenhum erro novo foi introduzido.
- **Concluído (15/07/2026):** Debug e Release com **0 erro(s)** e **4235 aviso(s)** cada. Resumo versionado em [`tasks/baseline-net10-v1.md`](./baseline-net10-v1.md) (+ [`baseline-net10-v1.json`](./baseline-net10-v1.json)). Logs brutos locais (gitignored via `*.log`): `build-baseline-debug.log`, `build-baseline-release.log` — regenerar com file logger (`-flp`), não com `2>&1`/`Tee-Object` (duplica avisos).
- `src/Mvp24Hours.sln`, todos os `*.csproj` em `src/**`
- https://learn.microsoft.com/dotnet/core/tools/dotnet-build
- https://learn.microsoft.com/visualstudio/msbuild/msbuild-command-line-reference

[x] 1.2 - Inventariar divergências de `TargetFramework` e `LangVersion` entre projetos — Task [#87255](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87255)
- Percorrer todos os `.csproj` da solução (produção e testes) e documentar em uma planilha/checklist qual `TargetFramework` e `LangVersion` cada um usa hoje. Confirmar que a migração para `net10.0` está incompleta (3 projetos de teste ainda em `net9.0`) e que `LangVersion` varia entre `12.0`, `13.0`, `latest` e ausente (default do SDK). Este inventário alimenta diretamente as fases 3 e 4.
- **Concluído (15/07/2026):** 28 projetos ativos inventariados — produção 12/12 em `net10.0`/`12.0`; testes 13×`net10.0` + **3×`net9.0`**. `LangVersion`: 18×`12.0`, 5×`latest`, 4×ausente, 1×`13.0`. Achado extra: os 3 projetos `net9.0` **não estão na `.sln`**. Inventário versionado em [`inventory-tfm-langversion-net10-v1.md`](./inventory-tfm-langversion-net10-v1.md) (+ [`.json`](./inventory-tfm-langversion-net10-v1.json)).
- `src/Tests/Mvp24Hours.Application.Test/Mvp24Hours.Application.Test.csproj` (net9.0), `src/Tests/Mvp24Hours.Infrastructure.Caching.Test/Mvp24Hours.Infrastructure.Caching.Test.csproj` (net9.0, LangVersion=latest), `src/Tests/Mvp24Hours.Infrastructure.Data.MongoDb.Test/Mvp24Hours.Infrastructure.Data.MongoDb.Test.csproj` (net9.0, LangVersion=13.0)
- https://learn.microsoft.com/dotnet/standard/frameworks
- https://learn.microsoft.com/dotnet/csharp/language-reference/configure-language-version

[x] 1.3 - Mapear todos os projetos sem `<Nullable>enable</Nullable>` — Task [#87256](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87256)
- Confirmar, por leitura de cada `.csproj`, a lista completa de projetos que usam sintaxe de nullable reference types (`?`) no código-fonte mas não possuem `<Nullable>enable</Nullable>` na `PropertyGroup` — esta é a causa raiz de praticamente 80% dos avisos atuais (CS8632). Produzir a lista definitiva que será usada como checklist na Fase 4.
- **Concluído (15/07/2026):** 28 ativos → **11** com Nullable enable · **17** sem (8 produção + 9 teste). CS8632 (3275) 100% em 7 produção + `Application.RabbitMQ.Test`. `Caching.Redis` e 8 testes sem Nullable não emitem CS8632 hoje, mas seguem no checklist 4.1/4.2. Inventário: [`inventory-nullable-net10-v1.md`](./inventory-nullable-net10-v1.md) (+ [`.json`](./inventory-nullable-net10-v1.json)).
- Projetos de produção sem Nullable: `src/Mvp24Hours.Infrastructure/Mvp24Hours.Infrastructure.csproj`, `src/Mvp24Hours.Infrastructure.Caching/Mvp24Hours.Infrastructure.Caching.csproj`, `src/Mvp24Hours.Infrastructure.Caching.Redis/Mvp24Hours.Infrastructure.Caching.Redis.csproj`, `src/Mvp24Hours.Infrastructure.Data.EFCore/Mvp24Hours.Infrastructure.Data.EFCore.csproj`, `src/Mvp24Hours.Infrastructure.Data.MongoDb/Mvp24Hours.Infrastructure.Data.MongoDb.csproj`, `src/Mvp24Hours.Infrastructure.Pipe/Mvp24Hours.Infrastructure.Pipe.csproj`, `src/Mvp24Hours.Infrastructure.RabbitMQ/Mvp24Hours.Infrastructure.RabbitMQ.csproj`, `src/Mvp24Hours.WebAPI/Mvp24Hours.WebAPI.csproj`; Projetos de teste sem Nullable: `src/Tests/Mvp24Hours.Application.MongoDb.Test`, `Mvp24Hours.Application.MySql.Test`, `Mvp24Hours.Application.Pipe.Test`, `Mvp24Hours.Application.PostgreSql.Test`, `Mvp24Hours.Application.RabbitMQ.Test`, `Mvp24Hours.Application.Redis.Test`, `Mvp24Hours.Application.SQLServer.Test`, `Mvp24Hours.Patterns.Test`, `Mvp24Hours.WebAPI.Test`
- https://learn.microsoft.com/dotnet/csharp/nullable-references

---

## FASE 2 — Desbloqueio do CI/CD (prioridade máxima)

> **ADO:** US [#87269](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87269) · Feature [#87242](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87242)

[x] 2.1 - Atualizar `DOTNET_VERSION` do workflow de CI para .NET 10 — Task [#87278](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87278)
- No workflow `ci.yml`, alterar a variável de ambiente `DOTNET_VERSION` de `'8.0.x'` para `'10.0.x'` (job `build-and-test`, `code-quality` e `package` consomem essa variável). Sem essa correção, o pipeline instala o SDK 8, que não é capaz de compilar projetos com `<TargetFramework>net10.0</TargetFramework>`, e todo o CI está quebrado desde o commit de migração (`fb91f2d`).
- **Concluído (15/07/2026):** `DOTNET_VERSION` em `.github/workflows/ci.yml` alterado de `'8.0.x'` para `'10.0.x'`. Os jobs `build-and-test`, `code-quality`, `package` e `security-scan` passam a instalar o SDK .NET 10 via `${{ env.DOTNET_VERSION }}`.
- `.github/workflows/ci.yml` (linha 10: `DOTNET_VERSION: '10.0.x'`)
- https://github.com/actions/setup-dotnet
- https://dotnet.microsoft.com/download/dotnet/10.0

[x] 2.2 - Atualizar `dotnet-version` do workflow `codeql-analysis.yml` para .NET 10 — Task [#87279](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87279)
- Mesma correção da tarefa 2.1, aplicada ao workflow de análise estática CodeQL, que hoje também fixa `dotnet-version: '8.0.x'`.
- **Concluído (15/07/2026):** `dotnet-version` em `.github/workflows/codeql-analysis.yml` alterado de `'8.0.x'` para `'10.0.x'`. O job `analyze` passa a instalar o SDK .NET 10 antes do `dotnet restore`/`dotnet build` usado pelo CodeQL.
- `.github/workflows/codeql-analysis.yml` (linha 32: `dotnet-version: '10.0.x'`)
- https://docs.github.com/code-security/code-scanning/creating-an-advanced-setup-for-code-scanning
- https://github.com/actions/setup-dotnet

[x] 2.3 - Validar (sem alterar ainda) o gate `TreatWarningsAsErrors` do job `code-quality` — Task [#87280](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87280)
- Ler o step `🔍 Run static code analysis` (`dotnet build --configuration Release /p:TreatWarningsAsErrors=true`) e confirmar que ele falhará enquanto as Fases 4–6 não estiverem concluídas. Documentar essa dependência no PR desta fase para justificar por que o CI só ficará totalmente verde ao final da Fase 10 (não remover o `TreatWarningsAsErrors`; ele é o objetivo final, não o problema).
- **Concluído (15/07/2026):** Gate **mantido** sem alteração. Build local com `/p:TreatWarningsAsErrors=true` → **FALHA** (**0** avisos / **83** erros): restore derruba só com NU1903/NU1900/NU1510; os ~4000 CS* do baseline nem chegam a ser emitidos. CI de PR só verde após Fases 3–7 + fechamento na Fase 10. Evidência e texto sugerido para o PR: [`gate-treatwarningsaserrors-net10-v1.md`](./gate-treatwarningsaserrors-net10-v1.md).
- `.github/workflows/ci.yml` (linha 79)
- https://learn.microsoft.com/visualstudio/msbuild/msbuild-warnings-as-errors

---

## FASE 3 — Fundação: padronização da linguagem C# e do build

> **ADO:** US [#87270](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87270) · Feature [#87242](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87242)

[x] 3.1 - Criar `Directory.Build.props` na raiz de `src/` centralizando propriedades comuns — Task [#87281](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87281)
- Criar `src/Directory.Build.props` definindo de forma única, para todos os projetos por herança automática do MSBuild: `<TargetFramework>net10.0</TargetFramework>` (ou `net10.0` como padrão, com overrides pontuais se necessário), `<LangVersion>latest</LangVersion>` (equivalente a C# 14 no SDK 10), `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<AnalysisLevel>latest</AnalysisLevel>` e `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>`. Depois de criado, remover as duplicações equivalentes de cada `.csproj` individual (mantendo apenas overrides justificados). Isso elimina a causa raiz da inconsistência encontrada na Fase 1 e evita que ela se repita em novos projetos.
- **Concluído (15/07/2026):** Criado [`src/Directory.Build.props`](../src/Directory.Build.props) com TFM/`LangVersion`/`Nullable`/`ImplicitUsings`/`AnalysisLevel`/`EnforceCodeStyleInBuild`. Removidas as propriedades equivalentes dos 28 `.csproj` ativos. Overrides justificados mantidos: `ImplicitUsings=disable` em `Infrastructure.CronJob`; `TargetFramework=net9.0` nos 3 testes órfãos (escopo da 3.4). Build `Debug` da solução: **0 erro(s)** / **2440 aviso(s)** (queda vs baseline 4235 — CS8632 eliminado pela herança de Nullable; 4.1/4.2 passam a herdar Nullable automaticamente).
- Todos os `*.csproj` em `src/**` (33 arquivos), especialmente os listados nas tarefas 1.2 e 1.3
- https://learn.microsoft.com/visualstudio/msbuild/customize-your-build#directorybuildprops-and-directorybuildtargets
- https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-14
- https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10/overview

[x] 3.2 - Criar `.editorconfig` na raiz do repositório com convenções de estilo C# — Task [#87282](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87282)
- Adicionar um `.editorconfig` cobrindo: convenções de nomenclatura (PascalCase/camelCase), preferências de `var` vs. tipo explícito, uso de expression-bodied members, ordenação de `using`, preferência por `file-scoped namespaces`, `primary constructors` e `collection expressions` (recursos consolidados em C# 12–14), além de severidade (`warning`/`suggestion`) para as regras de análise de nulidade e de qualidade (`CAxxxx`) já habilitadas via `AnalysisLevel`. Isso viabiliza `dotnet format --verify-no-changes` (já usado no CI) de forma consistente e documenta formalmente os "padrões da linguagem C#" pedidos.
- **Concluído (15/07/2026):** Criado [`.editorconfig`](../.editorconfig) na raiz. Base: CONTRIBUTING.md (Allman braces, 4 espaços, `_camelCase` em campos privados, `I` + PascalCase em interfaces). Preferências modernas (`file_scoped`, primary constructors, collection expressions) e nomenclatura em **suggestion** para não explodir o build com `EnforceCodeStyleInBuild` antes do alinhamento na Fase 10; nulidade (CS86xx/CS876x) e vulnerabilidades NuGet (NU190x) em **warning**; CA de qualidade misto (CA2022=warning, demais suggestion). Formatação continua gated por `dotnet format` (IDE0055=suggestion).
- `CONTRIBUTING.md` (seção "C# Style Guide", linha 201) como base das convenções já documentadas informalmente
- https://learn.microsoft.com/dotnet/fundamentals/code-analysis/code-style-rule-options
- https://learn.microsoft.com/dotnet/fundamentals/code-analysis/overview
- https://learn.microsoft.com/dotnet/core/tools/dotnet-format

[x] 3.3 - Avaliar e, se aprovado, adotar Central Package Management (`Directory.Packages.props`) — Task [#87283](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87283)
- Analisar a viabilidade de migrar todos os `PackageReference` (hoje espalhados em 33 `.csproj`, muitos com a mesma versão `10.0.9` repetida) para um único `src/Directory.Packages.props` com `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`. Isso reduz drasticamente o risco de divergência de versões entre projetos (como a já observada em `LangVersion`) e facilita a correção coordenada de vulnerabilidades (Fase 7). Caso não seja aprovado nesta rodada, registrar a decisão e revisitar em uma v2 do plano.
- **Concluído (15/07/2026):** **CPM adotado.** Criado [`src/Directory.Packages.props`](../src/Directory.Packages.props) (81 `PackageVersion`) + [`NuGet.Config`](../NuGet.Config) (nuget.org + source mapping). Removido `Version=` dos 28 `.csproj` ativos. Unificados 11 conflitos de versão (detalhe em [`cpm-decision-net10-v1.md`](./cpm-decision-net10-v1.md)). `CentralPackageTransitivePinningEnabled=false` (NU1109 com RabbitMQ.Client vs MassTransit). Build Debug: **0 erro(s)**.
- Todos os `*.csproj` em `src/**`
- https://learn.microsoft.com/nuget/consume-packages/central-package-management

[x] 3.4 - Alinhar os 3 projetos de teste ainda em `net9.0` para `net10.0` — Task [#87284](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87284)
- Atualizar `<TargetFramework>` de `net9.0` para `net10.0` (ou remover a propriedade se herdada do `Directory.Build.props` da tarefa 3.1) nos projetos que ficaram fora da migração original, e ajustar `LangVersion` para o padrão definido em 3.1. Reexecutar a build desses projetos isoladamente para confirmar compatibilidade de pacotes (ex.: `coverlet.collector`, `Microsoft.NET.Test.Sdk`, `xunit`) com `net10.0`.
- **Concluído (15/07/2026):** Removido override `<TargetFramework>net9.0</TargetFramework>` nos 3 `.csproj` (herdam `net10.0` + `LangVersion=latest` do `Directory.Build.props`). Inclusos na `Mvp24Hours.sln` (pasta Tests) — deixam de ser órfãos. Build isolado Debug: **0 erro(s)** nos três. Ajuste pontual FA 8.x em `Infrastructure.Data.MongoDb.Test`: `BeGreaterOrEqualTo`/`BeLessOrEqualTo` → `BeGreaterThanOrEqualTo`/`BeLessThanOrEqualTo` (4 sites).
- `src/Tests/Mvp24Hours.Application.Test/Mvp24Hours.Application.Test.csproj`, `src/Tests/Mvp24Hours.Infrastructure.Caching.Test/Mvp24Hours.Infrastructure.Caching.Test.csproj`, `src/Tests/Mvp24Hours.Infrastructure.Data.MongoDb.Test/Mvp24Hours.Infrastructure.Data.MongoDb.Test.csproj`
- https://learn.microsoft.com/dotnet/core/compatibility/10.0

[x] 3.5 - Remover os `.csproj` de backup duplicados deixados pela migração — Task [#87285](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87285)
- Excluir os 4 arquivos `Mvp24Hours - Backup.*.csproj` (cópias antigas pré-migração, não referenciadas em nenhuma `.sln` e que apenas geram confusão/risco de build acidental via wildcard) e confirmar, com `dotnet sln list` e uma nova execução de `dotnet build`, que nada depende deles.
- **Concluído (15/07/2026):** Removidos os 4 `Mvp24Hours - Backup.*.csproj` (`Infrastructure`, `Infrastructure.Pipe`, `Infrastructure.RabbitMQ`, `WebAPI`). Confirmado via `dotnet sln list`: nenhum Backup na solução (28 projetos ativos). Build Debug: **0 erro(s)**.
- `src/Mvp24Hours.Infrastructure/Mvp24Hours - Backup.Infrastructure.csproj`, `src/Mvp24Hours.Infrastructure.Pipe/Mvp24Hours - Backup.Infrastructure.Pipe.csproj`, `src/Mvp24Hours.Infrastructure.RabbitMQ/Mvp24Hours - Backup.Infrastructure.RabbitMQ.csproj`, `src/Mvp24Hours.WebAPI/Mvp24Hours - Backup.WebAPI.csproj`
- https://learn.microsoft.com/dotnet/core/tools/dotnet-sln

---

## FASE 4 — Habilitar e corrigir Nullable Reference Types (maior volume de warnings)

> **ADO:** US [#87271](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87271) · Feature [#87242](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87242)

[x] 4.1 - Habilitar `<Nullable>enable</Nullable>` nos 8 projetos de produção listados na tarefa 1.3 — Task [#87294](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87294)
- Para cada projeto (`Mvp24Hours.Infrastructure`, `Infrastructure.Caching`, `Infrastructure.Caching.Redis`, `Infrastructure.Data.EFCore`, `Infrastructure.Data.MongoDb`, `Infrastructure.Pipe`, `Infrastructure.RabbitMQ`, `WebAPI`), adicionar/herdar `<Nullable>enable</Nullable>` e recompilar isoladamente. Isso converte os CS8632 (sintaxe fora de contexto) em avisos reais de nulidade (CS8600/CS8602/CS8603/CS8604/CS8618/CS8619/CS8625) que precisam ser corrigidos nas tarefas 4.3–4.5 — portanto, executar esta tarefa projeto por projeto (não todos de uma vez) para manter o volume de correções gerenciável e revisável em PRs pequenos.
- **Concluído (15/07/2026):** os 8 projetos de produção já estão com Nullable habilitado por herança de `src/Directory.Build.props` (`<Nullable>enable</Nullable>`) e sem override local para desabilitar. Rebuild isolado em `Debug` executado projeto a projeto (`Mvp24Hours.Infrastructure`, `Infrastructure.Caching`, `Infrastructure.Caching.Redis`, `Infrastructure.Data.EFCore`, `Infrastructure.Data.MongoDb`, `Infrastructure.Pipe`, `Infrastructure.RabbitMQ`, `WebAPI`) com sucesso (`=== ALL BUILDS OK ===`).
- Ver lista completa de arquivos na tarefa 1.3
- https://learn.microsoft.com/dotnet/csharp/nullable-references
- https://devblogs.microsoft.com/dotnet/nullable-reference-types-in-csharp/

[x] 4.2 - Habilitar `<Nullable>enable</Nullable>` nos 9 projetos de teste listados na tarefa 1.3 — Task [#87295](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87295)
- Mesmo procedimento da tarefa 4.1, aplicado aos projetos de teste (`Mvp24Hours.Application.MongoDb.Test`, `Application.MySql.Test`, `Application.Pipe.Test`, `Application.PostgreSql.Test`, `Application.RabbitMQ.Test`, `Application.Redis.Test`, `Application.SQLServer.Test`, `Patterns.Test`, `WebAPI.Test`). Priorizar após 4.1, pois muitos desses testes referenciam os projetos de produção corrigidos e podem expor avisos adicionais nas assinaturas usadas.
- **Concluído (15/07/2026):** os 9 projetos de teste já estão com Nullable habilitado por herança de `src/Directory.Build.props` (`<Nullable>enable</Nullable>`) e sem override local. Confirmado via `dotnet msbuild -getProperty:Nullable` → `enable` em todos. Rebuild isolado em `Debug` projeto a projeto com sucesso (`=== ALL BUILDS OK ===`; 0 erro(s) em cada um).
- Ver lista completa de arquivos na tarefa 1.3
- https://learn.microsoft.com/dotnet/csharp/nullable-references

[x] 4.3 - Corrigir avisos CS8618 (propriedades/campos não-anuláveis sem valor ao saltar do construtor) em `Mvp24Hours.Core` — Task [#87296](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87296)
- Revisar cada ocorrência e decidir, caso a caso, entre: (a) inicializar a propriedade/campo no construtor, (b) declarar como anulável (`?`) quando o valor pode legitimamente não existir, ou (c) aplicar o modificador `required` (C# 11+) para forçar inicialização pelo consumidor. Atenção especial a `IBulkOperationsAsync.cs` (múltiplas propriedades de resultado/erro) e `Specification.cs`/`CompositeSpecifications.cs` (campos de expressão compilada).
- **Concluído (15/07/2026):** CS8618 em `Mvp24Hours.Core` reduzido de **22** ocorrências únicas para **0**. Estratégia: (b) `?` para membros opcionais/lazy (`ErrorMessage`, `ProgressCallback`, `Value`/`ValueExpression`, `_compiledExpression`/`_combinedExpression`/`_negatedExpression`, `_iv`/`_blindIndexSalt`, `_getter`/`_setter`, opções de tenant/paginação/IV/salt); (c) `required` para `SetPropertyCall.Property` e `EncryptionOptions.Key`. Build Debug do Core: **0 erro(s)** / **0 CS8618**.
- `src/Mvp24Hours.Core/Contract/Data/Async/IBulkOperationsAsync.cs` (linhas 55, 92, 360, 365, 370), `src/Mvp24Hours.Core/Domain/Specifications/Specification.cs` (linha 36), `src/Mvp24Hours.Core/Domain/Specifications/CompositeSpecifications.cs` (linhas 29, 64, 97)
- https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/required
- https://learn.microsoft.com/dotnet/csharp/nullable-references#nonnullable-reference-not-initialized

[x] 4.4 - Corrigir avisos CS8600/CS8602/CS8603/CS8604/CS8601/CS8619/CS8625 no restante da solução (Core, Infrastructure.*, WebAPI) — Task [#87297](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87297)
- Após habilitar Nullable nas Fases 4.1/4.2, tratar sistematicamente as conversões/atribuições de valores potencialmente nulos identificadas pelo compilador: adicionar checagens (`ArgumentNullException.ThrowIfNull`, operadores `??`/`?.`/`!`) ou ajustar assinaturas para refletir a real anulabilidade dos parâmetros/retornos. Começar pelos hotspots já identificados: `ConvertExtensions.cs` e `TelemetryHelper.cs` em `Mvp24Hours.Core` concentram a maioria dos CS8600 conhecidos hoje; após habilitar nullable nos 8 projetos de produção, os módulos com maior densidade histórica de anotações `?` (e portanto maior probabilidade de novos avisos reais) são `Infrastructure/Http`, `Infrastructure/BackgroundJobs`, `Infrastructure/Email`, `Infrastructure/FileStorage`, `Infrastructure.RabbitMQ/Observability` e `Infrastructure.RabbitMQ/Pipeline`.
- **Concluído (15/07/2026):** escopo produção (`Core`, `Infrastructure.*`, `WebAPI`) em **0** avisos dos códigos alvo (antes ~1071 no escopo). Estratégia: `T?` em params/retornos opcionais, `ThrowIfNull` para obrigatórios, `??`/`?.` e `!` pontual; auxiliares Core (`[NotNullWhen(true)]` em `AnySafe`, `GetById*` → `TEntity?`, `ModifiedBy` → `string?`). Build Debug da solução: **0 erro(s)** / **0** CS8600–04/8619/8625 nos 9 projetos do escopo. Residual fora do escopo 4.4: `Application` (~81) e testes (~562). CS8765/CS8767/CS8609 ficam na 4.5.
- `src/Mvp24Hours.Core/Extensions/ConvertExtensions.cs` (linha 45), `src/Mvp24Hours.Core/Helpers/TelemetryHelper.cs` (linhas 103–259), diretórios `src/Mvp24Hours.Infrastructure/Http/**`, `src/Mvp24Hours.Infrastructure/BackgroundJobs/**`, `src/Mvp24Hours.Infrastructure.RabbitMQ/Observability/**`, `src/Mvp24Hours.Infrastructure.RabbitMQ/Pipeline/**`
- https://learn.microsoft.com/dotnet/api/system.argumentnullexception.throwifnull
- https://learn.microsoft.com/dotnet/csharp/language-reference/operators/null-coalescing-operator

[x] 4.5 - Corrigir CS8765/CS8767/CS8609 (nulidade divergente em overrides e implementações de interface) — Task [#87298](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87298)
- Ajustar as assinaturas de métodos que sobrescrevem membros de classes base (`object.Equals`) ou implementam interfaces genéricas (`IEquatable<T>.Equals`, `IComparable<T>.CompareTo`, `IValueProvider.SetValue`) para que a anotação de nulidade do parâmetro corresponda exatamente à do membro original — tipicamente aceitando `T?`/`object?` em vez de `T`/`object`.
- **Concluído (15/07/2026):** CS8765/CS8767/CS8609 → **0** na solução. Ajustes: `ReadJson(..., TId? existingValue)` nos 4 converters tipados; `Enumeration.Equals`/`CompareTo`/`object.Equals` com `?`; `IValueProvider.SetValue(..., object?)`; `CommandLoggingInterceptor.ScalarExecuted*` com `object? result`. Já ok na 4.4: `ValueObjectConverter`, `EntityBase`. Testes Enumeration: **54/54**. Build Debug: **0 erro(s)** / **0** CS8765/CS8767/CS8609.
- `src/Mvp24Hours.Core/Converters/EntityIdNewtonsoftConverters.cs` (métodos `ReadJson`/`WriteJson`, várias sobrecargas), `src/Mvp24Hours.Core/Converters/ValueObjectConverter.cs` (linhas 28, 33), `src/Mvp24Hours.Core/Domain/Enumerations/Enumeration.cs` (linhas 186, 194, 224), `src/Mvp24Hours.Core/Domain/Entities/EntityBase.cs` (linhas 74, 80), `src/Mvp24Hours.Core/Serialization/Json/AnonymousTypeContractResolver.cs` (linha 66)
- https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/nullable-reference-types#overrides-and-interface-implementation

---

## FASE 5 — Modernização de APIs obsoletas (CS0618 / SYSLIB0xxx)

> **ADO:** US [#87272](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87272) · Feature [#87242](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87242)

[x] 5.1 - Migrar `CircuitBreaker<T>` (próprio, obsoleto) para `NativeResiliencePipeline` — Task [#87286](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87286)
- A classe interna `CircuitBreaker<object?>` está marcada `[Obsolete]` apontando para o guia de migração já presente no repositório. Localizar todos os usos internos (ex.: dentro do próprio `Mvp24Hours.Infrastructure`) e substituí-los pela API de resiliência nativa baseada em `Microsoft.Extensions.Resilience`, seguindo o passo a passo documentado localmente.
- **Concluído (15/07/2026):** único uso de produção (`ResilientCacheProvider`) migrado para `NativeResiliencePipeline` / `NativeResiliencePipeline<object?>` (retry + circuit breaker via Polly v8). `CacheResilienceOptions` mapeado para `NativeResilienceOptions` (`EnableTimeout=false` para preservar comportamento). Adicionado `ShouldHandleAsCircuitBreakerFailure` em `NativeResilienceOptions`. Wrapper não-genérico `CircuitBreaker` também marcado `[Obsolete]` com `#pragma` no auto-uso. CS0618 de `CircuitBreaker*` → **0**. Build solução Debug: **0 erro(s)**.
- `src/Mvp24Hours.Infrastructure/Resilience/Implementations/CircuitBreaker.cs` (linhas 320, 333)
- `docs/pt-br/modernization/generic-resilience.md`, `docs/en-us/modernization/generic-resilience.md`
- https://learn.microsoft.com/dotnet/core/resilience/

[x] 5.2 - Migrar `SqlServerDistributedLockProvider` de `System.Data.SqlClient` para `Microsoft.Data.SqlClient` — Task [#87287](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87287)
- Trocar os `using`/tipos `SqlConnection`, `SqlCommand` e `SqlParameter` do namespace legado `System.Data.SqlClient` (obsoleto) pelos equivalentes do pacote `Microsoft.Data.SqlClient`, já recomendado pela própria Microsoft para todos os cenários novos, incluindo suporte atualizado a TLS/Azure AD.
- **Concluído (15/07/2026):** `using` e tipos migrados para `Microsoft.Data.SqlClient` em `SqlServerDistributedLockProvider`. `PackageReference` em `Mvp24Hours.Infrastructure` trocado de `System.Data.SqlClient` → `Microsoft.Data.SqlClient` (CPM `7.0.2`). `System.Data.SqlClient` permanece em `Infrastructure.Data.EFCore` (fora do escopo 5.2). Build Debug do Infrastructure: **0 erro(s)**; sem CS0618 de `System.Data.SqlClient`.
- `src/Mvp24Hours.Infrastructure/DistributedLocking/Providers/SqlServerDistributedLockProvider.cs` (linhas 93, 96, 101–105, 107)
- https://learn.microsoft.com/sql/connect/ado-net/introduction-microsoft-data-sqlclient-namespace
- https://www.nuget.org/packages/Microsoft.Data.SqlClient

[x] 5.3 - Substituir construtores obsoletos de `X509Certificate2` por `X509CertificateLoader` (SYSLIB0057) — Task [#87288](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87288)
- Atualizar todas as chamadas a `new X509Certificate2(string)`, `new X509Certificate2(string, string?, X509KeyStorageFlags)` e `new X509Certificate2(byte[], ...)` em `CertificateHelper` para os métodos estáticos equivalentes de `X509CertificateLoader` (`LoadCertificateFromFile`, `LoadPkcs12FromFile`, `LoadCertificate`, `LoadPkcs12`), conforme a nova API introduzida no .NET 9/10 para carregamento seguro de certificados.
- **Concluído (15/07/2026):** em `CertificateHelper`, carga sem senha → `LoadCertificateFromFile` / `LoadCertificate`; com senha → `LoadPkcs12FromFile` / `LoadPkcs12` (file, base64 e bytes). SYSLIB0057 em `CertificateHelper` → **0**. Build Debug do Infrastructure: **0 erro(s)**. Residual fora do escopo 5.3: `MongoDbAuthenticationOptions` ainda usa `new X509Certificate2(...)`.
- `src/Mvp24Hours.Infrastructure/Http/Helpers/CertificateHelper.cs` (linhas 86, 87, 110, 111, 132, 133)
- https://aka.ms/dotnet-warnings/SYSLIB0057
- https://learn.microsoft.com/dotnet/api/system.security.cryptography.x509certificates.x509certificateloader

[x] 5.4 - Substituir uso de `ServicePointManager` por `HttpClient` (SYSLIB0014) — Task [#87289](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87289)
- Remover a configuração via `ServicePointManager` em `SmtpEmailProvider` (obsoleta desde que `WebRequest`/`HttpWebRequest`/`ServicePoint`/`WebClient` foram descontinuados) e mover a configuração equivalente (ex.: validação de certificado, timeouts) para a pilha baseada em `HttpClient`/`SocketsHttpHandler`, já usada no restante da infraestrutura HTTP do projeto.
- **Concluído (15/07/2026):** removido `ServicePointManager.ServerCertificateValidationCallback` de `SmtpEmailProvider` (SmtpClient não tem hook por conexão; callback global obsoleto e afetava HTTP). Se `ServerCertificateValidationCallback` estiver configurado, loga warning e ignora (validação padrão do OS). Docs em `SmtpEmailOptions` atualizados. Removido também `ServicePointManager.SecurityProtocol` morto em `HttpClientExtensions` e `WebRequestHelper` (TLS fica em `HttpClientHandler`/`SslStream`). SYSLIB0014 de `ServicePointManager` → **0**. Build Debug do Infrastructure: **0 erro(s)**. Residual: `WebRequestHelper` ainda usa `WebRequest.Create` com `#pragma` SYSLIB0014 (helper legado; migração completa para HttpClient fora do escopo).
- `src/Mvp24Hours.Infrastructure/Email/Providers/SmtpEmailProvider.cs` (linha 161)
- https://aka.ms/dotnet-warnings/SYSLIB0014
- https://learn.microsoft.com/dotnet/api/system.net.http.httpclient

[x] 5.5 - Substituir construtor obsoleto de `Rfc2898DeriveBytes` pelo método estático `Pbkdf2` (SYSLIB0060) — Task [#87290](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87290)
- Trocar `new Rfc2898DeriveBytes(string, byte[], int, HashAlgorithmName)` em `FieldEncryption` pela chamada estática `Rfc2898DeriveBytes.Pbkdf2(...)`, mantendo os mesmos parâmetros de senha, salt, iterações e algoritmo de hash, e validando que os testes de criptografia de campo continuam produzindo o mesmo resultado (ou documentar a mudança de comportamento, se houver).
- **Concluído (15/07/2026):** `EncryptionKeyHelper.DeriveKeyFromPassword` migrado para `Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, SHA256, 32)`. Equivalência byte-a-byte com o construtor+`GetBytes(32)` validada (`EQUIVALENT=true`). Nenhum outro `new Rfc2898DeriveBytes` na solução. SYSLIB0060 → **0**. Build Debug do MongoDb: **0 erro(s)**. Sem testes dedicados de FieldEncryption na suíte.
- `src/Mvp24Hours.Infrastructure.Data.MongoDb/Security/FieldEncryption.cs` (linha 399)
- https://aka.ms/dotnet-warnings/SYSLIB0060
- https://learn.microsoft.com/dotnet/api/system.security.cryptography.rfc2898derivebytes.pbkdf2

[x] 5.6 - Substituir `FallbackCredentialsFactory` (AWS SDK, obsoleto) por `DefaultAWSCredentialsIdentityResolver` — Task [#87291](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87291)
- Atualizar `AwsSecretsManagerProvider` para resolver credenciais AWS através de `DefaultAWSCredentialsIdentityResolver` (API recomendada pelo AWSSDK.Core atual), removendo a dependência da fábrica de credenciais legada.
- **Concluído (15/07/2026):** `FallbackCredentialsFactory.GetCredentials()` substituído por `DefaultAWSCredentialsIdentityResolver.GetCredentials(config)` (AWSSDK.Core v4 / AWSSDK.SecretsManager 4.0.100.3). Nenhum outro uso de `FallbackCredentialsFactory` na solução. CS0618 de `FallbackCredentialsFactory` → **0**. Build Debug do Infrastructure: **0 erro(s)**.
- `src/Mvp24Hours.Infrastructure/Security/Providers/AwsSecretsManagerProvider.cs` (linha 247)
- https://docs.aws.amazon.com/sdkfornet/v3/apidocs/index.html?page=SecurityToken/TSecurityToken.html
- https://docs.aws.amazon.com/sdk-for-net/latest/developer-guide/creds-assign.html

[x] 5.7 - Migrar aliases obsoletos de CQRS (`DomainEventBase`, `IDomainEvent`, `IDomainEventHandler<T>`) para os equivalentes `Mediator*` nos testes — Task [#87292](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87292)
- Substituir todos os usos de `DomainEventBase`, `IDomainEvent` e `IDomainEventHandler<TEvent>` (marcados obsoletos e programados para remoção) por `MediatorDomainEventBase`, `IMediatorDomainEvent` e `IMediatorDomainEventHandler<TEvent>` nos artefatos de teste de CQRS, garantindo que a suíte de testes não dependa de APIs que serão removidas em versão futura.
- **Concluído (15/07/2026):** `DomainEventBase` → `MediatorDomainEventBase` (eventos em `TestAggregate`, `TestDomainEvent`, `ProjectionTest`); `IDomainEventHandler<T>` → `IMediatorDomainEventHandler<T>` (3 handlers); asserts/nomes em `DomainEventTest` alinhados a `IMediatorDomainEvent*`. `Core.IDomainEvent` / `IDomainEventDispatcher` mantidos (não são aliases obsoletos). CS0618 dos aliases CQRS nos testes → **0**. DomainEventTest + ProjectionTest: **30/30**.
- `src/Tests/Mvp24Hours.Infrastructure.Cqrs.Test/Support/TestAggregate.cs` (linhas 15, 22, 31, 38, 44, 207), `src/Tests/Mvp24Hours.Infrastructure.Cqrs.Test/Support/TestDomainEvent.cs` (linhas 16, 25, 30, 44, 58), `src/Tests/Mvp24Hours.Infrastructure.Cqrs.Test/ProjectionTest.cs` (linha 433), `src/Tests/Mvp24Hours.Infrastructure.Cqrs.Test/DomainEventTest.cs` (linhas 149, 157)
- https://learn.microsoft.com/dotnet/csharp/language-reference/attributes/general#obsolete-attribute

[x] 5.8 - Atualizar construtores obsoletos do Testcontainers (`MsSqlBuilder()`, `MongoDbBuilder()`, `RabbitMqBuilder()`) — Task [#87293](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87293)
- Substituir as chamadas ao construtor sem parâmetros (obsoleto, será removido) pela variante que recebe explicitamente a imagem do container, conforme a discussão de migração oficial do projeto Testcontainers .NET, fixando versões de imagem conhecidas para builds reprodutíveis.
- **Concluído (15/07/2026):** construtores parameterless → `Builder("repo:tag")` conforme Testcontainers ≥4.10. Imagens fixadas: SQL Server `2022-CU14-ubuntu-22.04`, MongoDB `mongo:6.0`, RabbitMQ `rabbitmq:3.13-management`, Redis `redis:3.2.5-alpine` (4 testes Redis, residual CS0618). Removidos `.WithImage(...)` redundantes. CS0618 de `*Builder()` → **0**. Build Debug dos 4 projetos de teste: **0 erro(s)**. Execução com Docker fica para a tarefa 9.3.
- `src/Tests/Mvp24Hours.Application.Integration.Test/Fixtures/SqlServerContainerFixture.cs` (linha 26), `src/Tests/Mvp24Hours.Application.MongoDb.Test/CommandServiceTest.cs` (linha 27), `src/Tests/Mvp24Hours.Application.MongoDb.Test/QueryServiceTest.cs` (linha 28), `src/Tests/Mvp24Hours.Application.RabbitMQ.Test/Test1RabbitMQ.cs` (linha 26)
- https://github.com/testcontainers/testcontainers-dotnet/discussions/1470

---

## FASE 6 — Qualidade de código: warnings diversos

> **ADO:** US [#87273](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87273) · Feature [#87242](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87242)

[x] 6.1 - Corrigir CS0168 (variável de exceção declarada e nunca usada) em `Infrastructure.Data.EFCore` — Task [#87299](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87299)
- Nos blocos `catch (Exception ex)` onde `ex` nunca é referenciado, remover o identificador (`catch (Exception)`) ou, preferencialmente, usar a variável para logar o contexto do erro (mais alinhado às boas práticas de observabilidade já adotadas no restante do projeto).
- **Concluído (15/07/2026):** CS0168 → **0** em `Infrastructure.Data.EFCore`. Estratégia: remover identificador (sem logger nelas extensões estáticas): `catch (SqlException)` em `MigrateDatabase` (alinha ao async); `catch (Exception)` nos 3 `ExecuteUpdate*`/`ExecuteDelete*` que só param o stopwatch e fazem `throw`. Blocos que usam `ex.Message` em `BulkOperationResult.Failure` mantidos. Build Debug do projeto: **0 erro(s)** / **0 CS0168**.
- `src/Mvp24Hours.Infrastructure.Data.EFCore/Extensions/DatabaseExtensions.cs` (linha 29), `src/Mvp24Hours.Infrastructure.Data.EFCore/Extensions/BulkOperationsExtensions.cs` (linhas 356, 396, 437)
- https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-messages/cs0168

[x] 6.2 - Corrigir CS0219 (variável atribuída mas nunca usada) em `SagaStateMachineConsumer` — Task [#87300](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87300)
- Remover a variável local `isNew` se realmente não for necessária, ou completar a lógica que deveria consumi-la (revisar a intenção original do código antes de decidir entre as duas opções).
- **Concluído (15/07/2026):** CS0219 → **0**. `isNew` era código morto copiado de `SagaConsumerProcessor` (onde alimenta `SagaConsumeContext.IsNew`); em `SagaStateMachineConsumer` o fluxo usa `ProcessEventAsync` com `IConsumeContext` e já registra a criação via `LogInformation`. Removidos `var isNew = false` e `isNew = true`. Build Debug do RabbitMQ: **0 erro(s)** / **0 CS0219**.
- `src/Mvp24Hours.Infrastructure.RabbitMQ/Saga/SagaStateMachineConsumer.cs` (linha 69)
- https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-messages/cs0219

[x] 6.3 - Corrigir CS1718 (comparação de uma variável com ela mesma) em `EnumerationTest` — Task [#87301](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87301)
- Investigar as duas comparações apontadas (provavelmente um erro de digitação onde um dos operandos deveria referenciar uma segunda instância/variável) e corrigir a asserção para validar o comportamento pretendido pelo teste, e não uma tautologia.
- **Concluído (15/07/2026):** CS1718 → **0**. Não era typo: as asserções testavam reflexividade de `<=`/`>=` com o mesmo local (`pending <= pending`). Introduzido `pendingEqual = OrderStatus.Pending` e comparado `pending` com `pendingEqual`, preservando a intenção e eliminando a tautologia. `Comparison_Operators_WorkCorrectly` + EnumerationTest: **aprovados**. Build Debug do Core.Test: **0 erro(s)** / **0 CS1718**.
- `src/Tests/Mvp24Hours.Core.Test/EnumerationTest.cs` (linhas 383, 384)
- https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-messages/cs1718

[x] 6.4 - Corrigir CS0108 (ocultação de membro herdado sem `new`) em `TestOrderWithSnapshot` — Task [#87302](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87302)
- Avaliar se a propriedade `Id` de `TestOrderWithSnapshot` deve realmente ocultar `AggregateRoot.Id` (nesse caso, adicionar o modificador `new` explicitamente) ou se o membro deveria ser removido/renomeado para evitar a ambiguidade.
- **Concluído (15/07/2026):** CS0108 → **0**. A propriedade local `Id` era redundante: `SnapshotAggregateRoot<T>` já herda `Guid Id { get; protected set; }` de `AggregateRoot`. Removida a declaração local (e o comentário incorreto); `Apply`/`CreateSnapshot`/`RestoreFromSnapshot` passam a usar o `Id` da base. Build Debug do Cqrs.Test: **0 erro(s)** / **0 CS0108**.
- `src/Tests/Mvp24Hours.Infrastructure.Cqrs.Test/Support/TestAggregate.cs` (linha 207)
- https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-messages/cs0108

[x] 6.5 - Corrigir CA2022 (leitura potencialmente incompleta de `Stream.ReadAsync`) em `ETagMiddleware` — Task [#87303](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87303)
- Substituir a chamada direta a `Stream.ReadAsync(byte[], int, int)` por um laço que verifique o total de bytes lidos (ou usar `Stream.ReadExactlyAsync`, disponível a partir do .NET 7+), garantindo que buffers grandes sejam sempre lidos por completo antes do cálculo do ETag.
- **Concluído (15/07/2026):** CA2022 → **0**. Em `GenerateETagAsync`, `ReadAsync(buffer, 0, buffer.Length)` substituído por `ReadExactlyAsync(buffer)` (.NET 7+), garantindo leitura completa do body antes do hash do ETag. Build Debug do WebAPI: **0 erro(s)** / **0 CA2022**.
- `src/Mvp24Hours.WebAPI/Middlewares/ETagMiddleware.cs` (linha 169)
- https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2022
- https://learn.microsoft.com/dotnet/api/system.io.stream.readexactlyasync

[x] 6.6 - Corrigir xUnit1031 (bloqueio síncrono dentro de teste assíncrono) — Task [#87304](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87304)
- Localizar o teste apontado pelo analisador (uso de `.Result`/`.Wait()`/`GetAwaiter().GetResult()` dentro de um método de teste `async`) e substituir por `await`, eliminando o risco de deadlock em ambientes com `SynchronizationContext` customizado.
- Buscar por `xUnit1031` no log de build (`build-current.log` gerado na tarefa 1.1) para identificar o arquivo/linha exatos antes de aplicar a correção
- **Concluído (15/07/2026):** xUnit1031 → **0** (5 ocorrências únicas). Testes convertidos para `async Task` + `await`: 3× `Assert.ThrowsAsync(...).Result` em `HybridCacheTest`; `.Result` em `MongoDbConnectionManagerTests.Should_Reset_Reconnect_Attempts_On_Success`; `GetAwaiter().GetResult()` em `TransactionScopeTest.Dispose_WhenActive_ShouldAutoRollback` (mantém `Dispose` síncrono no `using`). Testes afetados: **5/5** aprovados. Build dos 3 projetos: **0 erro(s)** / **0 xUnit1031**.
- `src/Tests/Mvp24Hours.Infrastructure.Caching.Test/HybridCacheTest.cs` (linhas 606, 618, 632), `src/Tests/Mvp24Hours.Infrastructure.Data.MongoDb.Test/Resiliency/MongoDbConnectionManagerTests.cs` (linha 257), `src/Tests/Mvp24Hours.Application.Test/TransactionScopeTest.cs` (linha 313)
- https://xunit.net/xunit.analyzers/rules/xUnit1031

---

## FASE 7 — Segurança e higiene de dependências NuGet

> **ADO:** US [#87274](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87274) · Feature [#87242](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87242)

[x] 7.1 - Investigar e mitigar a vulnerabilidade NU1903 em `System.Security.Cryptography.Xml` 10.0.0 — Task [#87305](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87305)
- Executar `dotnet list package --vulnerable --include-transitive` em cada projeto afetado para identificar a árvore de dependências que traz `System.Security.Cryptography.Xml` 10.0.0 (referenciado pelos avisos das duas advisories abaixo) e definir a estratégia de correção: atualizar o pacote de nível superior que o traz transitivamente, ou adicionar um `PackageReference`/`PackageVersion` direto fixando uma versão corrigida quando disponível.
- **Concluído (15/07/2026):** cadeia `System.ServiceModel.*` 10.0.652802 → `Cryptography.Xml` **10.0.0** (GHSA-37gx / GHSA-w3x6; patch ≥10.0.6). Pin CPM `PackageVersion` **10.0.10** + `PackageReference` em `Infrastructure` (pinagem transitiva central rejeitada: NU1109 RabbitMQ.Client 6.8.1 vs MassTransit). Restore: **0** NU1903; `dotnet list … --vulnerable`: nenhum projeto vulnerável. Residual: 1× NU1510 intencional no Infrastructure (AspNetCore.App). Evidência: [`nu1903-cryptography-xml-net10-v1.md`](./nu1903-cryptography-xml-net10-v1.md).
- Projetos afetados: `src/Mvp24Hours.Infrastructure.Caching.Redis`, `src/Mvp24Hours.Infrastructure.CronJob`, `src/Mvp24Hours.WebAPI`, `src/Tests/Mvp24Hours.Application.Integration.Test`, `src/Tests/Mvp24Hours.Application.Pipe.Test`, `src/Tests/Mvp24Hours.Application.MySql.Test` (e demais listados no log da tarefa 1.1)
- https://github.com/advisories/GHSA-37gx-xxp4-5rgx
- https://github.com/advisories/GHSA-w3x6-4m5h-cxqf
- https://learn.microsoft.com/nuget/reference/errors-and-warnings/nu1903

[x] 7.2 - Remover `PackageReference` redundantes apontados por NU1510 — Task [#87306](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87306)
- Nos projetos indicados, remover as referências explícitas a pacotes que o NuGet identificou como não sendo "podados" (ou seja, já providos implicitamente via `FrameworkReference`/dependência transitiva no .NET 10), simplificando os `.csproj` e reduzindo a superfície de manutenção de versões.
- **Concluído (15/07/2026):** removidos **16** `PackageReference` redundantes (6 Core + 10 Infrastructure) cobertos por `FrameworkReference` AspNetCore.App. CPM: removidos 5 `PackageVersion` órfãos (`Configuration.FileExtensions`/`Json`, `Logging.Configuration`, `Options.DataAnnotations`, `RateLimiting`). Residual intencional: 1× NU1510 `System.Security.Cryptography.Xml` no Infrastructure (pin 7.1). Restore: **1** NU1510; build Debug: **0** erro(s). Evidência: [`nu1510-redundant-packages-net10-v1.md`](./nu1510-redundant-packages-net10-v1.md).
- `src/Mvp24Hours.Core/Mvp24Hours.Core.csproj` (`Microsoft.Extensions.Configuration.Binder`, `DependencyInjection.Abstractions`, `Logging.Abstractions`, `Logging.Configuration`, `Options.DataAnnotations`, `System.Threading.RateLimiting`), `src/Mvp24Hours.Infrastructure/Mvp24Hours.Infrastructure.csproj` (`Microsoft.Extensions.Caching.Memory`, `Configuration`, `Configuration.Binder`, `Configuration.FileExtensions`, `Configuration.Json`, `DependencyInjection`, `Diagnostics.HealthChecks`, `Hosting.Abstractions`, `Http`, `Logging.Abstractions`)
- https://learn.microsoft.com/nuget/reference/errors-and-warnings/nu1510
- https://learn.microsoft.com/dotnet/core/compatibility/sdk/8.0/implicit-package-references

[x] 7.3 - Revisar os workflows `security-scan` (em `ci.yml`) e `dependency-review.yml` após as correções das Fases 5 e 7.1 — Task [#87307](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87307)
- Após aplicar as correções de vulnerabilidade e de pacotes redundantes, reexecutar `dotnet list package --vulnerable --include-transitive` localmente (mesmo comando usado pelo job `security-scan`) para confirmar que a lista de vulnerabilidades foi zerada, e validar que `dependency-review.yml` não bloqueia PRs futuros por falsos positivos relacionados a esta migração.
- **Concluído (15/07/2026):** `dotnet list src/Mvp24Hours.sln package --vulnerable --include-transitive` → **0** projetos vulneráveis (28/28 ok). Achado: job `security-scan` rodava na raiz (sem `.sln`) e não falhava em advisories; corrigido para apontar à solução + fail-on-hit (grep EN/PT). `dependency-review.yml` mantido (`fail-on-severity: moderate`, deny GPL) — sem falso positivo pós-pin 7.1. Evidência: [`security-scan-dependency-review-net10-v1.md`](./security-scan-dependency-review-net10-v1.md).
- `.github/workflows/ci.yml` (job `security-scan`, linhas 113–138), `.github/workflows/dependency-review.yml`
- https://learn.microsoft.com/nuget/reference/cli-reference/cli-ref-list-package
- https://docs.github.com/code-security/supply-chain-security/understanding-your-software-supply-chain/about-dependency-review

---

## FASE 8 — Limpeza de artefatos e higiene do repositório

> **ADO:** US [#87275](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87275) · Feature [#87242](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87242)

[x] 8.1 - Remover `build-webapi-errors.txt` do controle de versão e evitar recorrência — Task [#87308](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87308)
- Excluir o arquivo de log de build de 554KB commitado por engano durante a migração (commit `fb91f2d`), e adicionar um padrão como `*.log`/`build-*.txt` ao `.gitignore` para impedir que logs de build voltem a ser versionados acidentalmente.
- **Concluído (16/07/2026):** `build-webapi-errors.txt` removido do versionamento (`git rm`). `.gitignore` já cobria `*.log`; adicionado `build-*.txt` para dumps ad-hoc de build (ex.: `build-*-errors.txt`). Confirmado que o único artefato `build-*.{txt,log}` versionado era esse arquivo.
- `build-webapi-errors.txt` (raiz do repositório), `.gitignore`
- https://git-scm.com/docs/gitignore

[x] 8.2 - Confirmar que nenhum outro artefato de build/log ficou versionado por engano — Task [#87309](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87309)
- Rodar `git ls-files | Select-String -Pattern '\.(log|txt)$'` (ou equivalente) e revisar manualmente os resultados além de `build-webapi-errors.txt`, garantindo que a raiz do repositório e as pastas de projeto não contenham outros artefatos gerados (logs de build, dumps de erro, `bin/`/`obj/` versionados etc.) que deveriam estar cobertos pelo `.gitignore` existente.
- **Concluído (16/07/2026):** auditados `.log`/`.txt` versionados e demais artefatos de build. Removido `mongo-cs8625.txt` (dump de avisos CS8625, ~18KB). Mantidos os 4 `docs/llms_*.txt` (documentação intencional). Nenhum `bin/`/`obj/`/`.trx`/`.nupkg`/coverage versionado. `.gitignore`: `*-cs[0-9]*.txt` + exceções `!tasks/*.md`/`!tasks/*.json` (o antigo `tasks/*` bloqueava evidências das fases 1–7). Evidência: [`repo-hygiene-artifacts-net10-v1.md`](./repo-hygiene-artifacts-net10-v1.md).
- `.gitignore` (`*.log`, `build-*.txt`, `*-cs[0-9]*.txt`, `tasks/*` com `!tasks/*.md` / `!tasks/*.json`)
- https://learn.microsoft.com/dotnet/core/tools/dotnet-build#build-outputs

---

## FASE 9 — Execução completa da suíte de testes

> **ADO:** US [#87276](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87276) · Feature [#87242](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87242)

[x] 9.1 - Inventariar todos os projetos de teste e suas dependências de infraestrutura — Task [#87314](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87314)
- Catalogar, para cada um dos 15 projetos em `src/Tests/**`, se ele é 100% unitário (sem dependência externa) ou se depende de Testcontainers/Docker (MongoDB, SQL Server, MySQL, PostgreSQL, Redis, RabbitMQ) ou de outros serviços externos. Confirmar a disponibilidade de Docker Desktop (ou engine compatível) no ambiente onde os testes serão executados — nesta auditoria, o Docker não estava disponível (`docker version` falhou ao conectar no daemon), o que bloquearia a execução dos testes de integração até que o ambiente seja provisionado.
- **Concluído (16/07/2026):** **16** projetos (não 15; inclui `Infrastructure.Data.MongoDb.Test`). Grupos: **A** unitário/in-process (9) · **B** EF `InMemory` default (3: SQLServer/MySql/PostgreSql — sem Docker) · **C** Testcontainers (4: Integration/MsSql, MongoDb, Redis, RabbitMQ). Docker Desktop **4.79.0** / Engine **29.5.3** OK (`hello-world`). MySql/PostgreSql/SQLServer **não** usam Testcontainers (só ifdef real via appsettings). Pacote `Testcontainers.PostgreSql` no Integration sem uso. Evidência: [`test-inventory-net10-v1.md`](./test-inventory-net10-v1.md).
- `src/Tests/Mvp24Hours.Application.Integration.Test`, `Mvp24Hours.Application.MongoDb.Test`, `Mvp24Hours.Application.RabbitMQ.Test`, `Mvp24Hours.Application.Redis.Test` (Testcontainers); `Mvp24Hours.Application.SQLServer.Test`, `MySql.Test`, `PostgreSql.Test` (EF InMemory); `Mvp24Hours.Core.Test`, `Application.Test`, `Patterns.Test`, `Pipe.Test`, `Caching.Test`, `CronJob.Test`, `Cqrs.Test`, `WebAPI.Test`, `Infrastructure.Data.MongoDb.Test` (unitários)
- https://dotnet.testcontainers.org/
- https://learn.microsoft.com/dotnet/core/testing/

[x] 9.2 - Executar toda a suíte de testes que não depende de infraestrutura externa — Task [#87315](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87315)
- Rodar `dotnet test src/Mvp24Hours.sln -c Debug --filter "..."` (ou por projeto individual) para todos os testes classificados como unitários na tarefa 9.1, registrando o resultado (`Aprovado`/`Com falha`/`Ignorado`) por projeto. Como referência de sanidade, `Mvp24Hours.Core.Test` já foi validado nesta auditoria com 788/788 testes aprovados em ~1s, servindo de baseline de que o ambiente de testes básico está saudável.
- **Concluído (16/07/2026):** Grupos **A + B** (12 projetos, sem Testcontainers). **2188** aprovados · **0** falhas · **4** ignorados (BulkOperations InMemory em SQLServer.Test) · total **2192**. Todos exit 0. Evidência: [`test-unit-run-net10-v1.md`](./test-unit-run-net10-v1.md) (+ [`.json`](./test-unit-run-net10-v1.json)).
- `src/Tests/Mvp24Hours.Core.Test`, demais projetos "unitários" mapeados na tarefa 9.1
- https://learn.microsoft.com/dotnet/core/tools/dotnet-test

[x] 9.3 - Provisionar Docker/Testcontainers e executar a suíte completa de testes de integração — Task [#87316](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87316)
- Em um ambiente com Docker disponível, executar `dotnet test` para os projetos que dependem de Testcontainers (MongoDB, SQL Server, MySQL, PostgreSQL, Redis, RabbitMQ), aplicando antes as correções da tarefa 5.8 (construtores não-obsoletos). Registrar tempo de execução, taxa de sucesso e quaisquer falhas específicas de compatibilidade com .NET 10/imagens de container atualizadas.
- **Concluído (16/07/2026):** Grupo **C** (4 projetos Testcontainers). Docker OK; imagens 5.8 pré-puxadas. **110** aprovados · **0** falhas · **0** ignorados. Fix pontual: removidos MassTransit* não usados do `RabbitMQ.Test` (puxavam Client 7.x → `TypeLoadException` em `IModel` / Client 6.8.1). Evidência: [`test-integration-run-net10-v1.md`](./test-integration-run-net10-v1.md) (+ [`.json`](./test-integration-run-net10-v1.json)).
- Projetos listados na tarefa 9.1 (grupo Testcontainers)
- https://dotnet.testcontainers.org/quickstart/
- https://docs.docker.com/desktop/

[x] 9.4 - Categorizar testes com `Trait`/`Category` para permitir execução seletiva (unitário vs. integração) — Task [#87317](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87317)
- Adicionar atributos `[Trait("Category", "Unit")]` / `[Trait("Category", "Integration")]` (xUnit) nas classes de teste, permitindo que o CI (e desenvolvedores locais sem Docker) executem `dotnet test --filter "Category=Unit"` de forma rápida e confiável, deixando os testes de integração para um job/etapa separada com Docker disponível.
- **Concluído (16/07/2026):** categorias aplicadas em classes de teste de `src/Tests/**` com estratégia por projeto (4 projetos Testcontainers marcados como `Integration`; demais marcados como `Unit`). Validação executada com filtros: `Category=Integration` (**110/110 aprovados**, 0 falhas) e `Category=Unit` (filtro separa corretamente integração; execução exibiu **6 falhas preexistentes** em `Mvp24Hours.Core.Test.Extensions.ConvertExtensionsTest`, fora do escopo de categorização). Evidência: [`test-category-traits-net10-v1.md`](./test-category-traits-net10-v1.md).
- Todos os projetos em `src/Tests/**`
- https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests
- https://xunit.net/docs/running-tests-in-parallel

[x] 9.5 - Consolidar e publicar o relatório final de execução de testes (TRX + cobertura) — Task [#87318](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87318)
- Agregar os resultados das tarefas 9.2 e 9.3 em um relatório único (arquivos `.trx` + cobertura via `coverlet`/`XPlat Code Coverage`, já configurados no CI), documentando total de testes, aprovados, falhas e ignorados por projeto, e anexando esse relatório ao PR final da modernização como evidência objetiva de que "todos os testes foram executados".
- **Concluído (16/07/2026):** reexecução unificada dos **16** projetos com `--logger trx --collect:"XPlat Code Coverage"`. **2298** aprovados · **0** falhas · **4** ignorados (BulkOperations InMemory) · total **2302**. Fix pontual: `ConvertExtensionsTest` InlineData com escapes `\u` (mojibake UTF-8 duplo → 6 falhas). Evidência: [`test-final-report-net10-v1.md`](./test-final-report-net10-v1.md) (+ [`.json`](./test-final-report-net10-v1.json)); TRX/cobertura locais em `tasks/test-results-9.5/` (gitignored). `.gitignore`: `!tasks/*.md` / `!tasks/*.json` + ignore de `*.trx` / `coverage.cobertura.xml`.
- `.github/workflows/ci.yml` (step `🧪 Run tests`, linha 40–41, já usa `--logger trx --collect:"XPlat Code Coverage"`)
- https://learn.microsoft.com/dotnet/core/testing/unit-testing-code-coverage
- https://github.com/coverlet-coverage/coverlet

---

## FASE 10 — Validação final e fechamento do gate de qualidade

> **ADO:** US [#87277](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87277) · Feature [#87242](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87242)

[ ] 10.1 - Rebuild completo da solução visando zero warnings — Task [#87310](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87310)
- Executar novamente `dotnet build src/Mvp24Hours.sln -c Release` e comparar o total de avisos com o baseline da tarefa 1.1, confirmando que os ~4235 avisos originais foram eliminados (ou reduzidos a um conjunto residual explicitamente aceito e documentado, ex.: NU1900 de feed privado fora do escopo do repositório).
- `build-baseline.log` (gerado na tarefa 1.1)
- https://learn.microsoft.com/dotnet/core/tools/dotnet-build

[ ] 10.2 - Executar `dotnet format --verify-no-changes` em toda a solução — Task [#87311](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87311)
- Validar que o `.editorconfig` criado na tarefa 3.2 é suficiente para que todo o código já esteja formatado conforme o padrão definido, replicando exatamente o step `🎨 Check code formatting` do job `code-quality` do CI antes de abrir o PR final.
- `.github/workflows/ci.yml` (linha 76), `.editorconfig`
- https://learn.microsoft.com/dotnet/core/tools/dotnet-format

[ ] 10.3 - Validar de ponta a ponta o gate `TreatWarningsAsErrors=true` do pipeline `code-quality` — Task [#87312](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87312)
- Rodar localmente `dotnet build src/Mvp24Hours.sln -c Release /p:TreatWarningsAsErrors=true` e confirmar build verde, replicando o step exato do CI (ver tarefa 2.3). Só then abrir/atualizar o Pull Request final desta iniciativa, evitando qualquer regressão de CI vermelho.
- `.github/workflows/ci.yml` (linha 79)
- https://learn.microsoft.com/visualstudio/msbuild/msbuild-warnings-as-errors

[ ] 10.4 - Atualizar `CHANGELOG.md` com o resumo da modernização para .NET 10 — Task [#87313](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87313)
- Documentar, na próxima entrada do changelog, as mudanças relevantes para consumidores dos pacotes NuGet: alinhamento de `TargetFramework`/`LangVersion`, habilitação de Nullable em todos os projetos, remoção/substituição de APIs obsoletas (Fase 5) e eventuais mudanças de comportamento visíveis externamente (ex.: se algum tipo público passou a exigir `required` ou teve assinatura de nulidade alterada).
- `CHANGELOG.md`
- https://keepachangelog.com/pt-BR/1.1.0/
