# Tasks — Zeragem dos warnings residuais pós-.NET 10 (v2)

> Gerado em 17/07/2026. Continuação de [`tasks-net10-v1.md`](./tasks-net10-v1.md) (Fases 1–10 concluídas).
> **Status final (2026-07-18):** plano **concluído** (Fases 4–7). Release **0 erro(s) / 0 aviso(s)** com gate estrito; `MvpResidualWarnings` = só `NU1510`. Dívida 10.1/10.2/10.3 da v1 encerrada (tarefa 7.4).
> **Ponto de partida:** a solução compilava com **0 erro(s)**. A Fase 10 (v1) aceitou um **residual de ~948 avisos** e neutralizou o gate `TreatWarningsAsErrors` via `MvpResidualWarnings` em [`src/Directory.Build.props`](../src/Directory.Build.props). **Este plano zerou esse residual e reativou o gate estrito.**
>
> **Objetivo (atingido):** `dotnet build src/Mvp24Hours.sln -c Release --no-incremental /p:TreatWarningsAsErrors=true` → **0 erro(s) / 0 aviso(s)**, com `MvpResidualWarnings` **somente `NU1510`** (pin de segurança).

---

## 0 — Por que este plano existe (leia antes de começar)

Na primeira rodada gastamos muitos tokens porque cada aviso foi tratado *ad hoc*: releitura de logs inteiros no contexto, redescoberta da abordagem a cada arquivo e builds da solução inteira a cada correção. Este plano corrige isso com **três decisões estruturais**:

1. **Padrões de correção fixos por família de aviso** (§1). O agente **não decide a abordagem** — ele aplica o padrão. Isso elimina a deliberação repetida.
2. **Fluxo de execução padronizado e enxuto** (§2). Build **isolado por projeto**, contagem por código via script (nunca despejar o log no contexto), correção, recontagem. Diffs pequenos e revisáveis por PR.
3. **Trabalho agrupado por projeto** (produção → testes), com o gate global (`MvpResidualWarnings`) sendo **encolhido por código** só quando o código zera na solução inteira.

