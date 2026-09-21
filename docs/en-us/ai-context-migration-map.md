# AI Context Migration Map

Status: **Frozen for documentation v1**

This page assigns exactly one disposition to every markdown page under `docs/en-us/ai-context/**` before Phase 5 content edits. It freezes destinations so no page is silently lost. Physical moves and content rewrites happen in Phase 5; URLs stay stable until then.

Related policy:

- [Documentation Scope and Information Architecture](documentation-ia-policy.md)
- [Documentation Authoring Guide](documentation-authoring-guide.md)

## Inventory

| Metric | Task 0.4 claim | Actual |
|--------|----------------|--------|
| Page count | 41 | **22** |
| Location | `docs/en-us/ai-context/**` | Flat directory, 22 `.md` files |
| Sidebar coverage | — | Former pages are compatibility stubs; primary navigation uses **Architecture Guides** and collapsed **AI & MCP Resources** |

Downstream consumers deferred to Phase 5.7: `docs/mvp24hours.mdc`, `docs/llms_compact_en.txt`, `docs/llms_complete_en.txt`.

### Planned destination roots

| Destination | Path |
|-------------|------|
| Architecture Guides | `docs/en-us/guides/architecture/**` |
| AI & MCP bridge | `docs/en-us/ai-resources/home.md` |
| Testing guide | `docs/en-us/testing/home.md` |
| Deployment guide | `docs/en-us/guides/deployment/containerization.md` |
| Health catalog | `docs/en-us/infrastructure/health-checks.md` |

## Disposition summary

| Disposition | Count | Meaning |
|-------------|------:|---------|
| Keep/Rewrite | 13 | Preserve as human Architecture Guides or deployment guidance |
| Merge into canonical docs | 4 | Extract unique content into module owners, then stub |
| Convert to compatibility stub/index | 5 | Short index at the old URL; no second API truth |
| **Total** | **22** | |

## Exhaustive disposition table

| # | Current path | Disposition | Destination | Unique content to preserve |
|---|--------------|-------------|-------------|----------------------------|
| 1 | `ai-context/home.md` | Keep/Rewrite | `guides/architecture/home.md` (+ stub); Cursor/LLM downloads → `ai-resources/home.md` | Feature map; samples links; convention checklist; strip AI-only routing |
| 2 | `ai-context/decision-matrix.md` | Keep/Rewrite | `guides/architecture/decision-matrix.md` (+ stub) | Decision trees; template/DB/messaging/pattern matrices; combination recipes |
| 3 | `ai-context/project-structure.md` | Keep/Rewrite | `guides/architecture/project-structure.md` (+ stub) | Naming conventions; shared config list; entity/DTO layout patterns |
| 4 | `ai-context/architecture-templates.md` | Convert to stub/index | Stub → Architecture Guides landing + structure pages | Template Variations (audit, Dapper hybrid); advanced-template link table |
| 5 | `ai-context/structure-minimal-api.md` | Keep/Rewrite | `guides/architecture/structures/structure-minimal-api.md` (+ stub) | Directory tree; Program.cs/endpoints/DbContext; DI extension pattern |
| 6 | `ai-context/structure-simple-nlayers.md` | Keep/Rewrite | `guides/architecture/structures/structure-simple-nlayers.md` (+ stub) | Multi-project layout; service/controller examples; DI wiring |
| 7 | `ai-context/structure-complex-nlayers.md` | Keep/Rewrite | `guides/architecture/structures/structure-complex-nlayers.md` (+ stub) | Application layer layout; specifications; enterprise folders |
| 8 | `ai-context/template-cqrs.md` | Keep/Rewrite | `guides/architecture/blueprints/template-cqrs.md` (+ stub) | CQRS solution tree; rewrite MediatR → Mvp24Hours Mediator APIs |
| 9 | `ai-context/template-event-driven.md` | Keep/Rewrite | `guides/architecture/blueprints/template-event-driven.md` (+ stub) | Domain vs integration event layout; publisher/consumer blueprint |
| 10 | `ai-context/template-hexagonal.md` | Keep/Rewrite | `guides/architecture/blueprints/template-hexagonal.md` (+ stub) | Ports/adapters; inbound/outbound folder structure |
| 11 | `ai-context/template-clean-architecture.md` | Keep/Rewrite | `guides/architecture/blueprints/template-clean-architecture.md` (+ stub) | Dependency rules; use-case layout; rewrite MediatR references |
| 12 | `ai-context/template-ddd.md` | Keep/Rewrite | `guides/architecture/blueprints/template-ddd.md` (+ stub) | Bounded contexts; aggregates/factories; ubiquitous language |
| 13 | `ai-context/template-microservices.md` | Keep/Rewrite | `guides/architecture/blueprints/template-microservices.md` (+ stub) | Multi-service tree; autonomy; inter-service patterns; rewrite MediatR |
| 14 | `ai-context/database-patterns.md` | Convert to stub/index | Stub → `database/**` | Dapper hybrid; entity interface patterns; migration checklist |
| 15 | `ai-context/messaging-patterns.md` | Convert to stub/index | Stub → `broker*.md`, `cqrs/integration-rabbitmq.md` | Customer+RabbitMQ walkthrough; pipeline+messaging integration |
| 16 | `ai-context/observability-patterns.md` | Convert to stub/index | Stub → `observability/**`, health catalog | Multi-provider health catalog; correlation-ID pattern; mark NLog deprecated |
| 17 | `ai-context/modernization-patterns.md` | Convert to stub/index | Stub → `modernization/**` | Only merge Mvp24Hours wiring missing after v10 audit |
| 18 | `ai-context/testing-patterns.md` | Merge into canonical docs | `testing/home.md` (+ stub) | Test solution layout; Bogus; WebApplicationFactory; naming conventions |
| 19 | `ai-context/security-patterns.md` | Merge into canonical docs | `webapi-advanced.md`, `infrastructure/secrets-security.md` (+ stub) | JWT/RBAC templates; password hashing; CORS patterns |
| 20 | `ai-context/error-handling-patterns.md` | Merge into canonical docs | `webapi-advanced.md`, `core/exceptions.md`, related module pages (+ stub) | Domain exception hierarchy; `IBusinessResult<T>` patterns; middleware mapping |
| 21 | `ai-context/api-versioning-patterns.md` | Merge into canonical docs | Expand `webapi-advanced.md` (+ stub) | Versioning strategy matrix; Swagger multi-version; sunset headers |
| 22 | `ai-context/containerization-patterns.md` | Keep/Rewrite | `guides/deployment/containerization.md` (+ stub) | Dockerfile/Compose/nginx/CI; update base images to .NET 10 |

