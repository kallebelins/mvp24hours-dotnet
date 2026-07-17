# Tasks — Zeragem dos warnings residuais pós-.NET 10 (v2)

> Gerado em 17/07/2026. Continuação de [`tasks-net10-v1.md`](./tasks-net10-v1.md) (Fases 1–10 concluídas).
> **Ponto de partida:** a solução compila com **0 erro(s)**. A Fase 10 (v1) aceitou um **residual de ~948 avisos** e neutralizou o gate `TreatWarningsAsErrors` via `MvpResidualWarnings` em [`src/Directory.Build.props`](../src/Directory.Build.props). **Este plano zera esse residual e reativa o gate estrito.**
>
> **Objetivo:** `dotnet build src/Mvp24Hours.sln -c Release --no-incremental /p:TreatWarningsAsErrors=true` → **0 erro(s) / 0 aviso(s)**, com `MvpResidualWarnings` **vazio** (exceto `NU1510` intencional, ver §5.3).

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

[ ] 4.0 - Reconciliar o baseline residual (dedup) e versionar
- Rodar o build `Release` da solução (§2, mas na `.sln`) e extrair a contagem **deduplicada** do resumo MSBuild por código e por projeto, para servir de métrica objetiva de progresso. Versionar em `tasks/warnings-baseline-v2.md` (+ `.json`). Reexecutar ao fim de cada fase.
- `src/Mvp24Hours.sln`
- https://learn.microsoft.com/dotnet/core/tools/dotnet-build

[ ] 4.1 - LOGGEN002 — atribuir EventIds únicos nos `[LoggerMessage]` (Pipe, RabbitMQ, WebAPI, EFCore)
- **Padrão:** §1.C. Localizar os tipos `*LoggerMessages`/`*Log` com `[LoggerMessage(EventId = ...)]` duplicados e reatribuir IDs únicos e sequenciais por tipo, com faixas por módulo. Ex. inicial: `Mvp24Hours.Infrastructure.Pipe/Logging/PipelineLoggerMessages.cs` (event id `2003` repetido).
- Ao final, se LOGGEN002 = 0 na solução ⇒ remover de `MvpResidualWarnings`.
- `src/Mvp24Hours.Infrastructure.Pipe/**`, `src/Mvp24Hours.Infrastructure.RabbitMQ/**`, `src/Mvp24Hours.WebAPI/**`, `src/Mvp24Hours.Infrastructure.Data.EFCore/**`
- https://learn.microsoft.com/dotnet/core/extensions/logger-message-generator
- https://aka.ms/dotnet-extensions-warnings/LOGGEN002

[ ] 4.2 - CS0618 (EFCore) — `System.Data.SqlClient` → `Microsoft.Data.SqlClient` (residual da 5.2)
- **Padrão:** §1.B. Concluir em `Infrastructure.Data.EFCore` a migração iniciada na tarefa 5.2 da v1 (que deixou o EFCore fora de escopo). Trocar `using`/tipos `SqlConnection`/`SqlCommand`/`SqlParameter` e o `PackageReference` (CPM) de `System.Data.SqlClient` para `Microsoft.Data.SqlClient`. Ex.: `HealthChecks/SqlServerHealthCheck.cs` (linha ~189).
- Rodar os testes que cobrem EFCore SQL Server (grupo B/InMemory + integração quando houver Docker).
- `src/Mvp24Hours.Infrastructure.Data.EFCore/**`, `src/Directory.Packages.props`
- https://learn.microsoft.com/sql/connect/ado-net/introduction-microsoft-data-sqlclient-namespace

[ ] 4.3 - CS0618 (MongoDb.Test) — `MongoDbResiliencyPolicy` → `NativeMongoDbResilienceExtensions`
- **Padrão:** §1.B. Substituir os usos da API obsoleta `MongoDbResiliencyPolicy` nos testes (`Resiliency/MongoDbResiliencyPolicyTests.cs` e correlatos) pela recomendação citada na mensagem (`NativeMongoDbResilienceExtensions`). Se algum teste existe apenas para exercitar a API obsoleta, avaliar realocá-lo/removê-lo, documentando.
- Rodar `Mvp24Hours.Infrastructure.Data.MongoDb.Test`.
- `src/Tests/Mvp24Hours.Infrastructure.Data.MongoDb.Test/Resiliency/**`
- https://learn.microsoft.com/dotnet/csharp/language-reference/attributes/general#obsolete-attribute

