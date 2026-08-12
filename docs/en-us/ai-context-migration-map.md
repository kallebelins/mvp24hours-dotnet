# AI Context Migration Map

Status: **Frozen for documentation v1**

This page assigns exactly one disposition to every markdown page under `docs/en-us/ai-context/**` before Phase 5 content edits. It freezes destinations so no page is silently lost. Physical moves and content rewrites happen in Phase 5; URLs stay stable until then.

Related policy:

- [Documentation Scope and Information Architecture](documentation-ia-policy.md)
- [Documentation Authoring Guide](documentation-authoring-guide.md)

## Inventory

| Metric | Task 0.4 claim | Actual |
|--------|----------------|--------|
| Page count | 41 | **42** |
| Location | `docs/en-us/ai-context/**` | Flat directory, 42 `.md` files |
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
| External MCP/AI project | `mvp24hours-mcp-ai/docs/**` (URL TBD) |
| Semantic Kernel Graph docs | `https://skgraph.dev/` / `semantic-kernel-graph-docs` |

## Disposition summary

| Disposition | Count | Meaning |
|-------------|------:|---------|
| Keep/Rewrite | 13 | Preserve as human Architecture Guides or deployment guidance |
| Merge into canonical docs | 4 | Extract unique content into module owners, then stub |
| Convert to compatibility stub/index | 5 | Short index at the old URL; no second API truth |
| Move to external MCP/AI project | 20 | Externalize SK, SKG, Agent Framework, and AI indexes |
| **Total** | **42** | |

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
| 23 | `ai-context/ai-implementation-index.md` | Move external | `mvp24hours-mcp-ai/docs/ai-implementation-index.md` (+ stub) | Approach comparison tables; Mvp24Hours+AI sketch; external repo links |
| 24 | `ai-context/ai-decision-matrix.md` | Move external | `mvp24hours-mcp-ai/docs/ai-decision-matrix.md` (+ stub) | SK vs SKG vs Agent Framework decision tree |
| 25 | `ai-context/template-sk-chat-completion.md` | Move external | `mvp24hours-mcp-ai/docs/templates/sk/` (+ stub) | Kernel/provider setup; streaming; conversation history |
| 26 | `ai-context/template-sk-plugins.md` | Move external | `mvp24hours-mcp-ai/docs/templates/sk/` (+ stub) | Plugin authoring; function calling |
| 27 | `ai-context/template-sk-rag-basic.md` | Move external | `mvp24hours-mcp-ai/docs/templates/sk/` (+ stub) | Vector store; ingestion; retrieval prompts |
| 28 | `ai-context/template-sk-planners.md` | Move external | `mvp24hours-mcp-ai/docs/templates/sk/` (+ stub) | Handlebars planner; auto-planning flows |
| 29 | `ai-context/template-skg-graph-executor.md` | Move external | `semantic-kernel-graph/docs/templates/` (+ stub) | Graph structure; sequential/parallel/conditional execution |
| 30 | `ai-context/template-skg-react-agent.md` | Move external | `semantic-kernel-graph/docs/templates/` (+ stub) | Reason→Act→Observe loop |
| 31 | `ai-context/template-skg-chain-of-thought.md` | Move external | `semantic-kernel-graph/docs/templates/` (+ stub) | Step-by-step reasoning graphs |
| 32 | `ai-context/template-skg-chatbot-memory.md` | Move external | `semantic-kernel-graph/docs/templates/` (+ stub) | Persistent conversation memory |
| 33 | `ai-context/template-skg-multi-agent.md` | Move external | `semantic-kernel-graph/docs/templates/` (+ stub) | SKG multi-agent coordination |
| 34 | `ai-context/template-skg-document-pipeline.md` | Move external | `semantic-kernel-graph/docs/templates/` (+ stub) | Multi-stage document analysis |
| 35 | `ai-context/template-skg-human-in-loop.md` | Move external | `semantic-kernel-graph/docs/templates/` (+ stub) | Approval gates; human oversight |
| 36 | `ai-context/template-skg-checkpointing.md` | Move external | `semantic-kernel-graph/docs/templates/` (+ stub) | Checkpoint/recovery flows |
| 37 | `ai-context/template-skg-streaming.md` | Move external | `semantic-kernel-graph/docs/templates/` (+ stub) | Real-time event streaming |
| 38 | `ai-context/template-skg-observability.md` | Move external | `semantic-kernel-graph/docs/templates/` (+ stub) | Graph execution metrics |
| 39 | `ai-context/template-agent-framework-basic.md` | Move external | `mvp24hours-mcp-ai/docs/templates/agent-framework/` (+ stub) | Provider-agnostic AI abstractions |
| 40 | `ai-context/template-agent-framework-workflows.md` | Move external | `mvp24hours-mcp-ai/docs/templates/agent-framework/` (+ stub) | Agent Framework workflow patterns |
| 41 | `ai-context/template-agent-framework-multi-agent.md` | Move external | `mvp24hours-mcp-ai/docs/templates/agent-framework/` (+ stub) | Enterprise multi-agent orchestration |
| 42 | `ai-context/template-agent-framework-middleware.md` | Move external | `mvp24hours-mcp-ai/docs/templates/agent-framework/` (+ stub) | Agent middleware pipeline |

