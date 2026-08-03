# Coverage baseline (versioned)

Consolidated line coverage for the main solution (`src/Mvp24Hours.slnx`), measured with merged unit + integration Cobertura reports and `reportgenerator` assembly filter `+Mvp24Hours*;-Mvp24Hours*.Test*`.

Regenerate: `./scripts/run-ci-local.ps1 -SkipSamples`

## Current snapshot — roadmap implementation (2026-08-03)

| Metric | Value |
|--------|-------|
| Line coverage | **59.4%** (44,321 / 74,605 coverable lines) |
| Branch coverage | **40.9%** |
| CI regression floor | **55%** |
| Product target | **95%** |
| Test projects | **19** in `src/Mvp24Hours.slnx` |

### Assembly breakdown

| Assembly | Line % | Covered / coverable |
|----------|--------|---------------------|
| Infrastructure.RabbitMQ | 38.7 | 3,288 / 8,478 |
| Infrastructure.Data.MongoDb | 41.0 | 3,663 / 8,919 |
| Infrastructure.Pipe | 55.8 | 3,614 / 6,472 |
| Infrastructure.Data.EFCore | 56.9 | 3,710 / 6,517 |
| Core | 60.9 | 4,529 / 7,430 |
| Infrastructure.Caching | 62.8 | 2,060 / 3,280 |
| WebAPI | 64.2 | 5,146 / 8,004 |
| Infrastructure.Identity.Keycloak | 66.3 | 948 / 1,428 |
| Application | 66.5 | 4,330 / 6,509 |
| Infrastructure.Cqrs | 67.5 | 3,040 / 4,500 |
| Infrastructure | 75.5 | 8,270 / 10,952 |
| Infrastructure.CronJob | 81.2 | 1,707 / 2,100 |
| Infrastructure.Caching.Redis | 100.0 | 16 / 16 |

### Roadmap phase status

| Phase | Gate | Status |
|-------|------|--------|
| 0 — CI split, filter fix | 45% | Done (44.5% → split unit/integration) |
| 1 — Unit quick wins | 55% | Done |
| 2 — Docker integration | 65% | In progress (59.4%) |
| 3 — WebAPI + Pipe | 75% | Tests added; gate pending |
| 4 — EFCore + App + Infra | 85% | Tests added; gate pending |
| 5 — Final sweep | 95% | Product target; ~35.6 pp remaining |

### CI architecture

- **Unit job**: `Category!=Integration` (ubuntu + matrix OS for tests)
- **Integration job**: `Category=Integration` (ubuntu + Docker)
- **Coverage gate**: merged Cobertura from both jobs; excludes test assemblies

See [Testing home](home.md) for local commands.