[ ] 4.4 - SYSLIB0057 (MongoDb) — `X509Certificate2` → `X509CertificateLoader` (residual da 5.3)
- **Padrão:** §1.B. Concluir em `MongoDbAuthenticationOptions` (linha ~333) a substituição dos construtores de `X509Certificate2` por `X509CertificateLoader.LoadCertificate*/LoadPkcs12*`, deixada fora do escopo na tarefa 5.3 da v1.
- `src/Mvp24Hours.Infrastructure.Data.MongoDb/Security/MongoDbAuthenticationOptions.cs`
- https://aka.ms/dotnet-warnings/SYSLIB0057

[ ] 4.5 - ASPDEPR006 (WebAPI) — `IActionContextAccessor` obsoleto
- **Padrão:** §1.B. Em `Extensions/ServiceCollectionExtentions.cs` (linha ~65), substituir `IActionContextAccessor`/`AddActionContextAccessor` pela abordagem recomendada na mensagem (uso de `IHttpContextAccessor` / obtenção do `ActionContext` via serviços atuais), validando o comportamento dependente.
- Rodar `Mvp24Hours.WebAPI.Test`.
- `src/Mvp24Hours.WebAPI/Extensions/ServiceCollectionExtentions.cs`
- https://learn.microsoft.com/aspnet/core/mvc/advanced/app-parts

[ ] 4.6 - CS0108 (MongoDb) — remover ocultação de `RepositoryAsync._logger`
- **Padrão:** §1.D. `RepositoryAsync<T>._logger` oculta `RepositoryBase<T>._logger`. Remover o campo redundante e usar o da base (ou `new` explícito se houver diferença real de tipo/uso — investigar antes).
- `src/Mvp24Hours.Infrastructure.Data.MongoDb/Async/RepositoryAsync.cs` (linha ~29) e correlatos
- https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-messages/cs0108

[ ] 4.7 - xUnit1031 — eliminar o bloqueio síncrono residual
- **Padrão:** §1.E. Localizar as 2 ocorrências residuais (`.Result`/`.Wait()`/`GetAwaiter().GetResult()` em teste `async`) e converter para `await`.
- Projeto(s) de teste apontados pela recontagem 4.0
- https://xunit.net/xunit.analyzers/rules/xUnit1031

[ ] 4.8 - Encolher o gate: remover códigos zerados de `MvpResidualWarnings`
- Recompilar a solução e remover de `MvpResidualWarnings` os códigos que agora estão em **0 na solução**: `LOGGEN002`, `ASPDEPR006`, `SYSLIB0057`, `CS0108`, `xUnit1031` e `CS0618` (se 4.2 e 4.3 o zeraram por completo). Confirmar `dotnet build ... /p:TreatWarningsAsErrors=true` **verde**.
- `src/Directory.Build.props`
- https://learn.microsoft.com/visualstudio/msbuild/msbuild-warnings-as-errors

---

## FASE 5 — Nullable nos projetos de produção (por projeto)

> **Estratégia:** um projeto por tarefa/PR, do maior volume ao menor. Aplicar §1.A. Build isolado (§2). Não remover códigos CS86xx do gate ainda (são globais — sairão na §7). Ao terminar cada projeto, rodar os testes que o cobrem.
> **ADO:** US a criar.

[ ] 5.1 - Nullable em `Mvp24Hours.Infrastructure.Data.MongoDb` (~172 CS8618 + demais)
- **Padrão:** §1.A. Maior ofensor. Predominam CS8618 (POCOs/opções/documentos). Distinguir: opções de configuração ⇒ `required`/default; documentos/mapas preenchidos pelo driver Mongo ⇒ `= null!` com comentário `// set by MongoDB driver`. Tratar também CS8602/CS8604 do projeto.
- Rodar `Mvp24Hours.Application.MongoDb.Test` (+ integração quando houver Docker).
- `src/Mvp24Hours.Infrastructure.Data.MongoDb/**`
- https://learn.microsoft.com/dotnet/csharp/nullable-references

