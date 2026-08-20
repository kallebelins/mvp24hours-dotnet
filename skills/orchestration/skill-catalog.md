# Skill catalog (signals for skill-router)

Used by [skill-router.md](skill-router.md). Each table path is a logical domain
path. Resolve it as `catalog/<path>` in a global install, or `../<path>` while
running from this repository's `skills/orchestration/`. This router is **not**
listed as a target (do not hand off to itself).

**36 catalog files** = this router + **35** domain skills below.

## Architecture (7)

| Skill | Type | Path | Signals PT / EN | Do not use when |
|-------|------|------|-----------------|-----------------|
| demand-architect | Architect | `architecture/demand-architect.md` | demanda, US, RFC, user story, propor arquitetura, recursos, BOM, bill of materials | They want code or pattern depth; full codebase inventory; transformation ADR |
| solution-architect | Architect | `architecture/solution-architect.md` | qual estrutura, Minimal/Simple/Complex, blueprint, decision matrix, new project design | US/RFC with no constraints yet (demand first); existing-system ADR |
| clean-architecture-specialist | Specialist | `architecture/clean-architecture-specialist.md` | Clean Architecture, inward dependencies, use cases, interface adapters | Still choosing structure; they only want “N-Layers vs DDD” at architect level |
| ddd-specialist | Specialist | `architecture/ddd-specialist.md` | DDD, aggregate, bounded context, domain events, rich model | Structure not chosen; they want CQRS/mediator how-to without domain modeling |
| hexagonal-specialist | Specialist | `architecture/hexagonal-specialist.md` | hexagonal, ports and adapters, ACL adapters | Sync vs async / webhook choice → integration-architect first |
| event-driven-specialist | Specialist | `architecture/event-driven-specialist.md` | event-driven architecture, integration events as the system style | Only “put a queue after save” → messaging or integration; saga compensation → saga skill |
| microservices-specialist | Specialist | `architecture/microservices-specialist.md` | microservices, Aspire, independent deploy, service boundaries | Modular monolith / Complex N-Layers without service split |

## Data (5)

| Skill | Type | Path | Signals PT / EN | Do not use when |
|-------|------|------|-----------------|-----------------|
| data-architect | Architect | `data/data-architect.md` | qual persistência, EF vs Mongo vs Redis vs Dapper, choose store | Store already chosen; they want mapping/SQL/cache APIs |
| efcore-specialist | Specialist | `data/efcore-specialist.md` | EF Core, DbContext, repository, specification, migrations, SQL Server/Postgres | Choosing between stores; Dapper-only reads; document DB |
| dapper-specialist | Specialist | `data/dapper-specialist.md` | Dapper, SQL reads, hybrid EF writes + Dapper | Pure EF or Mongo; cache strategy |
| mongodb-specialist | Specialist | `data/mongodb-specialist.md` | MongoDB, document, BSON, collections | Relational/EF; Redis as primary store |
| redis-specialist | Specialist | `data/redis-specialist.md` | Redis data structures, Redis pub/sub as store, Redis package APIs | HybridCache / L1-L2 strategy → caching-architect |

## Messaging (3)

| Skill | Type | Path | Signals PT / EN | Do not use when |
|-------|------|------|-----------------|-----------------|
| messaging-architect | Architect | `messaging/messaging-architect.md` | broker, RabbitMQ pattern, queue vs bus, which messaging style | Sync vs async for partners still open → integration-architect; typed consumer code |
| rabbitmq-advanced-specialist | Specialist | `messaging/rabbitmq-advanced-specialist.md` | typed consumers, RPC/request-response, delayed/scheduled messages | Pattern not chosen; distributed saga |
| saga-orchestration-specialist | Specialist | `messaging/saga-orchestration-specialist.md` | saga, compensation, distributed transaction, choreography vs orchestration | In-process pipe/filter without broker → pipeline-architect |

## Integration (1)

| Skill | Type | Path | Signals PT / EN | Do not use when |
|-------|------|------|-----------------|-----------------|
| integration-architect | Architect | `integration/integration-architect.md` | webhook, HttpClient, partner API, sync vs async, BFF, anti-corruption, idempotency | Broker internals already chosen; inbound OpenAPI contract only |

## CQRS (3)

| Skill | Type | Path | Signals PT / EN | Do not use when |
|-------|------|------|-----------------|-----------------|
| cqrs-architect | Architect | `cqrs/cqrs-architect.md` | CQRS, read/write split, when to use CQRS | Structure still open; handler/behavior implementation; event store |
| mediator-patterns-specialist | Specialist | `cqrs/mediator-patterns-specialist.md` | AddMvpMediator, IMediatorCommand/Query, behaviors, handlers | Whether to adopt CQRS still open |
| event-sourcing-specialist | Specialist | `cqrs/event-sourcing-specialist.md` | event store, projections, snapshots, event replay, audit via events | CQRS without event sourcing; integration events only |

## Observability (2)

| Skill | Type | Path | Signals PT / EN | Do not use when |
|-------|------|------|-----------------|-----------------|
| observability-architect | Architect | `observability/observability-architect.md` | OpenTelemetry, traces, metrics, logs, telemetry stack | Circuit breaker/retry policy design |
| resilience-patterns-specialist | Specialist | `observability/resilience-patterns-specialist.md` | circuit breaker, retry, timeout, Microsoft.Extensions.Resilience | Telemetry pipeline / exporters |

## Single-file categories (10)

