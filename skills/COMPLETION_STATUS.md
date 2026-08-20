# Mvp24Hours Skills - Completion Status

> **Last Updated**: August 2026  
> **Status**: Catalog complete (36 named skills + documentation)

## Overview

This document tracks the Mvp24Hours Specialized Architect Skills ecosystem.

### Summary Statistics

- **Total skills in catalog**: 36 (35 domain + skill-router; see README)
- **Completed**: 36 (100%)
- **Documentation**: README, Template, Generation Guide, this Status, Project Summary
- **Categories**: 15
- **MCP Integration**: Required for canonical APIs

---

## Completion Status by Category

### Orchestration (1/1)

| Skill | Type | Status | File |
|-------|------|--------|------|
| skill-router | Orchestrator | Complete | `orchestration/skill-router.md` |

Signals reference: `orchestration/skill-catalog.md` and `orchestration/mcp-scenarios.md` (not separate skills).

### Architecture (7/7)

| Skill | Type | Status | File |
|-------|------|--------|------|
| demand-architect | Architect | Complete | `architecture/demand-architect.md` |
| solution-architect | Architect | Complete | `architecture/solution-architect.md` |
| clean-architecture-specialist | Specialist | Complete | `architecture/clean-architecture-specialist.md` |
| ddd-specialist | Specialist | Complete | `architecture/ddd-specialist.md` |
| hexagonal-specialist | Specialist | Complete | `architecture/hexagonal-specialist.md` |
| event-driven-specialist | Specialist | Complete | `architecture/event-driven-specialist.md` |
| microservices-specialist | Specialist | Complete | `architecture/microservices-specialist.md` |

### Data & Persistence (5/5)

| Skill | Type | Status | File |
|-------|------|--------|------|
| data-architect | Architect | Complete | `data/data-architect.md` |
| efcore-specialist | Specialist | Complete | `data/efcore-specialist.md` |
| dapper-specialist | Specialist | Complete | `data/dapper-specialist.md` |
| mongodb-specialist | Specialist | Complete | `data/mongodb-specialist.md` |
| redis-specialist | Specialist | Complete | `data/redis-specialist.md` |

### Messaging (3/3)

| Skill | Type | Status | File |
|-------|------|--------|------|
| messaging-architect | Architect | Complete | `messaging/messaging-architect.md` |
| rabbitmq-advanced-specialist | Specialist | Complete | `messaging/rabbitmq-advanced-specialist.md` |
| saga-orchestration-specialist | Specialist | Complete | `messaging/saga-orchestration-specialist.md` |

### CQRS (3/3)

| Skill | Type | Status | File |
|-------|------|--------|------|
| cqrs-architect | Architect | Complete | `cqrs/cqrs-architect.md` |
| event-sourcing-specialist | Specialist | Complete | `cqrs/event-sourcing-specialist.md` |
| mediator-patterns-specialist | Specialist | Complete | `cqrs/mediator-patterns-specialist.md` |

### Observability & Resilience (2/2)

| Skill | Type | Status | File |
|-------|------|--------|------|
| observability-architect | Architect | Complete | `observability/observability-architect.md` |
| resilience-patterns-specialist | Specialist | Complete | `observability/resilience-patterns-specialist.md` |

### Single-file categories (10/10)

| Skill | Type | Status | File |
|-------|------|--------|------|
| pipeline-architect | Architect | Complete | `pipeline/pipeline-architect.md` |
| caching-architect | Architect | Complete | `caching/caching-architect.md` |
| infrastructure-architect | Architect | Complete | `infrastructure/infrastructure-architect.md` |
| webapi-architect | Architect | Complete | `webapi/webapi-architect.md` |
| api-contract-architect | Architect | Complete | `webapi/api-contract-architect.md` |
| testing-architect | Architect | Complete | `testing/testing-architect.md` |
| identity-architect | Architect | Complete | `identity/identity-architect.md` |
| security-architect | Architect | Complete | `security/security-architect.md` |
| cronjob-architect | Architect | Complete | `cronjob/cronjob-architect.md` |
| integration-architect | Architect | Complete | `integration/integration-architect.md` |

### Modernization & Transformation (5/5)

| Skill | Type | Status | File |
|-------|------|--------|------|
| architecture-analyst | Architect | Complete | `modernization/architecture-analyst.md` |
| architecture-proposal-architect | Architect | Complete | `modernization/architecture-proposal-architect.md` |
| port-transpilation-specialist | Specialist | Complete | `modernization/port-transpilation-specialist.md` |
| architecture-rewrite-architect | Architect | Complete | `modernization/architecture-rewrite-architect.md` |
| dotnet-modernization-specialist | Specialist | Complete | `modernization/dotnet-modernization-specialist.md` |

---

## Quality notes

- Newer skills target **350–500 lines** and MCP-verified APIs.
- Phase 1 API corrections (August 2026): Mongo `AddMvp24HoursDbContext`, `IMediator`, `RegisterHandlersFromAssemblyContaining`.
- Phase 1 trim (August 2026): `data-architect` and `efcore-specialist` rewritten to the 350–500 line template; `cqrs-architect` and `messaging-architect` already fit.
- Always confirm DI names with `find_source_symbol` before generating code.
- Vocabulary (August 2026): **Minimal / Simple / Complex** are structure templates (`minimal-api`, `simple-nlayers`, `complex-nlayers`). Sample `.Tier` from `list_samples` may be Blueprint or Capability even when the id starts with `complex-`. Never infer tier from the sample prefix.

---

## Documentation Status

| Document | Status | File |
|----------|--------|------|
| Master README | Complete | `README.md` |
| Skill Template | Complete | `SKILL_TEMPLATE.md` |
| Completion Status | Complete | `COMPLETION_STATUS.md` |
| Generation Guide | Historical outlines | `SKILLS_GENERATION_GUIDE.md` |
| Project Summary | Complete | `PROJECT_SUMMARY.md` |

---

## Maintenance

- Refresh skills when Mvp24Hours packages or MCP docs change.
- Current source target: .NET 10; published package version may still be 9.1.21 — see `docs/en-us/migration.md`.

**Last Updated**: August 2026  
**Status**: Catalog complete