[ ] 5.2 - Nullable em `Mvp24Hours.Application` (~154)
- **Padrão:** §1.A. Projeto de produção não coberto na Fase 4 da v1 (CS8604/CS8602/CS8618 dispersos nos serviços de aplicação). Ajustar assinaturas para refletir a real anulabilidade de parâmetros/retornos dos serviços.
- Rodar `Mvp24Hours.Application.Test` + os testes por provedor (grupo B).
- `src/Mvp24Hours.Application/**`
- https://learn.microsoft.com/dotnet/csharp/nullable-references

[ ] 5.3 - Nullable em `Mvp24Hours.Infrastructure.Data.EFCore` (~14 CS8618 + demais, após 4.2)
- **Padrão:** §1.A. Restante do EFCore após a migração de SqlClient (4.2). POCOs/entidades de mapeamento ⇒ `= null!` com comentário `// set by EF Core` quando aplicável; opções ⇒ `required`/default.
- Rodar os testes de EFCore (grupo B + integração com Docker).
- `src/Mvp24Hours.Infrastructure.Data.EFCore/**`
- https://learn.microsoft.com/ef/core/miscellaneous/nullable-reference-types

[ ] 5.4 - Nullable em `Mvp24Hours.Infrastructure.RabbitMQ` (~18 CS8618 + demais, após 4.1)
- **Padrão:** §1.A. Restante do RabbitMQ após LOGGEN002 (4.1). Atenção a handlers/consumers (CS8622) e opções de conexão (CS8618).
- Rodar `Mvp24Hours.Application.RabbitMQ.Test` (Docker) quando disponível.
- `src/Mvp24Hours.Infrastructure.RabbitMQ/**`
- https://learn.microsoft.com/dotnet/csharp/nullable-references

[ ] 5.5 - Nullable em `Mvp24Hours.WebAPI` (~10 CS8618 + demais, após 4.1/4.5)
- **Padrão:** §1.A. Restante do WebAPI após LOGGEN002 (4.1) e ASPDEPR006 (4.5). Middlewares/filtros/opções.
- Rodar `Mvp24Hours.WebAPI.Test`.
- `src/Mvp24Hours.WebAPI/**`
- https://learn.microsoft.com/dotnet/csharp/nullable-references

[ ] 5.6 - Nullable em `Mvp24Hours.Infrastructure.Pipe` (após 4.1)
- **Padrão:** §1.A. Restante do Pipe após LOGGEN002 (4.1).
- Rodar `Mvp24Hours.Application.Pipe.Test` / `Mvp24Hours.Infrastructure.Pipe.Test`.
- `src/Mvp24Hours.Infrastructure.Pipe/**`
- https://learn.microsoft.com/dotnet/csharp/nullable-references

[ ] 5.7 - Nullable em `Mvp24Hours.Infrastructure.Cqrs` (~12 CS8618)
- **Padrão:** §1.A.
- Rodar `Mvp24Hours.Infrastructure.Cqrs.Test`.
- `src/Mvp24Hours.Infrastructure.Cqrs/**`
- https://learn.microsoft.com/dotnet/csharp/nullable-references

[ ] 5.8 - Nullable em `Mvp24Hours.Core` (residual ~2)
- **Padrão:** §1.A. Fechar o residual mínimo do Core.
- Rodar `Mvp24Hours.Core.Test`.
- `src/Mvp24Hours.Core/**`
- https://learn.microsoft.com/dotnet/csharp/nullable-references

---

## FASE 6 — Nullable nos projetos de teste (por projeto)

> **Estratégia:** mesmo fluxo (§2) e padrão (§1.A). Em testes é comum CS8618 em fixtures/DTOs e CS8604 em setups; preferir inicialização com default a `= null!`. Muitos avisos somem "de graça" após corrigir as assinaturas de produção (§5) — por isso os testes vêm **depois**.
> **ADO:** US a criar.

[ ] 6.1 - Nullable em `Mvp24Hours.Application.SQLServer.Test` (~44)
- **Padrão:** §1.A. `src/Tests/Mvp24Hours.Application.SQLServer.Test/**`

