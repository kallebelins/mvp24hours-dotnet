# Discovery Playbook — Port to Mvp24Hours

Language-agnostic workflow for porting an existing codebase (any language) to C# with Mvp24Hours patterns. The agent performs discovery in the workspace; the MCP DevKit provides architecture, layers, samples, and validation.

## Phase A — Discovery (agent, before MCP)

Read the source code at the paths provided by the user. Extract:

| Concept | What to look for |
| --- | --- |
| Domain model | Entities, aggregates, value objects, enums |
| API surface | Routes, controllers, handlers, GraphQL, gRPC |
| Use cases | Services, managers, interactors, command handlers |
| Persistence | ORM models, repositories, migrations, queries |
| Messaging | Queues, topics, event publishers, webhooks |
| Auth | JWT, OAuth, session, API keys |
| Background work | Cron, workers, scheduled jobs |
| Cross-cutting | Validation, mapping, logging, caching |

Write a short summary: bounded contexts, main entities, integrations, and team/deployment constraints.

## Phase B — Mvp24Hours mapping (via MCP tools)

1. **`get_discovery_playbook`** — this document
2. **`resolve_architecture`** — pick template from the discovery summary
3. **`get_architecture_template`** + **`list_layers`** — where each discovered concept belongs
4. **`search_sample_patterns`** — find concrete implementations (DbContext, handlers, endpoints)
5. **`get_sample_file`** — read reference files from the matched sample
6. **`get_di_registration_hints`** — `Program.cs` wiring from reference sample
7. **`suggest_project_structure`** — target solution tree with product name
8. **`verify_doc_claim`** — confirm each Mvp24Hours API exists in `src/`
9. **`run_compliance_check`** — validate against checklist

## Concept → Layer mapping (generic)

| Discovered concept | Mvp24Hours layer | Reference action |
| --- | --- | --- |
| Domain model / entity | Core or Domain | `list_layers` + CRUD/DDD sample |
| Request/response DTO | Core or Application | `complex-crud-ef-customer-api` |
| Business rule / use case | Application | Application services or CQRS handlers |
| Database access | Infrastructure | `search_sample_patterns` → DbContext, Repository |
| HTTP / REST API | WebAPI or Host | Reference sample + `get_di_registration_hints` |
| Message broker | Infrastructure + Application | `resolve_feature` → rabbitmq or event-driven |
| Authentication | Infrastructure + WebAPI | `resolve_feature` → keycloak |
| Scheduled job | Worker or Host | `resolve_feature` → cronjob |
| Health / status | WebAPI | `resolve_feature` → health-checks |

## Rules

- Do **not** assume a language-specific mapping — infer from structure and naming.
- Use **Mvp24Hours Mediator**, not MediatR.
- Target **net10.0**, **Program.cs** composition, **native OpenAPI**, **OpenTelemetry**, **TimeProvider**.
- **`src/` and `src/Tests/`** override documentation when they conflict.
- Prefer the **canonical sample** for the chosen template tier before inventing patterns.

## Recommended MCP prompt

Use prompt **`port-to-mvp24hours`** with:

- `situation` — summary from Phase A
- `sourcePaths` — comma-separated paths to source code in the workspace