| Skill | Type | Path | Signals PT / EN | Do not use when |
|-------|------|------|-----------------|-----------------|
| pipeline-architect | Architect | `pipeline/pipeline-architect.md` | pipeline, pipes and filters, operation flow, checkpoints | Broker saga / compensation across services |
| caching-architect | Architect | `caching/caching-architect.md` | HybridCache, cache strategy, stampede, invalidation, L1/L2 | Redis as database/pub-sub specialist topics |
| infrastructure-architect | Architect | `infrastructure/infrastructure-architect.md` | email, SMS, blob/S3, secrets store, distributed lock, background infra | Cron schedule host only; Keycloak IdP |
| webapi-architect | Architect | `webapi/webapi-architect.md` | HTTP host, Map*, controllers, AddMvp24HoursWebEssential, Problem Details pipeline | Consumer contract / versioning / OpenAPI-first as the main ask |
| api-contract-architect | Architect | `webapi/api-contract-architect.md` | OpenAPI-first, API versioning, Problem Details envelope, breaking changes, `/openapi/v1.json` | Host composition / DI of WebAPI |
| testing-architect | Architect | `testing/testing-architect.md` | test pyramid, WebApplicationFactory, Testcontainers, fakes, smoke tests strategy | Single-feature implementation without test design |
| identity-architect | Architect | `identity/identity-architect.md` | Keycloak, JWT, authentication, authorization, IdP | Secrets, headers, PII masking, encryption at rest |
| security-architect | Architect | `security/security-architect.md` | secrets, LGPD/PII technical, security headers, Key Vault, rate limit, field encryption | Configuring Keycloak/JWT login |
| cronjob-architect | Architect | `cronjob/cronjob-architect.md` | cron, hosted service schedule, overlap prevention, worker timer | Generic background jobs / locks without schedule |

## Modernization (5)

| Skill | Type | Path | Signals PT / EN | Do not use when |
|-------|------|------|-----------------|-----------------|
| architecture-analyst | Architect | `modernization/architecture-analyst.md` | analisar legado, discovery, compliance review, inventory, no implementation | Greenfield US with no existing system; they already have a report and want ADR |
| architecture-proposal-architect | Architect | `modernization/architecture-proposal-architect.md` | ADR, strangler vs big-bang, target template, phases after discovery | No inventory yet; greenfield; they want to write port/rewrite code |
| port-transpilation-specialist | Specialist | `modernization/port-transpilation-specialist.md` | portar, transpile, Java/Python/other stack → Mvp24Hours, semantic port | Already on Mvp24Hours (that is rewrite); native API bump only |
| architecture-rewrite-architect | Architect | `modernization/architecture-rewrite-architect.md` | rewrite between templates, architecture-migration, already Mvp24Hours | Foreign stack; package/net10 bump without template change |
| dotnet-modernization-specialist | Specialist | `modernization/dotnet-modernization-specialist.md` | HybridCache APIs, TimeProvider, native OpenAPI, net10, package 9→10, same template | Template-to-template rewrite; foreign port |

## Disambiguation

| Conflict | Ask or prefer |
|----------|----------------|
| Demand vs implement | US/RFC + proposal, no code → `@demand-architect`. Constraints known + how to build → `@solution-architect` (then specialists). |
| Demand vs discovery | Business text, light repo → demand. Full existing-system inventory → `@architecture-analyst`. |
| Proposal vs port vs rewrite | Have discovery, need ADR → proposal. Foreign stack implementation → port. Already Mvp24Hours template change → rewrite. |
| webapi vs api-contract | Host, Map/controllers, DI → `@webapi-architect`. Contract, versions, OpenAPI-first → `@api-contract-architect`. |
| identity vs security | Login/IdP/JWT → `@identity-architect`. Secrets/headers/PII/encryption → `@security-architect`. |
| integration vs messaging vs event-driven | How this app talks to others (sync/async/webhook/BFF) → `@integration-architect`. Broker pattern inside the solution → `@messaging-architect`. Architecture *is* events → `@event-driven-specialist`. |
| data-architect vs store specialist | Store not chosen → `@data-architect`. EF/Dapper/Mongo/Redis already chosen → matching specialist. |
| caching vs redis | Cache strategy / HybridCache → `@caching-architect`. Redis APIs as data/pub-sub → `@redis-specialist`. |
| pipeline vs saga | In-process filters/checkpoints → `@pipeline-architect`. Distributed compensation → `@saga-orchestration-specialist`. |
| cqrs vs mediator vs event-sourcing | Whether/how to split reads → `@cqrs-architect`. Handler/behavior code → `@mediator-patterns-specialist`. Event store/projections → `@event-sourcing-specialist`. |
| Structure vs blueprint | Layout/host unknown + CQRS/DDD mentioned → ask: pick structure (`@solution-architect` / `@demand-architect`) vs implement the blueprint. |

## Sequential next steps (not same turn)

Typical **Próximo (depois)** only:

- `@demand-architect` → `@solution-architect`
- `@data-architect` → `@efcore-specialist` / `@mongodb-specialist` / `@dapper-specialist` / `@redis-specialist`
- `@cqrs-architect` → `@mediator-patterns-specialist`
- `@messaging-architect` → `@rabbitmq-advanced-specialist`
- `@architecture-analyst` → `@architecture-proposal-architect` → port **or** rewrite
- `@integration-architect` → `@webapi-architect` and/or `@messaging-architect`
