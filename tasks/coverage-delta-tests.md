# Delta de Cobertura — Fase 1 (baseline) vs Fase 13 (final)

> Gerado em 19/07/2026 após execução consolidada com `coverlet.runsettings` + `src/Tests/Directory.Build.props` (correção de instrumentação Coverlet / .NET 10 SDK).

## Resumo executivo

| Métrica | Baseline (Fase 1) | Final (Fase 13) | Delta | Meta |
|---------|-------------------|-----------------|-------|------|
| **Cobertura de linha** | **28,3%** | **37,7%** | **+9,4 pp** | >95% |
| Linhas cobertas | 4.524 | 39.748 | +35.224 | — |
| Linhas cobráveis | 15.973 | 105.224 | +89.251 | — |
| Cobertura de branch | 25,8% | 29,2% | +3,4 pp | — |
| Cobertura de método | 47,8% | 42,7% | −5,1 pp | — |
| Assemblies com dados | 3 / 12 | **12 / 12** | +9 | 12 / 12 |
| Testes aprovados | 2.294 | **4.492** | **+2.198** | — |
| Testes ignorados | 4 | 6 | +2 | — |

**Conclusão:** a meta de **>95%** de cobertura de linha **não foi atingida** (37,7%). Houve avanço substancial (+9,4 pp no consolidado mensurável e instrumentação completa dos 12 projetos de produção). O gate de CI configurado usa **37%** como piso anti-regressão até nova rodada elevar a cobertura em direção à meta.

## Correção de instrumentação (Coverlet)

Antes da Fase 13, muitos projetos de teste geravam `coverage.cobertura.xml` vazio (`lines-valid="0"`) por `CecilAssemblyResolutionException` ao instrumentar assemblies copiados com dependências podadas pelo SDK .NET 10.

**Correção aplicada:**

- `src/Tests/Directory.Build.props` — `RestoreEnablePackagePruning=false`, `CopyLocalLockFileAssemblies=true`, `PreserveCompilationContext=true`
- `coverlet.runsettings` — filtro `Include [Mvp24Hours.*]*`, exclusão de assemblies de teste

## Delta por assembly de produção

| Assembly | Baseline | Final | Delta | Linhas cobertas (final) |
|----------|----------|-------|-------|-------------------------|
| Mvp24Hours.Infrastructure.CronJob | 29,0% | **71,2%** | +42,2 pp | 2.078 / 2.918 |
| Mvp24Hours.Infrastructure.Cqrs | 28,1% | **63,1%** | +35,0 pp | 3.940 / 6.235 |
| Mvp24Hours.Infrastructure | — | **57,5%** | novo | 8.777 / 15.252 |
| Mvp24Hours.Infrastructure.Caching | — | **45,9%** | novo | 2.100 / 4.569 |
| Mvp24Hours.Infrastructure.Caching.Redis | 40,9% | 40,9% | 0 | 9 / 22 |
| Mvp24Hours.Application | — | **38,1%** | novo | 3.489 / 9.147 |
| Mvp24Hours.Infrastructure.Data.EFCore | — | **37,7%** | novo | 3.508 / 9.298 |
| Mvp24Hours.Core | — | **34,7%** | novo | 3.885 / 11.178 |
| Mvp24Hours.Infrastructure.Pipe | — | **33,9%** | novo | 3.144 / 9.266 |
| Mvp24Hours.WebAPI | — | **29,5%** | novo | 3.219 / 10.904 |
| Mvp24Hours.Infrastructure.Data.MongoDb | — | **25,0%** | novo | 3.147 / 12.561 |
| Mvp24Hours.Infrastructure.RabbitMQ | — | **20,7%** | novo | 2.447 / 11.787 |

*Baseline “—” = sem dados Coverlet no run consolidado da Fase 1.*

## Maiores ganhos (Fases 2–12)

1. **CronJob** (+42 pp) — Fase 10: configuration, context, control, dependencies, events, scheduling.
2. **Cqrs** (+35 pp) — Fase 9: behaviors, projections, event sourcing, messaging.
3. **Infrastructure** (57,5%) — Fase 2: ~1.198 testes novos (email, SMS, storage, HTTP, jobs, locks, security, etc.).
4. **EF Core / Application / Core / WebAPI / Pipe / Caching / MongoDb / RabbitMQ** — projetos antes sem cobertura mensurável agora instrumentados.

## Gaps remanescentes (prioridade para >95%)

| Assembly | Cobertura | Observação |
|----------|-----------|------------|
| RabbitMQ | 20,7% | Maior superfície sem cobertura; integração MassTransit/Rabbit parcial |
| MongoDb | 25,0% | Advanced/Performance parcialmente cobertos via Testcontainers |
| WebAPI | 29,5% | Middlewares/filters cobertos; bootstrap/hosting ainda expostos |
| Pipe | 33,9% | Typed/advanced flows parcialmente cobertos |
| Core | 34,7% | Contratos/helpers cobertos; extensions ainda amplas |
| Caching.Redis | 40,9% | Apenas `RedisServiceExtensions`; segundo overload DI sem teste |

## Evidências versionadas

- Baseline: `tasks/coverage-baseline-tests.json`
- Final: `tasks/coverage-final-tests.json`
- Relatório HTML: `tasks/coverage-final-report.html`
- Run local: `test-results/coverage-report-final/` (gitignored)

## Comandos de reprodução

```bash
dotnet test src/Mvp24Hours.sln --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory ./test-results
reportgenerator -reports:"test-results/**/coverage.cobertura.xml" -targetdir:"./test-results/coverage-report-final" -reporttypes:"Html;JsonSummary;Cobertura" -assemblyfilters:"+Mvp24Hours*"
powershell -File ./scripts/check-coverage-gate.ps1 -SummaryJsonPath ./test-results/coverage-report-final/Summary.json
```