[ ] 6.2 - Nullable em `Mvp24Hours.Core.Test` (~38)
- **Padrão:** §1.A. `src/Tests/Mvp24Hours.Core.Test/**`

[ ] 6.3 - Nullable em `Mvp24Hours.Infrastructure.Data.MongoDb.Test` (parte nullable, após 4.3)
- **Padrão:** §1.A. `src/Tests/Mvp24Hours.Infrastructure.Data.MongoDb.Test/**`

[ ] 6.4 - Nullable em `Mvp24Hours.Application.MySql.Test` / `.PostgreSql.Test` / `.Redis.Test` (~28 cada)
- **Padrão:** §1.A. Fixtures e dados de teste compartilham o mesmo formato entre os três; aplicar a mesma correção. `src/Tests/Mvp24Hours.Application.MySql.Test/**`, `.PostgreSql.Test/**`, `.Redis.Test/**`

[ ] 6.5 - Nullable nos testes restantes (`Pipe.Test`, `Patterns.Test`, `MongoDb.Test`, `Cqrs.Test`, `RabbitMQ.Test`, `CronJob.Test`)
- **Padrão:** §1.A. Volumes pequenos (≤14 cada). `src/Tests/**`

---

## FASE 7 — Fechamento do gate estrito e validação final

> **ADO:** US a criar.

[ ] 7.1 - Zerar `MvpResidualWarnings` e reativar o gate estrito
- Após §5 e §6, recompilar a solução (`Release --no-incremental`) e remover **todos** os códigos CS86xx (e qualquer resíduo) de `MvpResidualWarnings`. Manter **somente** `NU1510` se o pin de segurança da tarefa 7.1 (v1) ainda exigir (reavaliar). Confirmar `dotnet build src/Mvp24Hours.sln -c Release --no-incremental /p:TreatWarningsAsErrors=true` → **0 erro(s) / 0 aviso(s)**.
- `src/Directory.Build.props`
- https://learn.microsoft.com/visualstudio/msbuild/msbuild-warnings-as-errors

[ ] 7.2 - Elevar regras de estilo do `.editorconfig` e rodar `dotnet format` completo
- Concluir o que a tarefa 10.2 (v1) adiou: elevar as regras de estilo/analisador de `suggestion` para `warning`/`error` no `.editorconfig` e rodar `dotnet format src/Mvp24Hours.sln --verify-no-changes` **completo** (sem `--severity error`), agora que os fixers de nullable/obsoleto não têm mais o que aplicar. Ajustar o CI para o escopo completo.
- `.editorconfig`, `.github/workflows/ci.yml`
- https://learn.microsoft.com/dotnet/core/tools/dotnet-format

[ ] 7.3 - Suíte completa de testes + relatório final
- Reexecutar a suíte (unit + integração com Docker) confirmando **0 falhas** e nenhuma regressão introduzida pelas mudanças de nulidade/API. Consolidar TRX + cobertura como na tarefa 9.5 (v1).
- `src/Mvp24Hours.sln`
- https://learn.microsoft.com/dotnet/core/testing/unit-testing-code-coverage

[ ] 7.4 - Atualizar `CHANGELOG.md` e encerrar a dívida da v1
- Registrar no `CHANGELOG.md` a zeragem dos ~948 avisos residuais e a reativação do gate estrito, fechando as pendências "v2" apontadas nas tarefas 10.1/10.2/10.3 da v1.
- `CHANGELOG.md`, [`tasks-net10-v1.md`](./tasks-net10-v1.md)
- https://keepachangelog.com/pt-BR/1.1.0/

---

## Resumo do sequenciamento (ROI)

1. **Fase 4** — quick wins não-nullable: remove **6 códigos** do gate cedo, diffs pequenos.
2. **Fase 5** — nullable de produção, do maior ofensor (MongoDb) ao menor; melhora as assinaturas que os testes consomem.
3. **Fase 6** — nullable de testes (muitos já reduzidos pela Fase 5).
4. **Fase 7** — gate estrito, formatação completa, testes e changelog.

**Métrica de progresso:** nº de códigos em `MvpResidualWarnings` (19 → 0) e total de avisos do build `Release` (~948 → 0). Recontar com o fluxo §2 ao fim de cada fase.