> **Convenção de status:** `[ ]` pendente · `[x]` concluído · `[~]` em andamento/bloqueado (explicar no PR).
> **ADO:** work items a criar sob a mesma Feature da v1 ([#87242](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87242)). Registrar US/Task por fase antes de iniciar.

---

## 1 — PADRÕES DE CORREÇÃO (fonte única da verdade)

> **Regra de ouro:** corrija a **causa** (assinatura/inicialização corretas), não o sintoma. Supressão (`!`, `#pragma`, `= null!`) é exceção e **exige comentário justificando a invariante**. Nunca desabilite Nullable no projeto para "resolver".

### 1.A — Nullable Reference Types (CS86xx)

| Código | Situação | Padrão a aplicar (em ordem de preferência) |
|---|---|---|
| **CS8618** | Membro não-anulável não inicializado ao sair do construtor | (a) `required` para membros públicos que o consumidor **deve** fornecer; (b) inicializar com default seguro (`= string.Empty;`, `= new();`, `= [];`) ou no construtor; (c) declarar `?` se legitimamente opcional. **POCOs de mapeamento (EF/Mongo) preenchidos pelo framework:** `= null!` **com comentário** `// set by <ORM>`. |
| **CS8602** | Deref de valor possivelmente nulo | Guarda (`if (x is null) ...`), `?.`/`??`, ou `ArgumentNullException.ThrowIfNull(x)` no topo se for pré-condição. |
| **CS8604** | Argumento possivelmente nulo | `ThrowIfNull` para obrigatório; `?? valorPadrão`; ou tornar o parâmetro do método chamado `T?` se ele aceita nulo. |
| **CS8603** | Retorno possivelmente nulo | Mudar retorno para `T?`; ou garantir não-nulo (`?? throw`, `?? default`). |
| **CS8600** | Conversão de null literal/possível para não-anulável | Alvo `T?`; ou `??`. |
| **CS8625** | `null` literal atribuído a não-anulável | Alvo `T?` (parâmetro/campo). Default `null` em parâmetro opcional ⇒ tipo `T?`. |
| **CS8619 / CS8620** | Nulidade divergente em genérico/coleção | Alinhar argumentos de tipo (`IEnumerable<T?>`, `Dictionary<string, T?>`, `Expression<Func<T, bool>>`). |
| **CS8622** | Nulidade de parâmetro em delegate/event handler | Assinar o handler com `object?`/`T?` conforme o delegate base. |
| **CS8629** | `Nullable<T>` value type pode ser null | `.Value` só após checagem; ou `x ?? default`. |
| **CS8631 / CS8764** | Nulidade em constraint de tipo / override | Alinhar exatamente à assinatura/constraint da base. |

**Proibições:** não usar `#nullable disable` por arquivo; não usar `= null!` fora de POCOs de framework; não "resolver" espalhando `!`.

### 1.B — APIs obsoletas (CS0618 / SYSLIB / ASPDEPR)

- Migrar para a API recomendada **citada na própria mensagem do aviso**.
- `#pragma warning disable` só é permitido em helper legado explicitamente marcado como fora de escopo — e **com comentário** apontando o motivo.
- Migração de API deve vir acompanhada de execução dos testes do projeto (não-regressão comportamental).

### 1.C — LOGGEN002 (event id duplicado em `[LoggerMessage]`)

- Atribuir `EventId` **único e sequencial dentro do tipo** gerador de logs.
- Padronizar faixas por módulo (ex.: `1000–1999` Pipe, `2000–2999` RabbitMQ, etc.) para evitar recorrência.

### 1.D — CS0108 (ocultação de membro herdado)

- Se o membro da classe derivada é redundante ⇒ **remover** e usar o da base.
- Se a ocultação é intencional ⇒ modificador `new` explícito + comentário.

### 1.E — xUnit1031 (bloqueio síncrono em teste async)

- Converter o teste para `async Task` e trocar `.Result`/`.Wait()`/`GetAwaiter().GetResult()` por `await`.

---

## 2 — FLUXO DE EXECUÇÃO POR TAREFA (obrigatório)

> **Nunca** rode a solução inteira para corrigir um projeto. **Nunca** despeje o log de build no contexto. Sempre trabalhe isolado e conte por script.

1. **Build isolado com log em arquivo** (não vai para o contexto):
   ```powershell
   dotnet build <caminho-do-projeto.csproj> -c Release --no-incremental `
     -flp:logfile=warn.log`;verbosity=normal
   ```
2. **Contar por código** (só o resumo vai ao contexto):
   ```powershell
   (Get-Content warn.log) `
     | Select-String -Pattern '(warning|aviso)\s([A-Za-z]+\d+):' `
     | ForEach-Object { if ($_ -match '(warning|aviso)\s([A-Za-z]+\d+):') { $matches[2] } } `
     | Group-Object | Sort-Object Count -Descending `
     | ForEach-Object { '{0,-14} {1}' -f $_.Name, $_.Count }
   ```
3. **Localizar as ocorrências** de um código específico com `Grep`/`rg` no código-fonte (nunca lendo o log inteiro).
4. **Aplicar o padrão** da §1 para a família do aviso.
5. **Rebuild isolado** até o(s) código(s)-alvo do projeto zerarem.
6. **Rodar os testes** do projeto afetado (ou dos testes que o cobrem) — garantir não-regressão. Testes de integração (Testcontainers) só quando o Docker estiver disponível.
7. **Encolher o gate:** ao final da fase, recompilar a **solução** e remover de `MvpResidualWarnings` (em [`src/Directory.Build.props`](../src/Directory.Build.props)) **apenas os códigos que zeraram na solução inteira**. Confirmar build verde com `/p:TreatWarningsAsErrors=true`.
8. **Descartar `warn.log`** (coberto por `*.log` no `.gitignore`).

**Regra do gate:** `MvpResidualWarnings` é **global por código** (não por projeto). Um código só sai da lista quando é **0 na solução toda**. Por isso as famílias concentradas (§4, quick wins) saem cedo; o nullable (§5–§6) sai por último, código a código, na §7.

---

## 3 — Baseline residual (v1, 2026-07-17)

Contagem de linhas de aviso do build `Release` completo (indicativa; o resumo dedup ≈ metade — reconciliar na tarefa 4.0 abaixo). **Total no gate hoje: 19 códigos** em `MvpResidualWarnings`.

| Código | Ocorr. | Família | Concentração (projetos) |
|---|---|---|---|
| CS8618 | 280 | Nullable | **MongoDb 172**, RabbitMQ 18, EFCore 14, Cqrs 12, WebAPI 10, testes |
| LOGGEN002 | 186 | Logging src-gen | RabbitMQ 58, WebAPI 46, EFCore 46, Pipe 36 |
| CS8604 | 162 | Nullable | Application, MongoDb, EFCore, testes |
| CS0618 | 76 | API obsoleta | **EFCore 42** (System.Data.SqlClient), **MongoDb.Test 32** (MongoDbResiliencyPolicy), MongoDb 2 |
| CS8602 | 58 | Nullable | disperso |
| CS8625 | 36 | Nullable | disperso |
| CS8619 | 24 | Nullable | disperso |
| CS8600 | 22 | Nullable | disperso |
| CS8620 | 22 | Nullable | disperso |
| CS8622 | 14 | Nullable | disperso |
| CS8603 | 12 | Nullable | disperso |
| CS8631 | 8 | Nullable | disperso |
| ASPDEPR006 | 8 | API obsoleta | **WebAPI** (`IActionContextAccessor`) |
| SYSLIB0057 | 6 | API obsoleta | **MongoDb** (`MongoDbAuthenticationOptions`, `X509Certificate2`) |
| CS0108 | 4 | Ocultação | **MongoDb** (`RepositoryAsync._logger`) |
| CS8629 | 4 | Nullable | disperso |
| CS8764 | 2 | Nullable | disperso |
| xUnit1031 | 2 | Teste async | teste residual |

**Concentração por projeto (linhas):** MongoDb 190 · Application 154 · EFCore 134 · RabbitMQ 88 · WebAPI 64 · SQLServer.Test 44 · Core.Test 38 · Pipe 36 · MongoDb.Test 32 · MySql.Test 28 · Redis.Test 28 · PostgreSql.Test 28 · Pipe.Test 14 · Cqrs 12 · MongoDb.Test(unit) 10 · Patterns.Test 10 · Cqrs.Test 6 · RabbitMQ.Test 4 · CronJob.Test 4 · Core 2.

---

## FASE 4 — Quick wins não-nullable (encolher o gate primeiro)

> **Estratégia:** cada código abaixo está concentrado em 1–4 projetos e pode ser **zerado por completo** rapidamente, permitindo **removê-lo do gate imediatamente** (redução de 6 dos 19 códigos). Ganho alto, diff pequeno, baixo risco.
> **ADO:** US a criar.

[x] 4.0 - Reconciliar o baseline residual (dedup) e versionar
- Rodar o build `Release` da solução (§2, mas na `.sln`) e extrair a contagem **deduplicada** do resumo MSBuild por código e por projeto, para servir de métrica objetiva de progresso. Versionar em `tasks/warnings-baseline-v2.md` (+ `.json`). Reexecutar ao fim de cada fase.
- `src/Mvp24Hours.sln`
- https://learn.microsoft.com/dotnet/core/tools/dotnet-build
- **Concluído 2026-07-17:** build `Release` (0 erros; resumo MSBuild = 463 avisos). Total **deduplicado = 446** em **18 códigos**. Baseline versionado em [`warnings-baseline-v2.md`](./warnings-baseline-v2.md) + [`.json`](./warnings-baseline-v2.json); script de recontagem em [`parse-warnings.ps1`](./parse-warnings.ps1). Reconciliação v1 (~948 linhas) → v2 (446 dedup). Top famílias p/ Fase 4: LOGGEN002 93, CS0618 38, ASPDEPR006 4, SYSLIB0057 3, CS0108 2, xUnit1031 1.

[x] 4.1 - LOGGEN002 — atribuir EventIds únicos nos `[LoggerMessage]` (Pipe, RabbitMQ, WebAPI, EFCore)
- **Padrão:** §1.C. Localizar os tipos `*LoggerMessages`/`*Log` com `[LoggerMessage(EventId = ...)]` duplicados e reatribuir IDs únicos e sequenciais por tipo, com faixas por módulo. Ex. inicial: `Mvp24Hours.Infrastructure.Pipe/Logging/PipelineLoggerMessages.cs` (event id `2003` repetido).
- Ao final, se LOGGEN002 = 0 na solução ⇒ remover de `MvpResidualWarnings`.
- `src/Mvp24Hours.Infrastructure.Pipe/**`, `src/Mvp24Hours.Infrastructure.RabbitMQ/**`, `src/Mvp24Hours.WebAPI/**`, `src/Mvp24Hours.Infrastructure.Data.EFCore/**`
- https://learn.microsoft.com/dotnet/core/extensions/logger-message-generator
- https://aka.ms/dotnet-extensions-warnings/LOGGEN002
- **Concluído 2026-07-17:** cada `[LoggerMessage]` passou a ter um `EventId` único e sequencial dentro do tipo, mantendo as faixas por módulo (Pipe `2001–2033`, RabbitMQ `4001–4043`, EFCore `5001–5036`, WebAPI `6001–6035`). Substituídos os constantes por-categoria compartilhados por um constante por método. Build isolado dos 4 projetos e da solução: **LOGGEN002 = 0**. `LOGGEN002` **removido de `MvpResidualWarnings`** em [`src/Directory.Build.props`](../src/Directory.Build.props); `dotnet build ... /p:TreatWarningsAsErrors=true` → **0 erro(s)**. Gate: 19 → **18 códigos**.

[x] 4.2 - CS0618 (EFCore) — `System.Data.SqlClient` → `Microsoft.Data.SqlClient` (residual da 5.2)
- **Padrão:** §1.B. Concluir em `Infrastructure.Data.EFCore` a migração iniciada na tarefa 5.2 da v1 (que deixou o EFCore fora de escopo). Trocar `using`/tipos `SqlConnection`/`SqlCommand`/`SqlParameter` e o `PackageReference` (CPM) de `System.Data.SqlClient` para `Microsoft.Data.SqlClient`. Ex.: `HealthChecks/SqlServerHealthCheck.cs` (linha ~189).
- Rodar os testes que cobrem EFCore SQL Server (grupo B/InMemory + integração quando houver Docker).
- `src/Mvp24Hours.Infrastructure.Data.EFCore/**`, `src/Directory.Packages.props`
- https://learn.microsoft.com/sql/connect/ado-net/introduction-microsoft-data-sqlclient-namespace
- **Concluído 2026-07-17:** `PackageReference` do EFCore trocado de `System.Data.SqlClient` para `Microsoft.Data.SqlClient` (7.0.2, já no CPM); entrada `System.Data.SqlClient` **removida** de [`Directory.Packages.props`](../src/Directory.Packages.props) (sem outros consumidores). `using`/tipos migrados em `HealthChecks/SqlServerHealthCheck.cs`, `Extensions/DatabaseExtensions.cs`, `Security/RowLevelSecurityHelper.cs` (`SqlParameter` qualificado) e `Resilience/MvpExecutionStrategy.cs` (checagem forte agora em `Microsoft.Data.SqlClient.SqlException`, com *fallback* por reflexão para o namespace legado). `using` órfão removido de `Extensions/ResilienceDbContextExtensions.cs`. Build isolado do EFCore: **0 erro(s)**; CS0618 de `System.Data.SqlClient` = **0** (os 3 CS0618 residuais no projeto são do `MvpExecutionStrategy` auto-obsoleto — fora do escopo desta tarefa). Testes `Mvp24Hours.Application.SQLServer.Test` (Debug/InMemory): **232 aprovados, 4 ignorados, 0 falhas**.

[x] 4.3 - CS0618 (MongoDb.Test) — `MongoDbResiliencyPolicy` → `NativeMongoDbResilienceExtensions`
- **Padrão:** §1.B. Substituir os usos da API obsoleta `MongoDbResiliencyPolicy` nos testes (`Resiliency/MongoDbResiliencyPolicyTests.cs` e correlatos) pela recomendação citada na mensagem (`NativeMongoDbResilienceExtensions`). Se algum teste existe apenas para exercitar a API obsoleta, avaliar realocá-lo/removê-lo, documentando.
- Rodar `Mvp24Hours.Infrastructure.Data.MongoDb.Test`.
- `src/Tests/Mvp24Hours.Infrastructure.Data.MongoDb.Test/Resiliency/**`
- https://learn.microsoft.com/dotnet/csharp/language-reference/attributes/general#obsolete-attribute
- **Concluído 2026-07-17:** `MongoDbResiliencyPolicyTests.cs` existia **apenas** para exercitar a API obsoleta `MongoDbResiliencyPolicy` (fonte de todos os CS0618 do projeto de teste). Como o substituto recomendado `NativeMongoDbResilienceExtensions` tem formato totalmente diferente (pipeline Polly v8 registrado via DI, sem `TripCircuitBreaker`/`Metrics`/`ExecuteWithFallbackAsync`) e **não tinha cobertura**, o arquivo foi **realocado**: removido `MongoDbResiliencyPolicyTests.cs` e criado [`NativeMongoDbResilienceExtensionsTests.cs`](../src/Tests/Mvp24Hours.Infrastructure.Data.MongoDb.Test/Resiliency/NativeMongoDbResilienceExtensionsTests.cs) exercitando a API recomendada (registro/nome, retry transiente, callback `OnRetry`, timeout `TimeoutRejectedException`, cancelamento e presets ReplicaSet/ShardedCluster/Standalone). Build isolado do MongoDb.Test: **CS0618 = 0** no projeto de teste. Testes `Category=Unit`: **129 aprovados, 0 falhas** (inclui os 12 novos). **Residual conhecido (fora de escopo de 4.3):** 2 CS0618 permanecem em **produção** — `Extensions/MongoDbResiliencyExtensions.cs:126` (`new MongoDbResiliencyPolicy(options)` registrado como `IMongoDbResiliencyPolicy` por compatibilidade). Precisa ser tratado (migração da extensão ou `#pragma` justificado) antes de a 4.8 remover `CS0618` do gate.

[x] 4.4 - SYSLIB0057 (MongoDb) — `X509Certificate2` → `X509CertificateLoader` (residual da 5.3)
- **Padrão:** §1.B. Concluir em `MongoDbAuthenticationOptions` (linha ~333) a substituição dos construtores de `X509Certificate2` por `X509CertificateLoader.LoadCertificate*/LoadPkcs12*`, deixada fora do escopo na tarefa 5.3 da v1.
- `src/Mvp24Hours.Infrastructure.Data.MongoDb/Security/MongoDbAuthenticationOptions.cs`
- https://aka.ms/dotnet-warnings/SYSLIB0057
- **Concluído 2026-07-17:** as 3 ocorrências de SYSLIB0057 (`MongoDbAuthenticationOptions.cs:333/350/351`) foram migradas. CA cert (só chave pública) → `X509CertificateLoader.LoadCertificateFromFile(CaCertificatePath)`; certificado cliente do X.509 mTLS (exige chave privada) → `X509CertificateLoader.LoadPkcs12FromFile(CertificatePath, CertificatePassword)`, unificando os dois ramos com/sem senha (o parâmetro `password` é anulável). O construtor de cópia `new X509Certificate2(certificate)` no callback de validação (linha ~389) **não** é obsoleto sob SYSLIB0057 e foi mantido. Build isolado do MongoDb: **0 erro(s)**, **SYSLIB0057 = 0**. Testes `Mvp24Hours.Infrastructure.Data.MongoDb.Test` (`Category=Unit`): **129 aprovados, 0 falhas**. Remoção do gate ocorre na 4.8 (SYSLIB0057 já estava concentrado só no MongoDb).

[x] 4.5 - ASPDEPR006 (WebAPI) — `IActionContextAccessor` obsoleto
- **Padrão:** §1.B. Em `Extensions/ServiceCollectionExtentions.cs` (linha ~65), substituir `IActionContextAccessor`/`AddActionContextAccessor` pela abordagem recomendada na mensagem (uso de `IHttpContextAccessor` / obtenção do `ActionContext` via serviços atuais), validando o comportamento dependente.
- Rodar `Mvp24Hours.WebAPI.Test`.
- `src/Mvp24Hours.WebAPI/Extensions/ServiceCollectionExtentions.cs`
- https://learn.microsoft.com/aspnet/core/mvc/advanced/app-parts
- **Concluído 2026-07-17:** removido o registro de `IActionContextAccessor`/`ActionContextAccessor` (obsoletos sob ASPDEPR006) em `AddMvp24HoursWebEssential`. O `IUrlHelper` (scoped) passou a reconstruir o `ActionContext` a partir do `IHttpContextAccessor` seguindo o roteamento por endpoint (`HttpContext.GetEndpoint()?.Metadata.GetMetadata<ActionDescriptor>() ?? new ActionDescriptor()` + `HttpContext.GetRouteData()`), conforme a recomendação oficial ([breaking change .NET 10](https://learn.microsoft.com/aspnet/core/breaking-changes/10/iactioncontextaccessor-obsolete)). `using`s adicionados: `Microsoft.AspNetCore.Mvc.Abstractions` (`ActionDescriptor`) e `Microsoft.AspNetCore.Routing` (`GetRouteData`). Build isolado do WebAPI: **0 erro(s)**, **ASPDEPR006 = 0** (residual do projeto agora só CS8618/CS8603 nullable, fora de escopo). Testes `Mvp24Hours.WebAPI.Test`: **5 aprovados, 0 falhas**. Remoção do gate ocorre na 4.8 (ASPDEPR006 estava concentrado só no WebAPI).

[x] 4.6 - CS0108 (MongoDb) — remover ocultação de `RepositoryAsync._logger`
- **Padrão:** §1.D. `RepositoryAsync<T>._logger` oculta `RepositoryBase<T>._logger`. Remover o campo redundante e usar o da base (ou `new` explícito se houver diferença real de tipo/uso — investigar antes).
- `src/Mvp24Hours.Infrastructure.Data.MongoDb/Async/RepositoryAsync.cs` (linha ~29) e correlatos
- https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-messages/cs0108
- **Concluído 2026-07-17:** os 2 CS0108 vinham de dois campos `_logger` que ocultavam o `protected RepositoryBase<T>._logger`: `RepositoryAsync<T>._logger` e `BulkOperationsRepositoryAsync<T>._logger`. Ambos foram **removidos** (campo redundante), passando o `logger` ao construtor da base — `RepositoryAsync<T>` agora usa `RepositoryBase<T>(dbContext, options, logger)` (antes omitia o `logger`) e `BulkOperationsRepositoryAsync<T>` deixou de reatribuir `_logger` no corpo do construtor. Isso alinha as duas classes ao padrão já usado por `Repository<T>`/`ReadOnlyRepository<T>`. Não há mudança de categoria de log: `ILogger<TCategoryName>` é covariante (`out`), então o logger mais derivado flui para o campo `_logger` da base sem perda da categoria original. Build isolado do MongoDb: **0 erro(s)**, **CS0108 = 0** (residual do projeto agora só nullable + 2 CS0618 conhecidos da 4.3, fora de escopo). Testes `Mvp24Hours.Infrastructure.Data.MongoDb.Test` (`Category=Unit`): **129 aprovados, 0 falhas**. Remoção do gate ocorre na 4.8 (CS0108 estava concentrado só no MongoDb).

[x] 4.7 - xUnit1031 — eliminar o bloqueio síncrono residual
- **Padrão:** §1.E. Localizar as 2 ocorrências residuais (`.Result`/`.Wait()`/`GetAwaiter().GetResult()` em teste `async`) e converter para `await`.
- Projeto(s) de teste apontados pela recontagem 4.0
- https://xunit.net/xunit.analyzers/rules/xUnit1031
- **Concluído 2026-07-17:** a única origem (dedup) do xUnit1031 estava em `Mvp24Hours.Core.Test/Helpers/StringHelperTest.cs:78` — o teste `GenerateKey_ThreadSafety_MultipleThreadsGeneratingKeys` era `[Fact] void` e bloqueava com `Task.WaitAll(tasks.ToArray())`. Convertido para `async Task` trocando o bloqueio por `await Task.WhenAll(tasks)`. Build isolado do `Core.Test`: **0 erro(s)**, **xUnit1031 = 0** (os 19 avisos residuais do projeto são nullable, tratados na 6.2). Testes `StringHelperTest`: **11 aprovados, 0 falhas**. Remoção do gate ocorre na 4.8 (xUnit1031 estava concentrado só no Core.Test).

[x] 4.8 - Encolher o gate: remover códigos zerados de `MvpResidualWarnings`
- Recompilar a solução e remover de `MvpResidualWarnings` os códigos que agora estão em **0 na solução**: `LOGGEN002`, `ASPDEPR006`, `SYSLIB0057`, `CS0108`, `xUnit1031` e `CS0618` (se 4.2 e 4.3 o zeraram por completo). Confirmar `dotnet build ... /p:TreatWarningsAsErrors=true` **verde**.
- `src/Directory.Build.props`
- https://learn.microsoft.com/visualstudio/msbuild/msbuild-warnings-as-errors
- **Concluído 2026-07-17:** build `Release --no-incremental` da solução → **0 erro(s)**, recontagem dedup (§2) = **309 avisos em 13 códigos**. Removidos de `MvpResidualWarnings` em [`src/Directory.Build.props`](../src/Directory.Build.props) os 4 códigos que zeraram na solução inteira nesta fase: **`ASPDEPR006`** (4.5), **`SYSLIB0057`** (4.4), **`CS0108`** (4.6) e **`xUnit1031`** (4.7). **`CS0618` NÃO foi removido** — restam **4 ocorrências residuais** legítimas fora do escopo da Fase 4: `MongoDbResiliencyExtensions.cs:126` (shim de compatibilidade `new MongoDbResiliencyPolicy(...)` registrado por `IMongoDbResiliencyPolicy`) e `ResilienceDbContextExtensions.cs:477/478/480` (EFCore `MvpExecutionStrategy` auto-obsoleto). Comentário do gate atualizado documentando o motivo. `dotnet build src/Mvp24Hours.sln -c Release --no-incremental /p:TreatWarningsAsErrors=true` → **0 erro(s)** (as removidas viram erro, mas estão em 0). Baseline atualizado em [`warnings-baseline-v2.md`](./warnings-baseline-v2.md) + [`.json`](./warnings-baseline-v2.json). Gate: 18 → **14 códigos** (`CS86xx`×12 + `CS0618` + `NU1510`).

---

## FASE 5 — Nullable nos projetos de produção (por projeto)

> **Estratégia:** um projeto por tarefa/PR, do maior volume ao menor. Aplicar §1.A. Build isolado (§2). Não remover códigos CS86xx do gate ainda (são globais — sairão na §7). Ao terminar cada projeto, rodar os testes que o cobrem.
> **ADO:** US a criar.

[x] 5.1 - Nullable em `Mvp24Hours.Infrastructure.Data.MongoDb` (~172 CS8618 + demais)
- **Padrão:** §1.A. Maior ofensor. Predominam CS8618 (POCOs/opções/documentos). Distinguir: opções de configuração ⇒ `required`/default; documentos/mapas preenchidos pelo driver Mongo ⇒ `= null!` com comentário `// set by MongoDB driver`. Tratar também CS8602/CS8604 do projeto.
- Rodar `Mvp24Hours.Application.MongoDb.Test` (+ integração quando houver Docker).
- `src/Mvp24Hours.Infrastructure.Data.MongoDb/**`
- https://learn.microsoft.com/dotnet/csharp/nullable-references
- **Concluído 2026-07-17:** build isolado do projeto reduziu de **97 → 0** avisos próprios (o "8/10 Aviso(s)" residual do log vem de projetos referenciados — `Core/ObjectHelper` CS8603 e `Cqrs/PaginatedQuery` CS8618, tratados em 5.7/5.8 — mais o CS0618 do shim da 4.3, ver abaixo). Padrões aplicados por família:
  - **Opções de configuração** (`MongoDbOptions`, `MongoDbBulkOperationOptions`, `TimeSeriesOptions`, `MongoDbObservabilityOptions`, `MongoDbTextSearchOptions`, `MongoDbSchemaValidationOptions`, `MongoDbConnectionPoolOptions`, atributos de índice, `EncryptedFieldAttribute`): campos obrigatórios com default seguro (`= string.Empty;`); campos genuinamente opcionais (documentados como "Default is null" / "If not specified") tornados anuláveis (`string?`, `BsonDocument?`, `string[]?`).
  - **DTOs/result populados por driver/serviço** (`ShardInfo/ShardDistribution/ShardStats`, `GeoPolygon`, `SchemaValidationResult`, `GeoNearResult<T>`, `TextSearchResult<T>`, `QueryExplainResult`, `IndexInfo`, `CollectionStats`, logs internos de `MongoDbSlowQueryLogger`/`MongoDbStructuredLogger`): strings → `= string.Empty;`, coleções/arrays → `= [];`, `BsonDocument`/`Stopwatch` sempre preenchidos → `= new();`, genéricos → `= default!;` com comentário, conforme sempre-preenchido vs. opcional (`BsonDocument?`).
  - **Ciclo de vida do contexto** (`Mvp24HoursContext`): `MongoClient`/`Database` → `= null!;` (`// set by Configure(...)`), `Session` → `= null!;` (`// set by StartSession(...)`), e `RowLevelSecurity` inicializado no ctor básico `(string, string)`.
  - **UoW** (`UnitOfWork`/`UnitOfWorkAsync`): `serviceProvider` marcado `IServiceProvider?` (só setado no ctor `[ActivatorUtilitiesConstructor]`) + guarda em `GetRepository`; `_logger` estático de `MongoDbBulkOperationsExtensions` → `ILogger?`.
  - **Eventos/exceções**: eventos de `MongoDbConnectionManager` → `EventHandler<...>?`; `MongoDbOperationTimeoutException.OperationType` → `string?`.
  - **CS8620/CS8622/CS8764**: `MongoDbDurationTracker` tupla `Collection` ⇒ `collection ?? string.Empty`; `MongoDbConnectionPoolMetrics.CollectMetrics(object? state)` alinhado ao `TimerCallback`; `EncryptedStringSerializer.Deserialize` alinhado à base (`override string`) com `Decrypt(...)!` justificado (cipher text não-nulo).
  - **Ajustes de ripple:** `MongoDbIndexManager.CreateIndexOptions<T>` passou a aceitar `string? name` (nome de índice opcional no driver).
- **Residual conhecido (fora de escopo de 5.1):** 1 `CS0618` em `Extensions/MongoDbResiliencyExtensions.cs:126` (shim de compatibilidade da 4.3, `new MongoDbResiliencyPolicy(...)`), a ser tratado antes de a Fase 7 remover `CS0618` do gate.
- **Testes** `Mvp24Hours.Infrastructure.Data.MongoDb.Test` (`Category=Unit`): **129 aprovados, 0 falhas** (integração `Application.MongoDb.Test` requer Docker). Códigos CS86xx permanecem no gate (globais — saem na §7).

[x] 5.2 - Nullable em `Mvp24Hours.Application` (~154)
- **Padrão:** §1.A. Projeto de produção não coberto na Fase 4 da v1 (CS8604/CS8602/CS8618 dispersos nos serviços de aplicação). Ajustar assinaturas para refletir a real anulabilidade de parâmetros/retornos dos serviços.
- Rodar `Mvp24Hours.Application.Test` + os testes por provedor (grupo B).
- `src/Mvp24Hours.Application/**`
- https://learn.microsoft.com/dotnet/csharp/nullable-references
- **Concluído 2026-07-17:** build isolado do projeto foi de **104 → 0** avisos próprios (os 27 residuais do log vêm de projetos referenciados — EFCore `CS8620/CS8618/CS8631/CS8629/CS0618` e Core `CS8603` — tratados em 5.3/5.8). Correção **na causa** (contratos), não no sintoma:
  - **`criteria` nulável no contrato (raiz de ~94 CS8604):** os métodos de consulta em `IQuery`/`IQueryAsync`/`IStreamingQueryAsync` (Core.Contract.Data) e nos contratos de serviço `IQueryService(Async)`, `IReadOnlyApplicationService(Async)`, `IApplicationServiceWithDto(Async)`, `IApplicationServiceWithDtoSeparate(Async)` declaravam `IPagingCriteria criteria` **não-anulável**, embora **todas** as implementações (EFCore/MongoDb/Caching/fakes) já usassem `IPagingCriteria?` e os overloads sem-critério passassem `null`. Alinhei o contrato para `IPagingCriteria? criteria` — sem ripple nas implementações (já eram nuláveis). Isso zerou o CS8604 de `criteria` **na solução inteira** (dedup 309 → **192**).
  - **`GetById`/`GetByIdAsync` (serviços de entidade) → `IBusinessResult<TEntity?>`:** o repositório já retorna `TEntity?` (entidade pode não existir) e `BusinessResult<T>.Data` é `T?`; portanto o retorno honesto é `IBusinessResult<TEntity?>`. Ajustados os 4 contratos de entidade (`IQueryService`, `IReadOnlyApplicationService` + async) e as implementações `ApplicationServiceBase`, `QueryServiceBase`, `RepositoryService` (+ `*Async`), `CacheableApplicationServiceBaseAsync`, `CacheableQueryServiceBaseAsync` (overrides + helper `GetByIdWithCacheAsync`) e `ObservableApplicationServiceBaseAsync`. Resolveu os CS8619 `IBusinessResult<TEntity?>`↔`<TEntity>`. Contratos de DTO (`IBusinessResult<TDto>`) **mantidos** (não apresentavam CS8619).
  - **Supressões `null!` removidas:** nos overloads delegantes (`List()/GetBy(clause)/GetById(id)` → passam `null`) e nos ramos de especificação (`((TEntity?)null!)` → `((TEntity?)null)`, `_repository.GetBy(..., null!)` → `null`), agora que `criteria` é anulável.
  - **DTO `GetById(Async)` (CS8604 em `MapToDto`):** guarda de nulo — se a entidade não é encontrada, retorna `new BusinessResult<TDto>()` em vez de mapear `null` (evita NPE do mapper). Adicionado `using Mvp24Hours.Core.ValueObjects.Logic;` em `ApplicationServiceBaseWithDtoAsync`.
  - **Bulk async (CS8604 em `entities`):** `ValidateEntities` aceita `IList<TEntity>?`, mas a operação em massa **exige** a lista — adicionado `ArgumentNullException.ThrowIfNull(entities)` (pré-condição, §1.A) em `BulkAddAsync`/`BulkModifyAsync`/`BulkRemoveAsync`; `entities?.Count ?? 0` → `entities.Count`.
  - **Cache CS8603:** removido o `result?.` redundante (`result` é `IList<TEntity>` não-nulo vindo do repositório) em `GetSingle/FirstBySpecificationAsync`.
- **Build/gate:** `dotnet build src/Mvp24Hours.sln -c Release --no-incremental` → **0 erro(s)**; nenhum código novo fora do gate (12 códigos residuais, todos em `MvpResidualWarnings`); `dotnet build ... /p:TreatWarningsAsErrors=true` → **0 erro(s)**. Códigos CS86xx permanecem no gate (globais — saem na §7).
- **Testes (Debug, `Category!=Integration`):** `Mvp24Hours.Application.Test` **264 aprovados/0 falhas**; `Mvp24Hours.Application.SQLServer.Test` **232 aprovados, 4 ignorados, 0 falhas**; `Mvp24Hours.Application.PostgreSql.Test` **96 aprovados/0 falhas** (integração por provedor requer Docker).

[x] 5.3 - Nullable em `Mvp24Hours.Infrastructure.Data.EFCore` (~14 CS8618 + demais, após 4.2)
- **Padrão:** §1.A. Restante do EFCore após a migração de SqlClient (4.2). POCOs/entidades de mapeamento ⇒ `= null!` com comentário `// set by EF Core` quando aplicável; opções ⇒ `required`/default.
- Rodar os testes de EFCore (grupo B + integração com Docker).
- `src/Mvp24Hours.Infrastructure.Data.EFCore/**`
- https://learn.microsoft.com/ef/core/miscellaneous/nullable-reference-types
- **Concluído 2026-07-17:** build isolado do projeto foi de **27 → 0** avisos próprios (os residuais do log vêm de projetos referenciados — `Core/ObjectHelper` CS8603, tratado em 5.8 — mais os 3 CS0618 do `MvpExecutionStrategy` auto-obsoleto em `ResilienceDbContextExtensions.cs`, conhecidos da 4.8/fora de escopo, a sair na Fase 7). Correção **na causa**, por família:
  - **UoW `serviceProvider` (CS8618×4):** `UnitOfWork`, `UnitOfWorkAsync`, `UnitOfWorkWithEvents`, `UnitOfWorkWithEventsAsync` só setam `serviceProvider` no ctor `[ActivatorUtilitiesConstructor]`; campo marcado `IServiceProvider?` + guarda em `GetRepository` (`InvalidOperationException` se criado sem provider), alinhado ao padrão já aplicado no MongoDb (5.1).
  - **`Mvp24HoursContext` (CS8618×2):** `EntityLogBy` (virtual, opcional — pode não haver usuário) → `object?`; ripple CS8600×2 nos casts `(dynamic)EntityLogBy` → `(dynamic?)EntityLogBy` em `ApplyLogRules`.
  - **`DatabaseExtensions` seeder (CS8620/CS8631×4 cada + CS8629×2):** `context` obtido por `GetService<TContext>()` (anulável) era passado a `InvokeSeeder*<TContext>` que exige `TContext` não-nulo — como o seeding **exige** o contexto (pré-condição), trocado por `GetRequiredService<TContext>()`; `retry.Value` → `retry ?? 0`.
  - **`Reference` (CS8620×4):** `EntityEntry<T>.Reference<TProperty>` espera `Expression<Func<T, TProperty?>>` (nav de referência é opcional), mas `LoadRelation` recebe `Expression<Func<T, TProperty>>` (contrato `IQueryRelation`, compartilhado com MongoDb/Caching/fakes — **fora de escopo** alterar). Mantido local ao EFCore reconstruindo a expressão com a anotação correta: `Expression.Lambda<Func<T, TProperty?>>(propertyExpression.Body, propertyExpression.Parameters)` (mesmo corpo, fortemente tipado, sem supressão) em `Repository`/`ReadOnlyRepository`/`RepositoryAsync`/`ReadOnlyRepositoryAsync`.
  - **`EncryptedValueConverters` (CS8620×2 + CS8618×1):** `HasEncryptedConversion(PropertyBuilder<byte[]>)` e `HasEncryptedJsonConversion<T>(PropertyBuilder<T>)` alinhados ao tipo-modelo anulável dos conversores (`ValueConverter<byte[]?,…>`/`ValueConverter<T?,…>`) → `PropertyBuilder<byte[]?>`/`PropertyBuilder<T?>`; `EncryptedAttribute.BlindIndexPropertyName` (opcional, "If not specified…") → `string?`.
- **Build/gate:** `dotnet build src/Mvp24Hours.sln -c Release --no-incremental` → **0 erro(s)**; nenhum código novo fora do gate (todos os residuais são CS86xx + CS0618, já em `MvpResidualWarnings`); `dotnet build ... /p:TreatWarningsAsErrors=true` → **0 erro(s)**. Códigos CS86xx permanecem no gate (globais — saem na §7).
- **Testes (Debug, `Category!=Integration` / InMemory grupo B):** `Mvp24Hours.Application.SQLServer.Test` **232 aprovados, 4 ignorados, 0 falhas**; `Mvp24Hours.Application.PostgreSql.Test` **96 aprovados, 0 falhas** (integração por provedor requer Docker).

[x] 5.4 - Nullable em `Mvp24Hours.Infrastructure.RabbitMQ` (~18 CS8618 + demais, após 4.1)
- **Padrão:** §1.A. Restante do RabbitMQ após LOGGEN002 (4.1). Atenção a handlers/consumers (CS8622) e opções de conexão (CS8618).
- Rodar `Mvp24Hours.Application.RabbitMQ.Test` (Docker) quando disponível.
- `src/Mvp24Hours.Infrastructure.RabbitMQ/**`
- https://learn.microsoft.com/dotnet/csharp/nullable-references
- **Concluído 2026-07-17:** build isolado do projeto foi de **22 → 0** avisos próprios (os 14 residuais do log vêm de projetos referenciados — `Cqrs/PaginatedQuery` CS8618 e `Core/ObjectHelper` CS8603, tratados em 5.7/5.8). Correção **na causa**, por família:
  - **CS8622 (event handlers, 6 assinaturas):** os handlers `OnConnectionShutdown`/`OnCallbackException`/`OnConnectionBlocked` em `MvpRabbitMQConnection` declaravam `object sender`, divergindo do delegado `EventHandler<T>` (`object? sender`). Alinhados para `object? sender` (assinatura da base, §1.A/CS8622). Zerou CS8622 **na solução inteira** (14 → 0; permanece no gate até a §7).
  - **`MvpRabbitMQConnection._connection` (CS8618):** só é atribuído em `TryConnect()` (não no ctor) e o ciclo de vida já é guardado por `IsConnected`/checagens de nulo → `IConnection?`. Ripple resolvido **sem supressão**: narrowing explícito em `CreateModel` (`if (!IsConnected || _connection is null) throw`), no bloco de assinatura de eventos (`if (IsConnected && _connection is not null)`) e no `Dispose` (unsubscribe/`Dispose` envoltos em `if (_connection is not null)`).
  - **Opções de configuração (`RabbitMQOptions`, CS8618×6):** campos obrigatórios com "não-setado" natural = vazio → default seguro (`RoutingKey`/`QueueName` = `string.Empty;`, consistente com os `?? string.Empty`/`.HasValue()` já usados no `MvpRabbitMQClient`); campos genuinamente opcionais (driver aceita ausência) → anuláveis (`ExchangeArguments`/`QueueArguments` `Dictionary<string, object>?`, alinhado ao `?? []`; `BasicProperties` `IBasicProperties?`, alinhado ao `?? properties`).
  - **`RabbitMQConnectionOptions.Configuration` (CS8618):** genuinamente opcional (usado com `!= null`/`??=` nos builders) → `RabbitMQConnection?`.
  - **`RabbitMQHostedOptions` (CS8618×2):** `Callback` (`TimerCallback` exigido pelo `new Timer(...)`) → `required` (§1.A — consumidor deve fornecer); `State` (estado do timer, aceita nulo) → `object?`; campo `state` do `MvpRabbitMQHostedService` alinhado para `object?`.
- **Build/gate:** `dotnet build src/Mvp24Hours.sln -c Release --no-incremental` → **0 erro(s)**; recontagem dedup (§2) = **150 avisos em 8 códigos** (CS8618 34, CS8604 34, CS8619 32, CS8602 29, CS8600 11, CS0618 4, CS8625 4, CS8603 2). Nenhum código novo fora do gate; `dotnet build ... /p:TreatWarningsAsErrors=true` → **0 erro(s)**. CS86xx (incl. CS8622, agora 0 na solução) permanecem no gate — saem na §7.
- **Testes:** `Mvp24Hours.Application.RabbitMQ.Test` compila contra as novas assinaturas (0 erro); todos os seus testes são `Category=Integration` (exigem RabbitMQ/Docker), sem testes unit para executar — a compilação verde é o sinal de não-regressão.

[x] 5.5 - Nullable em `Mvp24Hours.WebAPI` (~10 CS8618 + demais, após 4.1/4.5)
- **Padrão:** §1.A. Restante do WebAPI após LOGGEN002 (4.1) e ASPDEPR006 (4.5). Middlewares/filtros/opções.
- Rodar `Mvp24Hours.WebAPI.Test`.
- `src/Mvp24Hours.WebAPI/**`
- https://learn.microsoft.com/dotnet/csharp/nullable-references
- **Concluído 2026-07-17:** build isolado do projeto foi de **5 → 0** avisos próprios (os 7 residuais do log vêm de projetos referenciados — `Cqrs/PaginatedQuery` CS8618×6 e `Core/ObjectHelper` CS8603, tratados em 5.7/5.8). Todos os 5 eram CS8618, em 2 famílias:
  - **`CorsOptions` (CS8618×4):** `Origin`/`Headers`/`Methods`/`Credentials` são valores de CORS **genuinamente opcionais** — cada acesso em `CorsMiddleware` já é guardado por `.HasValue()` e recai em `"*"` quando `AllowAll`. Tornados anuláveis (`string?`, §1.A opção c). Ripple resolvido tornando os locais `originCors`/`headersCors`/`methodsCors` também `string?` (evita CS8600 no ramo `else` que copia as opções).
  - **`ModelBinder<T>.Data` (CS8618×1):** sempre preenchido pela *factory* `BindAsync` (`data ?? new T()`), nunca nulo. Como a constraint `IExtensionBinder<ModelBinder<T>>` (`where T : class, new()`) **exige** ctor público sem-parâmetro, não é possível setar só via ctor privado; inicializado com default seguro `= new();` (§1.A opção b), reaproveitando a constraint `new()` de `T`.
- **Build/gate:** `dotnet build src/Mvp24Hours.sln -c Release --no-incremental` → **0 erro(s)**; recontagem dedup (§2) = **145 avisos em 8 códigos** (WebAPI deixou de aparecer na lista por projeto). Nenhum código novo fora do gate; `dotnet build ... /p:TreatWarningsAsErrors=true` → **0 erro(s)**. Códigos CS86xx permanecem no gate (globais — saem na §7). Baseline atualizado em [`warnings-baseline-v2.json`](./warnings-baseline-v2.json).
- **Testes:** `Mvp24Hours.WebAPI.Test` (`Category!=Integration`): **5 aprovados, 0 falhas**.

[x] 5.6 - Nullable em `Mvp24Hours.Infrastructure.Pipe` (após 4.1)
- **Padrão:** §1.A. Restante do Pipe após LOGGEN002 (4.1).
- Rodar `Mvp24Hours.Application.Pipe.Test` / `Mvp24Hours.Infrastructure.Pipe.Test`.
- `src/Mvp24Hours.Infrastructure.Pipe/**`
- https://learn.microsoft.com/dotnet/csharp/nullable-references
- **Concluído 2026-07-17:** build isolado do projeto (§2) acusou **0 avisos próprios** — nenhuma correção de código necessária. Conforme o baseline §3, todo o residual do Pipe (36 linhas) era **LOGGEN002**, já zerado na tarefa 4.1; o projeto **não tinha avisos nullable** (`Nullable=enable` herdado de [`Directory.Build.props`](../src/Directory.Build.props) e código já conforme). Os únicos 2 avisos do log são `CS8603` em `Mvp24Hours.Core/Helpers/ObjectHelper.cs` — projeto **referenciado**, tratado na 5.8. **Testes** `Mvp24Hours.Application.Pipe.Test` (`Category!=Integration`): **78 aprovados, 0 falhas** (não há projeto `Mvp24Hours.Infrastructure.Pipe.Test`; os 3 CS8618 vistos no log de teste são do *próprio projeto de teste*, escopo da Fase 6/6.5). Sem mudança de código ⇒ gate inalterado (permanece verde com `/p:TreatWarningsAsErrors=true`). Códigos CS86xx seguem no gate (globais — saem na §7).

[x] 5.7 - Nullable em `Mvp24Hours.Infrastructure.Cqrs` (~12 CS8618)
- **Padrão:** §1.A.
- Rodar `Mvp24Hours.Infrastructure.Cqrs.Test`.
- `src/Mvp24Hours.Infrastructure.Cqrs/**`
- https://learn.microsoft.com/dotnet/csharp/nullable-references
- **Concluído 2026-07-17:** build isolado do projeto acusava **12 CS8618** (dedup 6), **todos** concentrados em `Queries/PaginatedQuery.cs` — os 3 campos de *backing* `_orderByAscendingExpr`, `_orderByDescendingExpr` e `_navigationExpr` de `PaginatedQuery<TEntity, TResponse>`. Eles **não** são setados no construtor: são inicializados *lazy* nos getters via `??= []` (`OrderByAscendingExpr`/`OrderByDescendingExpr`/`NavigationExpr`). Portanto o tipo honesto do campo é anulável (§1.A opção c): marcados `IList<Expression<Func<TEntity, dynamic>>>?`. Os getters permanecem retornando não-anulável (após o `??= []` o valor nunca é nulo). Sem ripple — os campos só são acessados pelos próprios getters. Build isolado do Cqrs: **CS8618 = 0** (os 2 CS8603 residuais do log vêm do projeto referenciado `Core/Helpers/ObjectHelper.cs:41`, tratado em 5.8).
- **Build/gate:** `dotnet build src/Mvp24Hours.sln -c Release --no-incremental /p:TreatWarningsAsErrors=true` → **0 erro(s)** (143 avisos, todos no gate; nenhum código novo escapou). Códigos CS86xx permanecem no gate (globais — saem na §7).
- **Testes:** `Mvp24Hours.Infrastructure.Cqrs.Test` (`Category!=Integration`): **347 aprovados, 0 falhas** (os 3 CS8604 do log são do próprio projeto de teste, escopo da 6.5).

[x] 5.8 - Nullable em `Mvp24Hours.Core` (residual ~2)
- **Padrão:** §1.A. Fechar o residual mínimo do Core.
- Rodar `Mvp24Hours.Core.Test`.
- `src/Mvp24Hours.Core/**`
- https://learn.microsoft.com/dotnet/csharp/nullable-references
- **Concluído 2026-07-17:** build isolado do projeto acusava **1 CS8603** (dedup 1; 2 linhas), em `Helpers/ObjectHelper.cs:41` — `ConvertToDynamic(object obj)` declara retorno **não-anulável** `dynamic`, mas `json.ToDeserialize<ExpandoObject>()` retorna `ExpandoObject?`. O contrato honesto é **não-nulo** (os chamadores em `JsonExtensions` e nos testes acessam propriedades dinâmicas do resultado; o `json` já cai em `"{}"` quando `obj` é nulo, sempre desserializando para um `ExpandoObject` vazio, nunca `null`). Aplicado §1.A/CS8603 (garantir não-nulo): `?? new ExpandoObject()` — mantém o retorno `dynamic` não-anulável sem supressão. Build isolado do Core: **0 aviso(s) / 0 erro(s)**. O CS8603 remanescente na solução (dedup 1) passou a ser só de **teste** (`WebRequestTest.cs:22`, escopo da Fase 6).
- **Build/gate:** `dotnet build src/Mvp24Hours.sln -c Release --no-incremental /p:TreatWarningsAsErrors=true` → **0 erro(s)**; recontagem dedup (§2) = **142 avisos em 8 códigos** (`Mvp24Hours.Core` deixou de aparecer na lista por projeto). Nenhum código novo fora do gate. Baseline atualizado em [`warnings-baseline-v2.json`](./warnings-baseline-v2.json). Códigos CS86xx permanecem no gate (globais — saem na §7). **Com a 5.8, a Fase 5 (nullable de produção) está concluída** — todo o residual nullable restante está nos projetos de teste (Fase 6).
- **Testes:** `Mvp24Hours.Core.Test` (`Category!=Integration`): **788 aprovados, 0 falhas** (uma falha *flaky* isolada de concorrência não reproduziu em reexecuções).

---

## FASE 6 — Nullable nos projetos de teste (por projeto)

> **Estratégia:** mesmo fluxo (§2) e padrão (§1.A). Em testes é comum CS8618 em fixtures/DTOs e CS8604 em setups; preferir inicialização com default a `= null!`. Muitos avisos somem "de graça" após corrigir as assinaturas de produção (§5) — por isso os testes vêm **depois**.
> **ADO:** US a criar.

[x] 6.1 - Nullable em `Mvp24Hours.Application.SQLServer.Test` (~44)
- **Padrão:** §1.A. `src/Tests/Mvp24Hours.Application.SQLServer.Test/**`
- **Concluído 2026-07-17:** build isolado do projeto foi de **37 → 0** avisos próprios (os 3 residuais do log são `CS0618` em `Infrastructure.Data.EFCore/Extensions/ResilienceDbContextExtensions.cs:477/478/480` — `MvpExecutionStrategy` auto-obsoleto do projeto referenciado, conhecidos da 4.8, a sair na Fase 7). Correção **na causa/guarda**, por família:
  - **CS8618 (6, entidades de teste):** propriedades `string` obrigatórias (`Name`/`Description`) de `Customer`/`Contact`/`CustomerBasic`/`ContactBasic`/`CustomerBasicLog`/`ContactBasicLog` sem inicialização → default seguro `= string.Empty;` (§1.A opção b / nota §6 "preferir default a `= null!`").
  - **CS8619 (12, tipo declarado):** após a 5.2, `GetById(Async)` de entidade retorna `IBusinessResult<TEntity?>`; os locais declarados `IBusinessResult<TEntity> result = service.GetById(...)` (Test1QueryService/BasicService/BasicLogService/LogService/Test2QueryServiceAsync/Test3/Test4) alinhados para `IBusinessResult<TEntity?>`.
  - **CS8602/CS8604 (deref/arg possivelmente nulo em comandos):** entidade/lista obtida por `GetById`/`List` (dados semeados) é pré-condição do teste — adicionado `Assert.NotNull(...)` (guarda `[NotNull]` do xUnit 2.9.3 estreita a nulidade, §1.A/CS8602-CS8604) antes de `Modify`/`Remove`/`RemoveById`/deref, em `Test3CommandService`/`Test4CommandServiceAsync`; nos testes de navegação, `data = result.GetDataValue(); Assert.NotNull(data);` antes de `data.Contacts...`.
  - **CS8604 (setup):** `AppSettings.GetConnectionString("DataContext")` (anulável) → `?? throw new InvalidOperationException(...)` em `Startup`/`StartupAsync` (§1.A/CS8604 obrigatório).
  - **CS8625 (2, testes de null deliberado):** `spec.IsSatisfiedBy(null!)` e `new CustomerByNameSpecification(null!)` em `Test8SpecificationPattern` — supressão `null!` **com comentário** justificando que o input nulo é intencional (verificar comportamento/guarda), §1 exceção.
- **Build/gate:** `dotnet build src/Mvp24Hours.sln -c Release --no-incremental /p:TreatWarningsAsErrors=true` → **0 erro(s)**; recontagem dedup (§2) = **108 avisos em 8 códigos** (`Mvp24Hours.Application.SQLServer.Test` deixou de aparecer na lista por projeto). Nenhum código novo fora do gate. Baseline atualizado em [`warnings-baseline-v2.json`](./warnings-baseline-v2.json). Códigos CS86xx permanecem no gate (globais — saem na §7).
- **Testes** (Debug/InMemory, `Category!=Integration`): **232 aprovados, 4 ignorados, 0 falhas**.

[x] 6.2 - Nullable em `Mvp24Hours.Core.Test` (~38)
- **Padrão:** §1.A. `src/Tests/Mvp24Hours.Core.Test/**`
- **Concluído 2026-07-17:** build isolado do projeto foi de **18 → 0** avisos próprios (dedup). Todos os avisos eram **testes de null deliberado** (`*_WithNull_*`) e derefs de resultados de teste. Correção **na causa** — as APIs de produção do `Mvp24Hours.Core` exercitadas por esses testes *declaravam parâmetros não-anuláveis mas já tratavam null internamente* (contrato desonesto). Alinhadas as assinaturas ao comportamento real (§1.A/CS8604 opção "tornar o parâmetro `T?` se ele aceita nulo"), seguindo o estilo que o próprio código já usava (`EnumerableExtensions.IsList`/`AnySafe` já eram anuláveis):
  - **`StringExtensions`:** `Truncate`/`Reticence`/`SqlSafe` → `this string? text` (corpos já faziam `text ?? string.Empty` / `text.NullSafe()`).
  - **`ConvertExtensions.NullSafe`:** `this string? target` (corpo já `target ?? string.Empty`; necessário pelo ripple do `SqlSafe`).
  - **`EnumerableExtensions`:** `IsDictionary(this object? Value)`, `AnyOrNotNull<T>(this IEnumerable<T>? source)` (ambos overloads), `ContainsKeySafe<TKey,TValue>(this IDictionary<TKey,TValue>? source, …)` (todos com guarda `== null`/`!= null` no corpo).
  - **`GuidExtensions.IsValidGuid`:** `this string? oid` (corpo `string.IsNullOrEmpty(oid)`).
  - **`Enumeration<TEnum>`:** operadores `==`/`!=`/`<`/`<=`/`>`/`>=` e conversões implícitas (`int`/`string`) → operandos/parâmetro anuláveis (`Enumeration<TEnum>?`), consistente com os corpos que já testam `left is null`/`enumeration?.`.
  - **`JsonHelper.Serialize<T>`:** `T? dto` (serializa null → `"null"`), resolve o CS8625 do teste `Serialize_WithNull_ReturnsNull`.
- **Lado do teste (guarda/alvo `T?`, §1.A):** `Assert.NotNull(...)` antes de deref de resultados de `Deserialize`/`Clone` (`JsonHelperTest.cs:197`, `ObjectHelperTest.cs:36/83`); `string? result = id;` na conversão implícita de `EntityId` (`EntityIdTest.cs:367`, CS8600 → alvo anulável). Nenhuma supressão `null!` nova.
- **Bônus:** por corrigir a **causa** nas APIs de produção, o CS8604 desses mesmos helpers zerou também nos demais projetos de teste que os consomem — dedup da solução caiu de **108 → 90** avisos (8 códigos, todos CS86xx + CS0618 no gate).
- **Build/gate:** `dotnet build src/Mvp24Hours.sln -c Release --no-incremental` → **0 erro(s)**; nenhum código novo fora do gate; `dotnet build ... /p:TreatWarningsAsErrors=true` → **0 erro(s)**. Baseline atualizado em [`warnings-baseline-v2.json`](./warnings-baseline-v2.json) (`Mvp24Hours.Core.Test` deixou de aparecer na lista por projeto). Códigos CS86xx permanecem no gate (globais — saem na §7).
- **Testes:** `Mvp24Hours.Core.Test` (`Category!=Integration`): **788 aprovados, 0 falhas**.

[x] 6.3 - Nullable em `Mvp24Hours.Infrastructure.Data.MongoDb.Test` (parte nullable, após 4.3)
- **Padrão:** §1.A. `src/Tests/Mvp24Hours.Infrastructure.Data.MongoDb.Test/**`
- **Concluído 2026-07-18:** build isolado do projeto (§2) acusou **0 avisos próprios** — nenhuma correção de código necessária. O residual histórico do projeto (baseline v1: ~10 linhas "MongoDb.Test(unit)" + 32 CS0618) já havia sido zerado nas tarefas anteriores: CS0618 na **4.3** (realocação `MongoDbResiliencyPolicyTests` → `NativeMongoDbResilienceExtensionsTests`) e o nullable residual absorvido pelas assinaturas de produção da Fase 5 / baseline pós-6.2 (o projeto **não aparece** em `byProject` de [`warnings-baseline-v2.json`](./warnings-baseline-v2.json)). O único aviso do log é `CS0618` em `Infrastructure.Data.MongoDb/Extensions/MongoDbResiliencyExtensions.cs:126` — projeto **referenciado**, shim conhecido da 4.3/5.1, a sair na Fase 7. **Nota:** os 8 avisos nullable de `Mvp24Hours.Application.MongoDb.Test` (baseline) ficam no escopo da **6.5** (`MongoDb.Test`).
- **Build/gate:** sem mudança de código ⇒ gate inalterado (permanece verde com `/p:TreatWarningsAsErrors=true`). Códigos CS86xx seguem no gate (globais — saem na §7).
- **Testes** `Category=Unit`: **129 aprovados, 0 falhas**.

[x] 6.4 - Nullable em `Mvp24Hours.Application.MySql.Test` / `.PostgreSql.Test` / `.Redis.Test` (~28 cada)
- **Padrão:** §1.A. Fixtures e dados de teste compartilham o mesmo formato entre os três; aplicar a mesma correção. `src/Tests/Mvp24Hours.Application.MySql.Test/**`, `.PostgreSql.Test/**`, `.Redis.Test/**`
- **Concluído 2026-07-18:** builds isolados foram de **20/20/14 → 0** avisos próprios (MySql/PostgreSql/Redis; os 3 CS0618 residuais nos logs MySql/PostgreSql vêm do `MvpExecutionStrategy` auto-obsoleto em EFCore referenciado — Fase 7). Mesmo padrão da 6.1 (SQLServer) aplicado nos três:
  - **MySql + PostgreSql (espelhados):** CS8618 `Customer.Name`/`Contact.Description` → `= string.Empty;`; CS8604 connection string → `?? throw new InvalidOperationException(...)` em `Startup`/`StartupAsync`; CS8619 `IBusinessResult<Customer>` → `IBusinessResult<Customer?>` + `Assert.NotNull(data)` antes de deref de navegação em `Test1`/`Test2`; CS8602/CS8604 em `Test3`/`Test4` → `Assert.NotNull(...)` antes de `Modify`/`Remove`/`RemoveById`.
  - **Redis:** CS8618 `Customer.Name` → `= string.Empty;`; CS8604 `GetSettings(...)` → `?? throw`; CS8618 `serviceProvider` → `Setup()` passa a **retornar** `IServiceProvider` (local por teste, sem campo); CS8600 `GetString`/`GetStringAsync` → alvo `string?`.
- **Build/gate:** `dotnet build src/Mvp24Hours.sln -c Release --no-incremental` → **0 erro(s)**; recontagem dedup (§2) = **36 avisos em 8 códigos** (MySql/PostgreSql/Redis deixaram de aparecer em `byProject`). Nenhum código novo fora do gate; `dotnet build ... /p:TreatWarningsAsErrors=true` → **0 erro(s)**. Baseline atualizado em [`warnings-baseline-v2.json`](./warnings-baseline-v2.json). Códigos CS86xx permanecem no gate (globais — saem na §7).
- **Testes** (Debug/InMemory, `Category!=Integration`): `MySql.Test` **96 aprovados, 0 falhas**; `PostgreSql.Test` **96 aprovados, 0 falhas**. `Redis.Test`: todos os testes são `Category=Integration` (exigem Redis/Docker) — compilação verde é o sinal de não-regressão.

[x] 6.5 - Nullable nos testes restantes (`Pipe.Test`, `Patterns.Test`, `MongoDb.Test`, `Cqrs.Test`, `RabbitMQ.Test`, `CronJob.Test`)
- **Padrão:** §1.A. Volumes pequenos (≤14 cada). `src/Tests/**`
- **Concluído 2026-07-18:** builds isolados foram de **7/5/8/3/2/2 → 0** avisos próprios (Pipe/Patterns/MongoDb/Cqrs/RabbitMQ/CronJob). Incluído também `Application.Integration.Test` (5 CS8619 residuais no baseline pós-6.4, mesmo padrão CS8619 da 6.1). Correção **na causa/guarda**:
  - **Pipe.Test:** CS8600 `default` → exceções anuláveis (`ArgumentNullException?`/`ArgumentOutOfRangeException?`); CS8618 em DTOs locais `Person`/`CC` → `= string.Empty;` / `= new();`.
  - **Patterns.Test:** CS8618 `_server` → `WireMockServer?`; CS8604/CS8603 `server.Url` → `?? throw new InvalidOperationException(...)`; CS8625 `_server = null` resolvido pelo tipo anulável.
  - **MongoDb.Test:** CS8618 `Customer.Name`/`Contact.Description` → `= string.Empty;`; CS8618 `serviceProvider` → `Setup()` retorna `IServiceProvider` (local por teste, padrão Redis/6.4); CS8619 `IBusinessResult<Customer>` → `IBusinessResult<Customer?>` + `Assert.NotNull(customer)` antes de deref.
  - **Cqrs.Test:** CS8604 `Assert.Contains` em `OrderBy`/`Navigation`/`Includes` anuláveis → `Assert.NotNull(...)` antes (estreita nulidade).
  - **RabbitMQ.Test:** CS8618 `CustomerEvent.Name` → `= string.Empty;`; CS8618 `serviceProvider` → setups retornam `IServiceProvider`.
  - **CronJob.Test:** CS8604 `_serviceProvider` anulável → `ArgumentNullException.ThrowIfNull` / local não-nulo após `EnsureServiceProvider()`.
  - **Integration.Test (bônus):** CS8619 `IBusinessResult<Category>` → `IBusinessResult<Category?>` (5 ocorrências).
- **Build/gate:** `dotnet build src/Mvp24Hours.sln -c Release --no-incremental` → **0 erro(s)**; recontagem dedup (§2) = **4 avisos em 1 código** (só `CS0618` residual conhecido da 4.8/Fase 7 em EFCore×3 + MongoDb×1). Todos os CS86xx da solução **zerados**. `dotnet build ... /p:TreatWarningsAsErrors=true` → **0 erro(s)**. Baseline atualizado em [`warnings-baseline-v2.json`](./warnings-baseline-v2.json). Códigos CS86xx permanecem no gate até a §7 (já em 0 na solução). **Com a 6.5, a Fase 6 (nullable de testes) está concluída.**
- **Testes** (Debug, `Category!=Integration`): `Pipe.Test` **78 aprovados, 0 falhas**; `Patterns.Test` **20 aprovados, 0 falhas**; `Cqrs.Test` **346 aprovados, 1 falha flaky** (`DispatchEventsAsync_WithMultipleEntities_ShouldDispatchFromAll` — passou em reexecução isolada); `CronJob.Test` **89 aprovados, 2 falhas pré-existentes** (`TestResilientCronJob` exige `ExecutionTracker` no DI em testes que não o registram — independente desta tarefa). MongoDb/RabbitMQ/Integration: todos `Category=Integration` (exigem Docker) — compilação verde é o sinal de não-regressão.

---

## FASE 7 — Fechamento do gate estrito e validação final

> **ADO:** US a criar.

[x] 7.1 - Zerar `MvpResidualWarnings` e reativar o gate estrito
- Após §5 e §6, recompilar a solução (`Release --no-incremental`) e remover **todos** os códigos CS86xx (e qualquer resíduo) de `MvpResidualWarnings`. Manter **somente** `NU1510` se o pin de segurança da tarefa 7.1 (v1) ainda exigir (reavaliar). Confirmar `dotnet build src/Mvp24Hours.sln -c Release --no-incremental /p:TreatWarningsAsErrors=true` → **0 erro(s) / 0 aviso(s)**.
- `src/Directory.Build.props`
- https://learn.microsoft.com/visualstudio/msbuild/msbuild-warnings-as-errors
- **Concluído 2026-07-18:** residual CS0618 (4) zerado antes de encolher o gate:
  - **EFCore** (`ResilienceDbContextExtensions.ConfigureSqlServerWithResilience`): removido o uso de `MvpExecutionStrategy` (obsoleto). Retry passa a usar só o built-in `EnableRetryOnFailure` / `SqlServerRetryingExecutionStrategy` quando `EnableRetryOnFailure` é true (alinhado à doc de `EFCoreResilienceOptions`; Polly → `NativeDbResilienceExtensions`).
  - **MongoDb** (`MongoDbResiliencyExtensions.AddMongoDbResiliency`): `#pragma warning disable CS0618` justificado no shim de compatibilidade que ainda registra `MongoDbResiliencyPolicy` como `IMongoDbResiliencyPolicy` (consumidores legados; preferir `AddNativeMongoDbResilience`).
  - **Gate:** `MvpResidualWarnings` em [`src/Directory.Build.props`](../src/Directory.Build.props) reduzido a **somente `NU1510`** (pin de segurança `System.Security.Cryptography.Xml` 10.0.10 no Infrastructure — `NoWarn=NU1510` no `PackageReference`; ainda necessário para consumidores sem AspNetCore.App). Removidos todos os CS86xx + `CS0618`.
  - `dotnet build src/Mvp24Hours.sln -c Release --no-incremental /p:TreatWarningsAsErrors=true` → **0 erro(s) / 0 aviso(s)**. Baseline [`warnings-baseline-v2.json`](./warnings-baseline-v2.json): **total = 0**. Gate: 14 → **1 código** (`NU1510`).
- **Testes:** `MongoDb.Test` (`Category=Unit`) **129 aprovados, 0 falhas**; `SQLServer.Test` (`Category!=Integration`) **232 aprovados, 4 ignorados, 0 falhas**.
[x] 7.2 - Elevar regras de estilo do `.editorconfig` e rodar `dotnet format` completo
- Concluir o que a tarefa 10.2 (v1) adiou: elevar as regras de estilo/analisador de `suggestion` para `warning`/`error` no `.editorconfig` e rodar `dotnet format src/Mvp24Hours.sln --verify-no-changes` **completo** (sem `--severity error`), agora que os fixers de nullable/obsoleto não têm mais o que aplicar. Ajustar o CI para o escopo completo.
- `.editorconfig`, `.github/workflows/ci.yml`
- https://learn.microsoft.com/dotnet/core/tools/dotnet-format
- **Concluído 2026-07-18:**
  - **`.editorconfig`:** preferências `dotnet_style_*` / `csharp_style_*` e diagnósticos IDE* elevados de `suggestion` → `warning` (EnforceCodeStyleInBuild). Exceções documentadas: **IDE0005** permanece suggestion (exige `GenerateDocumentationFile`, que dispara CS1570 em XML samples); **IDE0078** suggestion (fixer quebra precedência de `&` bitwise); **IDE0130**/`namespace_match_folder` desabilitados (crash MSBuildWorkspace, [dotnet/format#2192](https://github.com/dotnet/format/issues/2192)); **IDE1006**/naming permanece suggestion (NamingStyleCodeFixProvider sem Fix All); CA* invasivos e **NU1510** inalterados.
  - **`dotnet format` completo:** 1567/1723 arquivos (file-scoped namespaces, primary constructors, collection expressions, usings, etc.). Correções manuais: expressão bitwise em `ValidatorExtensions`; CS9113 em `CircuitBreaker` (logger de compatibilidade); IDE0008 (`IServiceCollection`) nos Startups SQLServer/MySql/PostgreSql; `DbSet` de teste com `= null! // set by EF Core`.
  - **CI:** step `🎨 Check code formatting` passou a `dotnet format src/Mvp24Hours.sln --exclude-diagnostics IDE0130 IDE1006 --verify-no-changes` (sem `--severity error`). Comentário do gate de análise alinhado à 7.1.
  - `dotnet format ... --verify-no-changes` → **exit 0** (0/1723). `dotnet build ... /p:TreatWarningsAsErrors=true` → **0 erro(s) / 0 aviso(s)**.

[x] 7.3 - Suíte completa de testes + relatório final
- Reexecutar a suíte (unit + integração com Docker) confirmando **0 falhas** e nenhuma regressão introduzida pelas mudanças de nulidade/API. Consolidar TRX + cobertura como na tarefa 9.5 (v1).
- `src/Mvp24Hours.sln`
- https://learn.microsoft.com/dotnet/core/testing/unit-testing-code-coverage
- **Concluído 2026-07-18:** `dotnet test src/Mvp24Hours.sln -c Release --logger trx --collect:"XPlat Code Coverage"` com Docker Desktop **4.79.0** / Engine **29.5.3**. **16** projetos · **2294** aprovados · **0** falhas · **4** ignorados (BulkOperations InMemory) · total **2298** · exit **0**. Evidência: [`test-final-report-net10-warnings.md`](./test-final-report-net10-warnings.md) (+ [`.json`](./test-final-report-net10-warnings.json)); TRX/cobertura em `tasks/test-results-7.3/` (gitignored).
  - **Fix (Release/CI):** `InMemory` só existia em `Debug|AnyCPU` nos csproj MySql/PostgreSql/SQLServer.Test — Release tentava MySQL/PG/SQL reais. `DefineConstants` passou a incluir `InMemory` em qualquer configuração.
  - **Fix (CronJob):** `GetRequiredService<ExecutionTracker>` → `GetService` em `TestResilientCronJob`/`FailingCronJob`/`SlowCronJob` (campo já anulável; 2 testes não registravam o tracker → **91/91**).
  - Cobertura: 28× `coverage.cobertura.xml`; agregado ~6046/91933 linhas (~6,6% — dumps por assembly com sobreposição; evidência de coleta, não KPI único).

[x] 7.4 - Atualizar `CHANGELOG.md` e encerrar a dívida da v1
- Registrar no `CHANGELOG.md` a zeragem dos ~948 avisos residuais e a reativação do gate estrito, fechando as pendências "v2" apontadas nas tarefas 10.1/10.2/10.3 da v1.
- `CHANGELOG.md`, [`tasks-net10-v1.md`](./tasks-net10-v1.md)
- https://keepachangelog.com/pt-BR/1.1.0/
- **Concluído 2026-07-18:**
  - **[`CHANGELOG.md`](../CHANGELOG.md)** entrada `[10.0.0]`: **Corrigido** atualizado para **~4235 → 0** avisos (residual ~948 eliminado); estilo/format completo; **Testes** com suíte 7.3 (2294/0/4); **CI/CD** com gate estrito (`MvpResidualWarnings` = só `NU1510`) e `dotnet format` sem `--severity error`.
  - **[`tasks-net10-v1.md`](./tasks-net10-v1.md):** cabeçalho com status final; notas **Fechado/Atualizado na v2** em 10.1 (zero warnings), 10.2 (format completo), 10.3 (gate estrito) e 10.4 (changelog).
- **Com a 7.4, a Fase 7 e o plano v2 (`tasks-net10-warnings.md`) estão concluídos.**

---

## Resumo do sequenciamento (ROI)

1. **Fase 4** — quick wins não-nullable: remove **6 códigos** do gate cedo, diffs pequenos.
2. **Fase 5** — nullable de produção, do maior ofensor (MongoDb) ao menor; melhora as assinaturas que os testes consomem.
3. **Fase 6** — nullable de testes (muitos já reduzidos pela Fase 5).
4. **Fase 7** — gate estrito, formatação completa, testes e changelog.

**Métrica de progresso:** nº de códigos em `MvpResidualWarnings` (19 → 0) e total de avisos do build `Release` (~948 → 0). Recontar com o fluxo §2 ao fim de cada fase.
