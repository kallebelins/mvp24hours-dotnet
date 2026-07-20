# Controle ADO — Feature #87242 Migração Arquitetura .NET 10

> Gerado em 20/07/2026. Fonte: `tasks-net10-v1.md`, `tasks-net10-warnings.md`, `tasks-net10-tests.md`.
> **Feature:** [#87242](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87242)
> **Sprint alvo (novos itens):** `Bancorbrás-Agile\2026\Q3 (SP15d)\Sprint 74`
> **Responsável:** klsantos@bancorbras.com.br
> **Tags US:** `ARQS-Continua; ARQS-Evolucao-Continua`
> **State alvo US (trabalho feito):** `Verificação e Validação` · **Tasks:** `Closed` + horas + comentário

## Legenda de status ADO

| Status | Significado |
|--------|-------------|
| `exists-ok` | Existe no ADO, Closed/comentado |
| `exists-open` | Existe no ADO, precisa fechar/atualizar |
| `to-create` | Ainda não existe — criar (após confirmação) |
| `us-pending` | US existe mas ainda não em Verificação |

## Resumo executivo

| Plano | Fases | Checklist | US existentes | US a criar | Tasks open a fechar |
|-------|-------|-----------|---------------|------------|---------------------|
| v1 | 10 | 44 | 10 (+ US migração #87243) | 0 | 6 |
| warnings (v2) | 4 | 26 | 0 | 4 | 0 |
| tests | 25 | 201 | 0 | 25 | 0 |
| **Total** | **39** | **271** | **11** | **29** | **6** |

## Ações em itens já existentes (v1)

### User Stories a mover para Verificação e Validação + comentário de fechamento

| ID | Título | State atual | Ação |
|----|--------|-------------|------|
| 87243 | Migrar biblioteca Mvp24Hours .NET 9→10 | Em Execução | Verificação + comentário |
| 87253 | FASE 1 Diagnóstico | New | Verificação + comentário |
| 87269 | FASE 2 CI/CD | New | Verificação + comentário |
| 87270 | FASE 3 Fundação | New | Verificação + comentário |
| 87271 | FASE 4 Nullable | New | Verificação + comentário |
| 87272 | FASE 5 APIs obsoletas | Verificação | Comentário reforço (já em VV) |
| 87273 | FASE 6 Qualidade | New | Verificação + comentário |
| 87274 | FASE 7 NuGet | New | Verificação + comentário |
| 87275 | FASE 8 Higiene | New | Verificação + comentário |
| 87276 | FASE 9 Testes | New | Verificação + comentário |
| 87277 | FASE 10 Gate | New | Verificação + comentário |

### Tasks a fechar / completar horas

| ID | Parent US | Título | State | Ação |
|----|-----------|--------|-------|------|
| 87298 | 87271 | CS8765/CS8767/CS8609 | New | Closed + 2h + comentário |
| 87305 | 87274 | NU1903 Cryptography.Xml | New | Closed + 2h + comentário |
| 87312 | 87277 | TreatWarningsAsErrors | New | Closed + 1h + comentário (fechado na v2) |
| 87315 | 87276 | Suíte unitários | New | Closed + 2h + comentário |
| 87317 | 87276 | Trait Category | New | Closed + 3h + comentário |
| 87318 | 87276 | Relatório TRX | New | Closed + 2h + comentário |
| 87287 | 87272 | Microsoft.Data.SqlClient | Closed | CompletedWork=1.5 (faltava) |
| 87290 | 87272 | Pbkdf2 | Closed | CompletedWork=1 (faltava) |
| 87294 | 87271 | Nullable produção | Closed | CompletedWork=4 (faltava) |
| 87297 | 87271 | CS860x restante | Closed | CompletedWork=8 (faltava) |

## Matriz US × Tasks (todas as fases)

| Plano | Fase | Título fase | #Tasks | US ADO | SP prop. | Status |
|-------|------|-------------|--------|--------|----------|--------|
| v1 | 1 | Diagnóstico e Baseline | 3 | [#87253](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87253) | — | exists → Verificação |
| v1 | 2 | Desbloqueio do CI/CD (prioridade máxima) | 3 | [#87269](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87269) | — | exists → Verificação |
| v1 | 3 | Fundação: padronização da linguagem C# e do build | 5 | [#87270](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87270) | — | exists → Verificação |
| v1 | 4 | Habilitar e corrigir Nullable Reference Types (maior volume de warnings) | 5 | [#87271](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87271) | — | exists → Verificação |
| v1 | 5 | Modernização de APIs obsoletas (CS0618 / SYSLIB0xxx) | 8 | [#87272](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87272) | — | exists → Verificação |
| v1 | 6 | Qualidade de código: warnings diversos | 6 | [#87273](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87273) | — | exists → Verificação |
| v1 | 7 | Segurança e higiene de dependências NuGet | 3 | [#87274](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87274) | — | exists → Verificação |
| v1 | 8 | Limpeza de artefatos e higiene do repositório | 2 | [#87275](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87275) | — | exists → Verificação |
| v1 | 9 | Execução completa da suíte de testes | 5 | [#87276](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87276) | — | exists → Verificação |
| v1 | 10 | Validação final e fechamento do gate de qualidade | 4 | [#87277](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87277) | — | exists → Verificação |
| warnings | 4 | Quick wins não-nullable (encolher o gate primeiro) | 9 | _pendente_ | 5 | to-create |
| warnings | 5 | Nullable nos projetos de produção (por projeto) | 8 | _pendente_ | 5 | to-create |
| warnings | 6 | Nullable nos projetos de teste (por projeto) | 5 | _pendente_ | 3 | to-create |
| warnings | 7 | Fechamento do gate estrito e validação final | 4 | _pendente_ | 2 | to-create |
| tests | 1 | Diagnóstico e Baseline de Cobertura | 4 | _pendente_ | 2 | to-create |
| tests | 2 | Criar Projeto de Testes para `Mvp24Hours.Infrastructure` | 15 | _pendente_ | 8 | to-create |
| tests | 3 | Criar Projeto de Testes para `Mvp24Hours.Infrastructure.Data.EFCore` | 15 | _pendente_ | 8 | to-create |
| tests | 4 | Expandir Testes de `Mvp24Hours.WebAPI` | 14 | _pendente_ | 8 | to-create |
| tests | 5 | Expandir Testes de `Mvp24Hours.Infrastructure.RabbitMQ` | 15 | _pendente_ | 8 | to-create |
| tests | 6 | Expandir Testes de `Mvp24Hours.Application` | 7 | _pendente_ | 5 | to-create |
| tests | 7 | Expandir Testes de `Mvp24Hours.Infrastructure.Pipe` | 11 | _pendente_ | 8 | to-create |
| tests | 8 | Expandir Testes de `Mvp24Hours.Infrastructure.Caching` | 12 | _pendente_ | 8 | to-create |
| tests | 9 | Expandir Testes de `Mvp24Hours.Infrastructure.Cqrs` | 4 | _pendente_ | 2 | to-create |
| tests | 10 | Expandir Testes de `Mvp24Hours.Infrastructure.CronJob` | 3 | _pendente_ | 2 | to-create |
| tests | 11 | Expandir Testes de `Mvp24Hours.Infrastructure.Data.MongoDb` | 5 | _pendente_ | 3 | to-create |
| tests | 12 | Expandir Testes de `Mvp24Hours.Core` | 6 | _pendente_ | 3 | to-create |
| tests | 13 | Validação Final e Gate de Cobertura | 4 | _pendente_ | 2 | to-create |
| tests | 14 | Expandir Cobertura de `Mvp24Hours.Infrastructure.RabbitMQ` (20.7% → 90%) | 11 | _pendente_ | 8 | to-create |
| tests | 15 | Expandir Cobertura de `Mvp24Hours.Infrastructure.Data.MongoDb` (25% → 90%) | 11 | _pendente_ | 8 | to-create |
| tests | 16 | Expandir Cobertura de `Mvp24Hours.WebAPI` (29.5% → 90%) | 11 | _pendente_ | 8 | to-create |
| tests | 17 | Expandir Cobertura de `Mvp24Hours.Infrastructure.Pipe` (33.9% → 90%) ✅ | 7 | _pendente_ | 5 | to-create |
| tests | 18 | Expandir Cobertura de `Mvp24Hours.Core` (34.7% → 90%) | 10 | _pendente_ | 5 | to-create |
| tests | 19 | Expandir Cobertura de `Mvp24Hours.Infrastructure.Data.EFCore` (37.7% → 90%) | 8 | _pendente_ | 5 | to-create |
| tests | 20 | Expandir Cobertura de `Mvp24Hours.Application` (38.1% → 90%) | 8 | _pendente_ | 5 | to-create |
| tests | 21 | Expandir Cobertura de `Mvp24Hours.Infrastructure.Caching.Redis` (40.9% → 90%) | 1 | _pendente_ | 1 | to-create |
| tests | 22 | Expandir Cobertura de `Mvp24Hours.Infrastructure.Caching` (45.9% → 90%) | 4 | _pendente_ | 2 | to-create |
| tests | 23 | Expandir Cobertura de `Mvp24Hours.Infrastructure` (57.5% → 90%) | 7 | _pendente_ | 5 | to-create |
| tests | 24 | Expandir Cobertura de `Mvp24Hours.Infrastructure.Cqrs` (63.1% → 90%) | 4 | _pendente_ | 2 | to-create |
| tests | 25 | Expandir Cobertura de `Mvp24Hours.Infrastructure.CronJob` (71.2% → 90%) | 4 | _pendente_ | 2 | to-create |

## Detalhamento checklist (controle linha a linha)

| Plano | Código | Título | Md | US | Task ADO | Horas | Status ADO |
|-------|--------|--------|----|----|----------|-------|------------|
| v1 | 1.1 | Gerar e versionar o baseline de build/warnings da solução | done | #87253 | #87254 | 1.5 | exists-ok |
| v1 | 1.2 | Inventariar divergências de `TargetFramework` e `LangVersion` entre projetos | done | #87253 | #87255 | 1.5 | exists-ok |
| v1 | 1.3 | Mapear todos os projetos sem `<Nullable>enable</Nullable>` | done | #87253 | #87256 | 1.5 | exists-ok |
| v1 | 2.1 | Atualizar `DOTNET_VERSION` do workflow de CI para .NET 10 | done | #87269 | #87278 | 1.5 | exists-ok |
| v1 | 2.2 | Atualizar `dotnet-version` do workflow `codeql-analysis.yml` para .NET 10 | done | #87269 | #87279 | 1.5 | exists-ok |
| v1 | 2.3 | Validar (sem alterar ainda) o gate `TreatWarningsAsErrors` do job `code-quality` | done | #87269 | #87280 | 1.5 | exists-ok |
| v1 | 3.1 | Criar `Directory.Build.props` na raiz de `src/` centralizando propriedades comun | done | #87270 | #87281 | 1.5 | exists-ok |
| v1 | 3.2 | Criar `.editorconfig` na raiz do repositório com convenções de estilo C# | done | #87270 | #87282 | 1.5 | exists-ok |
| v1 | 3.3 | Avaliar e, se aprovado, adotar Central Package Management (`Directory.Packages.p | done | #87270 | #87283 | 1.5 | exists-ok |
| v1 | 3.4 | Alinhar os 3 projetos de teste ainda em `net9.0` para `net10.0` | done | #87270 | #87284 | 1.5 | exists-ok |
| v1 | 3.5 | Remover os `.csproj` de backup duplicados deixados pela migração | done | #87270 | #87285 | 1.5 | exists-ok |
| v1 | 4.1 | Habilitar `<Nullable>enable</Nullable>` nos 8 projetos de produção listados na t | done | #87271 | #87294 | 1.5 | exists-ok (fix hours) |
| v1 | 4.2 | Habilitar `<Nullable>enable</Nullable>` nos 9 projetos de teste listados na tare | done | #87271 | #87295 | 1.5 | exists-ok |
| v1 | 4.3 | Corrigir avisos CS8618 (propriedades/campos não-anuláveis sem valor ao saltar do | done | #87271 | #87296 | 1.5 | exists-ok |
| v1 | 4.4 | Corrigir avisos CS8600/CS8602/CS8603/CS8604/CS8601/CS8619/CS8625 no restante da  | done | #87271 | #87297 | 1.5 | exists-ok (fix hours) |
| v1 | 4.5 | Corrigir CS8765/CS8767/CS8609 (nulidade divergente em overrides e implementações | done | #87271 | #87298 | 1.5 | exists-open |
| v1 | 5.1 | Migrar `CircuitBreaker<T>` (próprio, obsoleto) para `NativeResiliencePipeline` | done | #87272 | #87286 | 1.5 | exists-ok |
| v1 | 5.2 | Migrar `SqlServerDistributedLockProvider` de `System.Data.SqlClient` para `Micro | done | #87272 | #87287 | 1.5 | exists-ok (fix hours) |
| v1 | 5.3 | Substituir construtores obsoletos de `X509Certificate2` por `X509CertificateLoad | done | #87272 | #87288 | 1.5 | exists-ok |
| v1 | 5.4 | Substituir uso de `ServicePointManager` por `HttpClient` (SYSLIB0014) | done | #87272 | #87289 | 1.5 | exists-ok |
| v1 | 5.5 | Substituir construtor obsoleto de `Rfc2898DeriveBytes` pelo método estático `Pbk | done | #87272 | #87290 | 1.5 | exists-ok (fix hours) |
| v1 | 5.6 | Substituir `FallbackCredentialsFactory` (AWS SDK, obsoleto) por `DefaultAWSCrede | done | #87272 | #87291 | 1.5 | exists-ok |
| v1 | 5.7 | Migrar aliases obsoletos de CQRS (`DomainEventBase`, `IDomainEvent`, `IDomainEve | done | #87272 | #87292 | 1.5 | exists-ok |
| v1 | 5.8 | Atualizar construtores obsoletos do Testcontainers (`MsSqlBuilder()`, `MongoDbBu | done | #87272 | #87293 | 1.5 | exists-ok |
| v1 | 6.1 | Corrigir CS0168 (variável de exceção declarada e nunca usada) em `Infrastructure | done | #87273 | #87299 | 1.5 | exists-ok |
| v1 | 6.2 | Corrigir CS0219 (variável atribuída mas nunca usada) em `SagaStateMachineConsume | done | #87273 | #87300 | 1.5 | exists-ok |
| v1 | 6.3 | Corrigir CS1718 (comparação de uma variável com ela mesma) em `EnumerationTest` | done | #87273 | #87301 | 1.5 | exists-ok |
| v1 | 6.4 | Corrigir CS0108 (ocultação de membro herdado sem `new`) em `TestOrderWithSnapsho | done | #87273 | #87302 | 1.5 | exists-ok |
| v1 | 6.5 | Corrigir CA2022 (leitura potencialmente incompleta de `Stream.ReadAsync`) em `ET | done | #87273 | #87303 | 1.5 | exists-ok |
| v1 | 6.6 | Corrigir xUnit1031 (bloqueio síncrono dentro de teste assíncrono) | done | #87273 | #87304 | 1.5 | exists-ok |
| v1 | 7.1 | Investigar e mitigar a vulnerabilidade NU1903 em `System.Security.Cryptography.X | done | #87274 | #87305 | 1.5 | exists-open |
| v1 | 7.2 | Remover `PackageReference` redundantes apontados por NU1510 | done | #87274 | #87306 | 1.5 | exists-ok |
| v1 | 7.3 | Revisar os workflows `security-scan` (em `ci.yml`) e `dependency-review.yml` apó | done | #87274 | #87307 | 1.5 | exists-ok |
| v1 | 8.1 | Remover `build-webapi-errors.txt` do controle de versão e evitar recorrência | done | #87275 | #87308 | 1.5 | exists-ok |
| v1 | 8.2 | Confirmar que nenhum outro artefato de build/log ficou versionado por engano | done | #87275 | #87309 | 1.5 | exists-ok |
| v1 | 9.1 | Inventariar todos os projetos de teste e suas dependências de infraestrutura | done | #87276 | #87314 | 1.5 | exists-ok |
| v1 | 9.2 | Executar toda a suíte de testes que não depende de infraestrutura externa | done | #87276 | #87315 | 1.5 | exists-open |
| v1 | 9.3 | Provisionar Docker/Testcontainers e executar a suíte completa de testes de integ | done | #87276 | #87316 | 1.5 | exists-ok |
| v1 | 9.4 | Categorizar testes com `Trait`/`Category` para permitir execução seletiva (unitá | done | #87276 | #87317 | 1.5 | exists-open |
| v1 | 9.5 | Consolidar e publicar o relatório final de execução de testes (TRX + cobertura) | done | #87276 | #87318 | 1.5 | exists-open |
| v1 | 10.1 | Rebuild completo da solução visando zero warnings | done | #87277 | #87310 | 1.5 | exists-ok |
| v1 | 10.2 | Executar `dotnet format --verify-no-changes` em toda a solução | done | #87277 | #87311 | 1.5 | exists-ok |
| v1 | 10.3 | Validar de ponta a ponta o gate `TreatWarningsAsErrors=true` do pipeline `code-q | done | #87277 | #87312 | 1.5 | exists-open |
| v1 | 10.4 | Atualizar `CHANGELOG.md` com o resumo da modernização para .NET 10 | done | #87277 | #87313 | 1.5 | exists-ok |
| warnings | 4.0 | Reconciliar o baseline residual (dedup) e versionar | done | NEW | NEW | 2 | to-create |
| warnings | 4.1 | LOGGEN002 — atribuir EventIds únicos nos `[LoggerMessage]` (Pipe, RabbitMQ, WebA | done | NEW | NEW | 1.5 | to-create |
| warnings | 4.2 | CS0618 (EFCore) — `System.Data.SqlClient` → `Microsoft.Data.SqlClient` (residual | done | NEW | NEW | 1.5 | to-create |
| warnings | 4.3 | CS0618 (MongoDb.Test) — `MongoDbResiliencyPolicy` → `NativeMongoDbResilienceExte | done | NEW | NEW | 1.5 | to-create |
| warnings | 4.4 | SYSLIB0057 (MongoDb) — `X509Certificate2` → `X509CertificateLoader` (residual da | done | NEW | NEW | 1.5 | to-create |
| warnings | 4.5 | ASPDEPR006 (WebAPI) — `IActionContextAccessor` obsoleto | done | NEW | NEW | 1.5 | to-create |
| warnings | 4.6 | CS0108 (MongoDb) — remover ocultação de `RepositoryAsync._logger` | done | NEW | NEW | 1.5 | to-create |
| warnings | 4.7 | xUnit1031 — eliminar o bloqueio síncrono residual | done | NEW | NEW | 1.5 | to-create |
| warnings | 4.8 | Encolher o gate: remover códigos zerados de `MvpResidualWarnings` | done | NEW | NEW | 2 | to-create |
| warnings | 5.1 | Nullable em `Mvp24Hours.Infrastructure.Data.MongoDb` (~172 CS8618 + demais) | done | NEW | NEW | 3 | to-create |
| warnings | 5.2 | Nullable em `Mvp24Hours.Application` (~154) | done | NEW | NEW | 3 | to-create |
| warnings | 5.3 | Nullable em `Mvp24Hours.Infrastructure.Data.EFCore` (~14 CS8618 + demais, após 4 | done | NEW | NEW | 3 | to-create |
| warnings | 5.4 | Nullable em `Mvp24Hours.Infrastructure.RabbitMQ` (~18 CS8618 + demais, após 4.1) | done | NEW | NEW | 3 | to-create |
| warnings | 5.5 | Nullable em `Mvp24Hours.WebAPI` (~10 CS8618 + demais, após 4.1/4.5) | done | NEW | NEW | 3 | to-create |
| warnings | 5.6 | Nullable em `Mvp24Hours.Infrastructure.Pipe` (após 4.1) | done | NEW | NEW | 3 | to-create |
| warnings | 5.7 | Nullable em `Mvp24Hours.Infrastructure.Cqrs` (~12 CS8618) | done | NEW | NEW | 3 | to-create |
| warnings | 5.8 | Nullable em `Mvp24Hours.Core` (residual ~2) | done | NEW | NEW | 3 | to-create |
| warnings | 6.1 | Nullable em `Mvp24Hours.Application.SQLServer.Test` (~44) | done | NEW | NEW | 3 | to-create |
| warnings | 6.2 | Nullable em `Mvp24Hours.Core.Test` (~38) | done | NEW | NEW | 3 | to-create |
| warnings | 6.3 | Nullable em `Mvp24Hours.Infrastructure.Data.MongoDb.Test` (parte nullable, após  | done | NEW | NEW | 3 | to-create |
| warnings | 6.4 | Nullable em `Mvp24Hours.Application.MySql.Test` / `.PostgreSql.Test` / `.Redis.T | done | NEW | NEW | 3 | to-create |
| warnings | 6.5 | Nullable nos testes restantes (`Pipe.Test`, `Patterns.Test`, `MongoDb.Test`, `Cq | done | NEW | NEW | 1.5 | to-create |
| warnings | 7.1 | Zerar `MvpResidualWarnings` e reativar o gate estrito | done | NEW | NEW | 2 | to-create |
| warnings | 7.2 | Elevar regras de estilo do `.editorconfig` e rodar `dotnet format` completo | done | NEW | NEW | 2 | to-create |
| warnings | 7.3 | Suíte completa de testes + relatório final | done | NEW | NEW | 2 | to-create |
| warnings | 7.4 | Atualizar `CHANGELOG.md` e encerrar a dívida da v1 | done | NEW | NEW | 2 | to-create |
| tests | 1.1 | Corrigir teste flaky `NotificationTest.PublishAsync_ShouldExecuteHandlersSequent | done | NEW | NEW | 2 | to-create |
| tests | 1.2 | Corrigir erro de carregamento do `Mvp24Hours.Application.MongoDb.Test` | done | NEW | NEW | 2 | to-create |
| tests | 1.3 | Gerar relatório de cobertura detalhado por projeto | done | NEW | NEW | 1.5 | to-create |
| tests | 1.4 | Inventariar todas as classes de produção sem cobertura | done | NEW | NEW | 1.5 | to-create |
| tests | 2.1 | Criar projeto `Mvp24Hours.Infrastructure.Test` | done | NEW | NEW | 2 | to-create |
| tests | 2.2 | Testes para `Email/Providers/*` | done | NEW | NEW | 2 | to-create |
| tests | 2.3 | Testes para `Email/Templates/*` | done | NEW | NEW | 2 | to-create |
| tests | 2.4 | Testes para `Sms/Providers/*` | done | NEW | NEW | 2 | to-create |
| tests | 2.5 | Testes para `FileStorage/Providers/*` | done | NEW | NEW | 2 | to-create |
| tests | 2.6 | Testes para `Http/Resilience/*` | done | NEW | NEW | 2 | to-create |
| tests | 2.7 | Testes para `Http/DelegatingHandlers/*` | done | NEW | NEW | 2 | to-create |
| tests | 2.8 | Testes para `BackgroundJobs/*` | done | NEW | NEW | 2 | to-create |
| tests | 2.9 | Testes para `DistributedLocking/*` | done | NEW | NEW | 2 | to-create |
| tests | 2.10 | Testes para `Security/*` | done | NEW | NEW | 2 | to-create |
| tests | 2.11 | Testes para `Resilience/*` | done | NEW | NEW | 2 | to-create |
| tests | 2.12 | Testes para `HealthChecks/*` | done | NEW | NEW | 2 | to-create |
| tests | 2.13 | Testes para `Helpers/*` | done | NEW | NEW | 2 | to-create |
| tests | 2.14 | Testes para `Observability/*` | done | NEW | NEW | 2 | to-create |
| tests | 2.15 | Testes para `Testing/*` | done | NEW | NEW | 2 | to-create |
| tests | 3.1 | Criar projeto `Mvp24Hours.Infrastructure.Data.EFCore.Test` | done | NEW | NEW | 2 | to-create |
| tests | 3.2 | Testes para `Repository.cs` e `RepositoryAsync.cs` | done | NEW | NEW | 2 | to-create |
| tests | 3.3 | Testes para `ReadOnlyRepository.cs` e `ReadOnlyRepositoryAsync.cs` | done | NEW | NEW | 2 | to-create |
| tests | 3.4 | Testes para `BulkOperationsRepositoryAsync.cs` | done | NEW | NEW | 2 | to-create |
| tests | 3.5 | Testes para `StreamingRepositoryAsync.cs` | done | NEW | NEW | 2 | to-create |
| tests | 3.6 | Testes para `UnitOfWorkWithEventsAsync.cs` | done | NEW | NEW | 2 | to-create |
| tests | 3.7 | Testes para `Interceptors/*` | done | NEW | NEW | 2 | to-create |
| tests | 3.8 | Testes para `Specifications/SpecificationEvaluator.cs` | done | NEW | NEW | 2 | to-create |
| tests | 3.9 | Testes para `Resilience/*` | done | NEW | NEW | 2 | to-create |
| tests | 3.10 | Testes para `Testing/*` | done | NEW | NEW | 2 | to-create |
| tests | 3.11 | Testes para `Migrations/*` | done | NEW | NEW | 2 | to-create |
| tests | 3.12 | Testes para `Converters/*` | done | NEW | NEW | 2 | to-create |
| tests | 3.13 | Testes para `Cqrs/*` | done | NEW | NEW | 2 | to-create |
| tests | 3.14 | Testes para `ReadWriteSplitting/*` | done | NEW | NEW | 2 | to-create |
| tests | 3.15 | Testes para `SchemaValidation/*` | done | NEW | NEW | 2 | to-create |
| tests | 4.1 | Testes para `Middlewares/*` | done | NEW | NEW | 2 | to-create |
| tests | 4.2 | Testes para `Filters/*` | done | NEW | NEW | 2 | to-create |
| tests | 4.3 | Testes para `Binders/*` | done | NEW | NEW | 2 | to-create |
| tests | 4.4 | Testes para `RateLimiting/*` | done | NEW | NEW | 2 | to-create |
| tests | 4.5 | Testes para `Idempotency/*` | done | NEW | NEW | 2 | to-create |
| tests | 4.6 | Testes para `ContentNegotiation/*` | done | NEW | NEW | 2 | to-create |
| tests | 4.7 | Testes para `Exceptions/*` | done | NEW | NEW | 2 | to-create |
| tests | 4.8 | Testes para `Endpoints/*` | done | NEW | NEW | 2 | to-create |
| tests | 4.9 | Testes para `Configuration/*` | done | NEW | NEW | 2 | to-create |
| tests | 4.10 | Testes para `OpenApi/*` | done | NEW | NEW | 2 | to-create |
| tests | 4.11 | Testes para `Http/*` | done | NEW | NEW | 2 | to-create |
| tests | 4.12 | Testes para `Services/*` | done | NEW | NEW | 2 | to-create |
| tests | 4.13 | Testes para `HealthChecks/*` | done | NEW | NEW | 2 | to-create |
| tests | 4.14 | Testes para `Extensions/*` | done | NEW | NEW | 2 | to-create |
| tests | 5.1 | Testes para `Core/Contract/*` | done | NEW | NEW | 2 | to-create |
| tests | 5.2 | Testes para `MvpRabbitMQClient.cs` | done | NEW | NEW | 2 | to-create |
| tests | 5.3 | Testes para `Consumers/*` | done | NEW | NEW | 2 | to-create |
| tests | 5.4 | Testes para `Transactional/*` | done | NEW | NEW | 2 | to-create |
| tests | 5.5 | Testes para `Saga/*` | done | NEW | NEW | 2 | to-create |
| tests | 5.6 | Testes para `Scheduling/*` | done | NEW | NEW | 2 | to-create |
| tests | 5.7 | Testes para `RequestResponse/*` | done | NEW | NEW | 2 | to-create |
| tests | 5.8 | Testes para `Pipeline/Filters/*` | done | NEW | NEW | 2 | to-create |
| tests | 5.9 | Testes para `MultiTenancy/*` | done | NEW | NEW | 2 | to-create |
| tests | 5.10 | Testes para `Topology/*` | done | NEW | NEW | 2 | to-create |
| tests | 5.11 | Testes para `Serialization/*` | done | NEW | NEW | 2 | to-create |
| tests | 5.12 | Testes para `Testing/*` | done | NEW | NEW | 2 | to-create |
| tests | 5.13 | Testes para `Observability/*` | done | NEW | NEW | 2 | to-create |
| tests | 5.14 | Testes para `Hosted/*` | done | NEW | NEW | 2 | to-create |
| tests | 5.15 | Testes para `Configuration/*` | done | NEW | NEW | 2 | to-create |
| tests | 6.1 | Testes para `Logic/Async/*` | done | NEW | NEW | 2 | to-create |
| tests | 6.2 | Testes para `Logic/Cache/*` | done | NEW | NEW | 2 | to-create |
| tests | 6.3 | Testes para `Logic/Events/*` | done | NEW | NEW | 2 | to-create |
| tests | 6.4 | Testes para `Logic/Validation/*` | done | NEW | NEW | 2 | to-create |
| tests | 6.5 | Testes para `Logic/Observability/*` | done | NEW | NEW | 2 | to-create |
| tests | 6.6 | Testes para `Logic/Resilience/*` | done | NEW | NEW | 2 | to-create |
| tests | 6.7 | Testes para `Specifications/*` | done | NEW | NEW | 2 | to-create |
| tests | 7.1 | Testes para `Typed/*` | done | NEW | NEW | 2 | to-create |
| tests | 7.2 | Testes para `AdvancedFlow/DependencyGraph/*` | done | NEW | NEW | 2 | to-create |
| tests | 7.3 | Testes para `AdvancedFlow/Saga/*` | done | NEW | NEW | 2 | to-create |
| tests | 7.4 | Testes para `AdvancedFlow/Checkpoint/*` | done | NEW | NEW | 2 | to-create |
| tests | 7.5 | Testes para `AdvancedFlow/Priority/*` | done | NEW | NEW | 2 | to-create |
| tests | 7.6 | Testes para `Resiliency/*` | done | NEW | NEW | 2 | to-create |
| tests | 7.7 | Testes para `Middleware/*` | done | NEW | NEW | 2 | to-create |
| tests | 7.8 | Testes para `Observability/*` | done | NEW | NEW | 2 | to-create |
| tests | 7.9 | Testes para `Builders/*` | done | NEW | NEW | 2 | to-create |
| tests | 7.10 | Testes para `ExceptionMapping/*` | done | NEW | NEW | 2 | to-create |
| tests | 7.11 | Testes para `Validation/*` | done | NEW | NEW | 2 | to-create |
| tests | 8.1 | Testes para `Providers/*` | done | NEW | NEW | 2 | to-create |
| tests | 8.2 | Testes para `Patterns/*` | done | NEW | NEW | 2 | to-create |
| tests | 8.3 | Testes para `Serializers/*` | done | NEW | NEW | 2 | to-create |
| tests | 8.4 | Testes para `Invalidation/*` | done | NEW | NEW | 2 | to-create |
| tests | 8.5 | Testes para `Warming/*` | done | NEW | NEW | 2 | to-create |
| tests | 8.6 | Testes para `Prefetching/*` | done | NEW | NEW | 2 | to-create |
| tests | 8.7 | Testes para `Resilience/*` | done | NEW | NEW | 2 | to-create |
| tests | 8.8 | Testes para `Compression/*` | done | NEW | NEW | 2 | to-create |
| tests | 8.9 | Testes para `Synchronization/*` | done | NEW | NEW | 2 | to-create |
| tests | 8.10 | Testes para `Repository/*` | done | NEW | NEW | 2 | to-create |
| tests | 8.11 | Testes para `EFCore/*` | done | NEW | NEW | 2 | to-create |
| tests | 8.12 | Testes para `Observability/*` | done | NEW | NEW | 2 | to-create |
| tests | 9.1 | Testes para classes com 0% de cobertura | done | NEW | NEW | 2 | to-create |
| tests | 9.2 | Testes para `Projections/*` | done | NEW | NEW | 2 | to-create |
| tests | 9.3 | Testes para `EventSourcing/*` | done | NEW | NEW | 2 | to-create |
| tests | 9.4 | Testes para `Messaging/*` | done | NEW | NEW | 2 | to-create |
| tests | 10.1 | Testes para classes com 0% de cobertura | done | NEW | NEW | 2 | to-create |
| tests | 10.2 | Testes para `Extensions/*` | done | NEW | NEW | 2 | to-create |
| tests | 10.3 | Testes para `Scheduling/*` | done | NEW | NEW | 2 | to-create |
| tests | 11.1 | Testes para `Advanced/*` | done | NEW | NEW | 2 | to-create |
| tests | 11.2 | Testes para `Performance/*` | done | NEW | NEW | 2 | to-create |
| tests | 11.3 | Testes para `Security/*` | done | NEW | NEW | 2 | to-create |
| tests | 11.4 | Testes para `Interceptors/*` | done | NEW | NEW | 2 | to-create |
| tests | 11.5 | Testes para `Observability/*` | done | NEW | NEW | 2 | to-create |
| tests | 12.1 | Revisar cobertura atual do Core | done | NEW | NEW | 2 | to-create |
| tests | 12.2 | Testes para `Contract/**/*` | done | NEW | NEW | 2 | to-create |
| tests | 12.3 | Testes para `Domain/**/*` | done | NEW | NEW | 2 | to-create |
| tests | 12.4 | Testes para `Infrastructure/**/*` | done | NEW | NEW | 2 | to-create |
| tests | 12.5 | Testes para `Serialization/**/*` | done | NEW | NEW | 2 | to-create |
| tests | 12.6 | Testes para `Exceptions/*` | done | NEW | NEW | 2 | to-create |
| tests | 13.1 | Gerar relatório de cobertura final | done | NEW | NEW | 1.5 | to-create |
| tests | 13.2 | Comparar com baseline | done | NEW | NEW | 1.5 | to-create |
| tests | 13.3 | Configurar gate de cobertura no CI | done | NEW | NEW | 1.5 | to-create |
| tests | 13.4 | Documentar no CHANGELOG | done | NEW | NEW | 1.5 | to-create |
| tests | 14.1 | Criar `FluentBuildersTest.cs` — RetryPolicyBuilder, CircuitBreakerPolicyBuilder, | done | NEW | NEW | 2 | to-create |
| tests | 14.2 | Criar `DeduplicationTest.cs` — InMemoryMessageDeduplicationStore | done | NEW | NEW | 2 | to-create |
| tests | 14.3 | Criar `MessagesTest.cs` — Message<T> | done | NEW | NEW | 2 | to-create |
| tests | 14.4 | Criar `ExceptionsTest.cs` — RequestTimeoutException | done | NEW | NEW | 2 | to-create |
| tests | 14.5 | Criar `ChannelBatchProcessorTest.cs` — ChannelBatchProcessorOptions, BatchConsum | done | NEW | NEW | 2 | to-create |
| tests | 14.6 | Expandir `PipelineFiltersTest.cs` — LoggingConsumeFilter, ValidationConsumeFilte | done | NEW | NEW | 2 | to-create |
| tests | 14.7 | Expandir `TopologyTest.cs` — AutoBindingOptions, ConsumerBindingInfo, MessageBin | done | NEW | NEW | 2 | to-create |
| tests | 14.8 | Expandir `SagaTest.cs` — InMemorySagaRepository CRUD, SagaConsumeContext, SagaIn | done | NEW | NEW | 2 | to-create |
| tests | 14.9 | Expandir `ObservabilityTest.cs` + `ConfigurationTest.cs` | done | NEW | NEW | 2 | to-create |
| tests | 14.10 | Expandir `SerializationTest`, `TestingInfrastructureTest`, `MultiTenancyTest`, ` | done | NEW | NEW | 2 | to-create |
| tests | 14.11 | Executar testes e atualizar tasks-net10-tests.md | done | NEW | NEW | 2 | to-create |
| tests | 15.1 | Testes para `Repository.cs` e `ReadOnlyRepository.cs` (sync) | done | NEW | NEW | 2 | to-create |
| tests | 15.2 | Testes para `Async/*.cs` (async completo) | done | NEW | NEW | 2 | to-create |
| tests | 15.3 | Testes para `UnitOfWork.cs` e `Transactions/*` | done | NEW | NEW | 2 | to-create |
| tests | 15.4 | Testes para `Advanced/GridFS/*` | done | NEW | NEW | 2 | to-create |
| tests | 15.5 | Testes para `Advanced/Geospatial/*` | done | NEW | NEW | 2 | to-create |
| tests | 15.6 | Testes para `Advanced/TextSearch/*` | done | NEW | NEW | 2 | to-create |
| tests | 15.7 | Testes para `Advanced/TimeSeries/*` | done | NEW | NEW | 2 | to-create |
| tests | 15.8 | Testes para `Advanced/CappedCollections/*` | done | NEW | NEW | 2 | to-create |
| tests | 15.9 | Testes para `Performance/Aggregation/*` | done | NEW | NEW | 2 | to-create |
| tests | 15.10 | Testes para `Infrastructure/Migrations/*` | done | NEW | NEW | 2 | to-create |
| tests | 15.11 | Testes para `Extensions/*` (DI completo) | done | NEW | NEW | 2 | to-create |
| tests | 16.1 | Testes para `Authentication/*` | done | NEW | NEW | 2 | to-create |
| tests | 16.2 | Testes para `Authorization/*` | done | NEW | NEW | 2 | to-create |
| tests | 16.3 | Testes para `Versioning/*` | done | NEW | NEW | 2 | to-create |
| tests | 16.4 | Testes para `Controllers/*` | done | NEW | NEW | 2 | to-create |
| tests | 16.5 | Testes para `ModelBinding/*` (completo) | done | NEW | NEW | 2 | to-create |
| tests | 16.6 | Testes para `Formatters/*` | done | NEW | NEW | 2 | to-create |
| tests | 16.7 | Testes para `Caching/*` | done | NEW | NEW | 2 | to-create |
| tests | 16.8 | Testes para `Compression/*` | done | NEW | NEW | 2 | to-create |
| tests | 16.9 | Testes para `Cors/*` | done | NEW | NEW | 2 | to-create |
| tests | 16.10 | Testes para `Localization/*` | done | NEW | NEW | 2 | to-create |
| tests | 16.11 | Testes para `Swagger/*` (avançado) | done | NEW | NEW | 2 | to-create |
| tests | 17.1 | Testes para `PipelineMessage` (base completa) | done | NEW | NEW | 2 | to-create |
| tests | 17.2 | Testes para `Context/PipelineContext` (completo) | done | NEW | NEW | 2 | to-create |
| tests | 17.3 | Testes para `Typed/OperationResult` e `Typed/OperationChain` | done | NEW | NEW | 2 | to-create |
| tests | 17.4 | Testes para `Configuration/PipelineOptions` e `Extensions/PipelineServiceExtensi | done | NEW | NEW | 2 | to-create |
| tests | 17.5 | Testes para `Operations/Custom/*` | done | NEW | NEW | 2 | to-create |
| tests | 17.6 | Testes para `Operations/Branch/ConditionalBranchOperation` e `Operations/Paralle | done | NEW | NEW | 2 | to-create |
| tests | 17.7 | Testes para `AdvancedFlow/ForkJoin/ForkJoinOperation` | done | NEW | NEW | 2 | to-create |
| tests | 18.1 | Testes para `Aspire/*` (completo) | done | NEW | NEW | 2 | to-create |
| tests | 18.2 | Testes para `Contract/Data/*` | done | NEW | NEW | 2 | to-create |
| tests | 18.3 | Testes para `Contract/Infrastructure/Caching/*` | done | NEW | NEW | 2 | to-create |
| tests | 18.4 | Testes para `Contract/Infrastructure/Pipe/*` | done | NEW | NEW | 2 | to-create |
| tests | 18.5 | Testes para `Contract/Infrastructure/DependencyInjection/*` | done | NEW | NEW | 2 | to-create |
| tests | 18.6 | Testes para `Converters/*` (Newtonsoft) | done | NEW | NEW | 2 | to-create |
| tests | 18.7 | Testes para `Domain/Validation/*` | done | NEW | NEW | 2 | to-create |
| tests | 18.8 | Testes para `Extensions/*` (helpers) | done | NEW | NEW | 2 | to-create |
| tests | 18.9 | Testes para `Helpers/*` (utilitários avançados) | done | NEW | NEW | 2 | to-create |
| tests | 18.10 | Testes para `ValueObjects/*` (completo) | done | NEW | NEW | 2 | to-create |
| tests | 19.1 | Testes para `Mvp24HoursContext` (DbContext base) | done | NEW | NEW | 2 | to-create |
| tests | 19.2 | Testes para UnitOfWork / RepositoryBase | done | NEW | NEW | 2 | to-create |
| tests | 19.3 | Testes para Query Extensions (em vez de QueryObjects) | done | NEW | NEW | 2 | to-create |
| tests | 19.4 | Testes para ModelBuilder Extensions (em vez de Conventions) | done | NEW | NEW | 2 | to-create |
| tests | 19.5 | Testes para Security + Configuration (em vez de ShadowProperties) | done | NEW | NEW | 2 | to-create |
| tests | 19.6 | Testes para Observability + Logging (em vez de ChangeTracking) | done | NEW | NEW | 2 | to-create |
| tests | 19.7 | Testes para HealthChecks (em vez de Transactions) | done | NEW | NEW | 2 | to-create |
| tests | 19.8 | Testes para `Extensions/*` (DI completo) | done | NEW | NEW | 2 | to-create |
| tests | 20.1 | Testes para `Logic/ApplicationServiceBase*` (sync completo) | done | NEW | NEW | 2 | to-create |
| tests | 20.2 | Testes para `Logic/Async/*` (services base) | done | NEW | NEW | 2 | to-create |
| tests | 20.3 | Testes para `Logic/CommandServiceBase*` e `QueryServiceBase*` (sync) | done | NEW | NEW | 2 | to-create |
| tests | 20.4 | Testes para `Logic/Pagination/*` | done | NEW | NEW | 2 | to-create |
| tests | 20.5 | Testes para `Logic/Transaction/*` | done | NEW | NEW | 2 | to-create |
| tests | 20.6 | Testes para `Logic/Validation/*` (steps avançados) | done | NEW | NEW | 2 | to-create |
| tests | 20.7 | Testes para `Extensions/*` (DI completo) | done | NEW | NEW | 2 | to-create |
| tests | 20.8 | Testes para `Contract/*` (tipos concretos) | done | NEW | NEW | 2 | to-create |
| tests | 21.1 | Testes para `RedisServiceExtensions.cs` (completo) | done | NEW | NEW | 2 | to-create |
| tests | 22.1 | Testes para `Distributed/*` | done | NEW | NEW | 2 | to-create |
| tests | 22.2 | Testes para `Tags/*` | done | NEW | NEW | 2 | to-create |
| tests | 22.3 | Testes para `Locking/*` | done | NEW | NEW | 2 | to-create |
| tests | 22.4 | Testes para `Extensions/*` (DI completo) | done | NEW | NEW | 2 | to-create |
| tests | 23.1 | Testes para `Email/Services/*` | done | NEW | NEW | 2 | to-create |
| tests | 23.2 | Testes para `Sms/Services/*` | done | NEW | NEW | 2 | to-create |
| tests | 23.3 | Testes para `FileStorage/Services/*` | done | NEW | NEW | 2 | to-create |
| tests | 23.4 | Testes para `BackgroundJobs/Services/*` | done | NEW | NEW | 2 | to-create |
| tests | 23.5 | Testes para `DistributedLocking/Services/*` | done | NEW | NEW | 2 | to-create |
| tests | 23.6 | Testes para `Security/Services/*` | done | NEW | NEW | 2 | to-create |
| tests | 23.7 | Testes para `Extensions/*` (DI completo) | done | NEW | NEW | 2 | to-create |
| tests | 24.1 | Testes para `Handlers/*` (completo) | done | NEW | NEW | 2 | to-create |
| tests | 24.2 | Testes para `Dispatchers/*` | done | NEW | NEW | 2 | to-create |
| tests | 24.3 | Testes para `Validators/*` | done | NEW | NEW | 2 | to-create |
| tests | 24.4 | Testes para `Extensions/*` (DI completo) | done | NEW | NEW | 2 | to-create |
| tests | 25.1 | Testes para `Services/*` (avançado) | done | NEW | NEW | 2 | to-create |
| tests | 25.2 | Testes para `Resiliency/*` | done | NEW | NEW | 2 | to-create |
| tests | 25.3 | Testes para `State/*` | done | NEW | NEW | 2 | to-create |
| tests | 25.4 | Testes para `Extensions/*` + validators (DI completo) | done | NEW | NEW | 2 | to-create |

## Feature #87242 — descrição a atualizar

Incluir na Description as três frentes:
1. **Estabilização v1** (Fases 1–10) — `tasks-net10-v1.md` — US 87253–87277
2. **Zeragem warnings v2** (Fases 4–7) — `tasks-net10-warnings.md` — US a criar
3. **Cobertura de testes >95%** (Fases 1–25) — `tasks-net10-tests.md` — US a criar

## Checklist de execução (após confirmação)

- [ ] Fechar 6 tasks open + completar horas em 4 Closed
- [ ] Mover 10 US v1 (+87243) para Verificação e Validação com comentário
- [ ] Criar 4 US warnings + 26 tasks Closed
- [ ] Criar 25 US tests + 201 tasks Closed
- [ ] Vincular todas as US novas à Feature #87242
- [ ] Atualizar Description da Feature #87242
- [ ] Atualizar links ADO nos 3 arquivos markdown
- [ ] Atualizar este controle com IDs reais

## Observações

- Trabalho **retroativo** (Cenário K): state das US = Verificação e Validação (não Closed sem validação interna).
- Horas propostas são estimativa retroativa coerente com esforço típico; ajustar se necessário antes da gravação.
- US #87243 (migração TFM) permanece sob a Feature; estabilização e cobertura são US irmãs.
