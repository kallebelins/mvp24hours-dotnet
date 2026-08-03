# Coverage baseline (versioned)

Consolidated line coverage for the main solution (`src/Mvp24Hours.slnx`), measured with merged unit + integration Cobertura reports and `reportgenerator` assembly filter `+Mvp24Hours*;-Mvp24Hours*.Test*`.

Regenerate: `./scripts/run-ci-local.ps1 -SkipSamples`

## Current snapshot — Phase 2 complete (2026-08-03)

| Metric | Value |
|--------|-------|
| Line coverage | **65.9%** (49,170 / 74,608 coverable lines) |
| Branch coverage | **49.2%** |
| CI regression floor | **65%** |
| Product target | **95%** |
| Test projects | **19** in `src/Mvp24Hours.slnx` |

### Assembly breakdown

| Assembly | Line % | Covered / coverable |
|----------|--------|---------------------|
| Infrastructure.RabbitMQ | 49.4 | 4,190 / 8,478 |
| Infrastructure.Data.MongoDb | 53.3 | 4,763 / 8,922 |
| Infrastructure.Pipe | 59.8 | 3,872 / 6,472 |
| Core | 66.1 | 4,914 / 7,430 |
| Infrastructure.Data.EFCore | 66.3 | 4,326 / 6,517 |
| Infrastructure.Identity.Keycloak | 66.3 | 948 / 1,428 |
| Application | 69.8 | 4,545 / 6,509 |
| Infrastructure.Caching | 70.5 | 2,314 / 3,280 |
| WebAPI | 72.0 | 5,763 / 8,004 |
| Infrastructure.Cqrs | 72.2 | 3,251 / 4,500 |
| Infrastructure | 78.1 | 8,561 / 10,952 |
| Infrastructure.CronJob | 81.2 | 1,707 / 2,100 |
| Infrastructure.Caching.Redis | 100.0 | 16 / 16 |

### Roadmap phase status

| Phase | Gate | Status |
|-------|------|--------|
| 0 — CI split, filter fix | 45% | Done (44.5% → split unit/integration) |
| 1 — Unit quick wins | 55% | Done |
| 2 — Docker integration | 65% | **Done** (59.4% → 65.9%) |
| 3 — WebAPI + Pipe | 75% | Tests added; gate pending |
| 4 — EFCore + App + Infra | 85% | Tests added; gate pending |
| 5 — Final sweep | 95% | Product target; ~29.1 pp remaining |

### CI architecture

- **Unit job**: `Category!=Integration` (ubuntu + matrix OS for tests)
- **Integration job**: `Category=Integration` (ubuntu + Docker)
- **Coverage gate**: merged Cobertura from both jobs; excludes test assemblies

See [Testing home](home.md) for local commands.