## Duplicate clusters

| Cluster | Pages | Resolution |
|---------|-------|------------|
| Structure triple | `architecture-templates.md` and three `structure-*.md` pages | Keep the three structure pages; stub the templates index after merging variations |
| Navigation hub | `home.md`, `decision-matrix.md`, `project-structure.md`, `architecture-templates.md` | One Architecture Guides landing plus dedicated decision/structure pages |
| Two Decision Matrix labels | `decision-matrix.md` vs `ai-decision-matrix.md` | Keep Mvp24Hours matrix; externalize AI-framework matrix |
| Pattern mega-pages | database/messaging/observability/modernization patterns | Short indexes; merge unique slices into module docs |
| MediatR blueprints | CQRS, Clean Architecture, Microservices | Rewrite to Mvp24Hours Mediator in task 5.3 |
| Multi-agent frameworks | SKG vs Agent Framework multi-agent | Both externalize; preserve separately |
| AI index trio | `home.md` AI section, `ai-implementation-index.md`, `ai-decision-matrix.md` | Externalize indexes; strip from Architecture Guides landing |

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
> AI framework templates live in the external MCP / Semantic Kernel Graph project.
```

## Externalization block

| Framework group | Pages | Primary external destination |
|-----------------|------:|------------------------------|
| AI indexes | 2 | `mvp24hours-mcp-ai/docs/` |
| Semantic Kernel | 4 | `mvp24hours-mcp-ai/docs/templates/sk/` |
| Semantic Kernel Graph | 10 | [skgraph.dev](https://skgraph.dev/) / `semantic-kernel-graph-docs` |
| Agent Framework | 4 | `mvp24hours-mcp-ai/docs/templates/agent-framework/` |

Transition bridge: `docs/en-us/ai-resources/home.md` as a collapsed sidebar entry linking the MCP bridge, external repos, and machine-context downloads until MCP cutover.

## Phase 5 quality flags

- Replace stale `9.*` pins and .NET 9 container images with v10.8.0 / `net10.0`.
- Rewrite MediatR APIs in retained blueprints to Mvp24Hours Mediator APIs.
- Mark NLog-first observability guidance as deprecated relative to OpenTelemetry.
- Prefer Native OpenAPI / current WebAPI extensions over Swagger-only or `Startup.cs` examples where inappropriate.
- Do not validate external AI-framework packages against Mvp24Hours `src/` tests; externalize them instead.

## Acceptance criteria

- All 42 `ai-context/**` pages have exactly one disposition.
- Architecture decisions, project structures, blueprints, testing, and containerization content have explicit destinations.
- Semantic Kernel, Semantic Kernel Graph, Agent Framework, and AI-framework decision/index pages are marked for externalization.
- Unique merge content has a canonical target so no page is silently lost.
- Physical moves remain deferred to Phase 5; current URLs stay valid until stubs are written.