## Duplicate clusters

| Cluster | Pages | Resolution |
|---------|-------|------------|
| Structure triple | `architecture-templates.md` and three `structure-*.md` pages | Keep the three structure pages; stub the templates index after merging variations |
| Navigation hub | `home.md`, `decision-matrix.md`, `project-structure.md`, `architecture-templates.md` | One Architecture Guides landing plus dedicated decision/structure pages |
| Pattern mega-pages | database/messaging/observability/modernization patterns | Short indexes; merge unique slices into module docs |
| MediatR blueprints | CQRS, Clean Architecture, Microservices | Rewrite to Mvp24Hours Mediator in task 5.3 |

## Recommended merge targets

| Source | Unique content | Canonical target |
|--------|----------------|------------------|
| `database-patterns.md` | Dapper hybrid, entity patterns, migration checklist | `database/efcore-advanced.md`, `database/use-entity.md` |
| `messaging-patterns.md` | Customer+RabbitMQ end-to-end example | `cqrs/integration-rabbitmq.md` |
| `observability-patterns.md` | Multi-provider health-check catalog | `infrastructure/health-checks.md` |
| `testing-patterns.md` | Full test cookbook | `testing/home.md` |
| `security-patterns.md` | JWT/RBAC/password hashing | `webapi-advanced.md`, `infrastructure/secrets-security.md` |
| `error-handling-patterns.md` | Domain exception hierarchy | `core/exceptions.md`, `webapi-advanced.md` |
| `api-versioning-patterns.md` | Versioning strategy matrix | `webapi-advanced.md` |
| `architecture-templates.md` | Template Variations | Architecture Guides landing or structure pages |

## Compatibility stub policy

Every moved page retains a Markdown stub at its current Docsify URL for at least one major release:

```markdown
# Moved: {title}

This page moved to [{new location}]({relative-or-external-link}).

> Human architecture guidance lives in Architecture Guides.
> Module APIs live in canonical module documentation.
```

Transition bridge: `docs/en-us/ai-resources/home.md` as a collapsed sidebar entry linking the MCP bridge, external repos, and machine-context downloads until MCP cutover.

## Phase 5 quality flags

- Replace stale `9.*` pins and .NET 9 container images with v10.9.0 / `net10.0`.
- Rewrite MediatR APIs in retained blueprints to Mvp24Hours Mediator APIs.
- Mark NLog-first observability guidance as deprecated relative to OpenTelemetry.
- Prefer Native OpenAPI / current WebAPI extensions over Swagger-only or `Startup.cs` examples where inappropriate.

## Acceptance criteria

- All 22 `ai-context/**` pages have exactly one disposition.
- Architecture decisions, project structures, blueprints, testing, and containerization content have explicit destinations.
- Unique merge content has a canonical target so no page is silently lost.
- Physical moves remain deferred to Phase 5; current URLs stay valid until stubs are written.
