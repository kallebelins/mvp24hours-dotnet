# Coverage baseline (versioned)

Consolidated line coverage for the main solution (`src/Mvp24Hours.slnx`), measured with merged unit + integration Cobertura reports and `reportgenerator` assembly filter `+Mvp24Hours*;-Mvp24Hours*.Test*`.

Regenerate: `./scripts/run-ci-local.ps1 -SkipSamples`

## Current snapshot — Phase 3 complete (2026-08-03)

| Metric | Value |
|--------|-------|
| Line coverage | **75.7%** (56,570 / 74,640 coverable lines) |
| Branch coverage | **57.5%** |
| CI regression floor | **75%** |
| Product target | **95%** |
| Test projects | **19** in `src/Mvp24Hours.slnx` |

### Assembly breakdown

| Assembly | Line % | Covered / coverable |
|----------|--------|---------------------|
| Infrastructure.Data.MongoDb | 72.0 | 6,431 / 8,922 |
| Infrastructure.Data.EFCore | 71.5 | 4,660 / 6,517 |
| Infrastructure.Cqrs | 72.2 | 3,251 / 4,500 |
| Infrastructure.Caching | 71.7 | 2,354 / 3,280 |
| Core | 74.2 | 5,538 / 7,462 |
| WebAPI | 74.3 | 5,951 / 8,004 |
| Infrastructure.Pipe | 74.5 | 4,823 / 6,472 |
| Infrastructure.RabbitMQ | 75.3 | 6,392 / 8,478 |
| Application | 76.9 | 5,008 / 6,509 |
| Infrastructure.CronJob | 81.2 | 1,707 / 2,100 |
| Infrastructure.Identity.Keycloak | 88.0 | 1,257 / 1,428 |
| Infrastructure | 83.8 | 9,182 / 10,952 |
| Infrastructure.Caching.Redis | 100.0 | 16 / 16 |

### Roadmap phase status

| Phase | Gate | Status |
|-------|------|--------|
| 0 — CI split, filter fix | 45% | Done (44.5% → split unit/integration) |
| 1 — Unit quick wins | 55% | Done |
| 2 — Docker integration | 65% | Done (59.4% → 65.9%) |
| 3 — Consolidated ≥75% | 75% | **Done** (65.9% → 75.7%) |
| 4 — EFCore + App + Infra | 85% | Tests added; gate pending |
| 5 — Final sweep | 95% | Product target; ~19.3 pp remaining |

### CI architecture

- **Unit job**: `Category!=Integration` (ubuntu + matrix OS for tests)
- **Integration job**: `Category=Integration` (ubuntu + Docker)
- **Coverage gate**: merged Cobertura from both jobs; excludes test assemblies

See [Testing home](home.md) for local commands.
