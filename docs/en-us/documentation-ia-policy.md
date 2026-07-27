# Documentation Scope and Information Architecture

Status: **Accepted for documentation v1**

This page locks the scope and organizing principles for the English Mvp24Hours documentation overhaul. It defines the target state. Navigation changes are applied separately in task 1.7. The [Documentation Authoring Guide](documentation-authoring-guide.md) defines how pages are written and verified. The [AI Context Migration Map](ai-context-migration-map.md) freezes dispositions for every `ai-context/**` page. The [Options and DI Inventory](documentation-options-inventory.md) prioritizes configuration coverage for Phase 2–3.

## Scope

### In scope

- English user documentation under `docs/en-us/**`.
- Shared Docsify navigation and presentation files required to publish the English documentation.
- Public Mvp24Hours APIs, configuration, usage patterns, testing helpers, release notes, and migration guidance.
- Human-facing architecture guidance that helps users choose and apply Mvp24Hours modules.
- Compatibility pages for URLs moved or externalized during this initiative.
- A small bridge to external AI/MCP resources and machine-readable context downloads.

### Out of scope

- Translating or synchronizing `docs/pt-br/**`.
- Documenting roadmap features that do not exist in source.
- Maintaining Semantic Kernel, Semantic Kernel Graph, or Agent Framework guidance as Mvp24Hours product documentation.
- Reorganizing the external samples repository.
- Replacing canonical module documentation with generated AI context.

Source code and tests are authoritative when existing prose disagrees with implementation. Public behavior should be demonstrated by the matching projects under `src/Tests/**` and testing helpers under `src/Mvp24Hours.*/Testing/**`.

## Target information architecture

The primary human-facing navigation uses these top-level sections, in this order:

1. **Start** — home, getting started, installation, feature selection, and first application.
2. **Core & Domain** — core primitives, domain abstractions, entities, value objects, specifications, validation contracts, and shared contracts.
3. **Data & Persistence** — relational databases, EF Core, MongoDB, repositories, unit of work, transactions, and persistence-specific testing.
4. **Application Layer** — application services, mapping, validation flows, and application orchestration.
5. **CQRS & Messaging** — mediator, commands, queries, events, behaviors, event sourcing, sagas, RabbitMQ, and messaging integrations.
6. **Infrastructure Modules** — the infrastructure overview plus Pipeline, Caching, Email, SMS, File Storage, Secrets, Distributed Locking, HTTP clients, and module-specific helpers.
7. **Web API & Background Jobs** — ASP.NET Web API, OpenAPI, CronJob, Hangfire/Quartz abstractions, and hosted work.
8. **Observability & Resilience** — logging, tracing, metrics, health checks, retries, circuit breakers, and cross-cutting resilience guidance.
9. **Architecture Guides** — decision guidance, project structures, and validated architecture blueprints for human readers.
10. **Release & Migration** — current releases, breaking changes, upgrade procedures, and modernization migration.

**AI & MCP Resources** is a small, collapsed utility entry after the human-facing sections. It is not a primary documentation domain. It links to the external MCP bridge and, while needed for compatibility, machine-context downloads and the Cursor rule.

## Current navigation → target mapping

Use this table as the checklist for task 1.7. Keep existing URLs; change labels and grouping only.

| Current sidebar group | Target section | Notes |
|-----------------------|----------------|-------|
| Home, Getting Started | **Start** | Keep as the learning entry path. |
| Core Module | **Core & Domain** | Rename child “Infrastructure” → “Abstractions” in 1.7. |
| Database | **Data & Persistence** | Keep existing database URLs. |
| Application Services | **Application Layer** | Group with Mapping and Validation. |
| CQRS/Mediator | **CQRS & Messaging** | Keep CQRS tree; move Broker here from Infrastructure. |
| Infrastructure (Broker, Pipeline, Caching) | **Infrastructure Modules** / **CQRS & Messaging** | Broker → Messaging; Pipeline and Caching stay Infrastructure. |
| ASP.NET Web API | **Web API & Background Jobs** | Rename “Documentation”/Swagger page to OpenAPI under this section. |
| CronJob | **Web API & Background Jobs** | Join Web API and background job docs. |
| Observability | **Observability & Resilience** | `observability/home.md` remains the canonical entry. |
| Modernization (.NET 9) | **Release & Migration** + topic owners | Platform migration stays here; native feature pages link from their owning modules. |
| AI Context (For AI Agents) | **Architecture Guides** + **AI & MCP Resources** | Human guidance → Architecture Guides; machine/external AI → collapsed AI & MCP Resources. |
| Reference | Dissolved | OpenAPI → Web API; Mapping/Validation/Specification → Application/Core; Release/Migration → Release & Migration. |

## Module placement decisions

New or currently underrepresented modules have one primary home:

- Email, SMS, File Storage, Secrets, and Distributed Locking: **Infrastructure Modules**.
- Background Jobs and CronJob: **Web API & Background Jobs**.
- OpenAPI / Swagger (`documentation.md`): **Web API & Background Jobs**.
- Mapping: **Application Layer**.
- Validation: **Application Layer**, with domain-level contracts linked from **Core & Domain** when needed.
- Specification: pattern introduction in **Core & Domain**; CQRS query integration remains under **CQRS & Messaging**.
- Health Checks: canonical catalog under **Observability & Resilience**, with module-specific details linking back to it.
- Testing: a dedicated guide linked from **Start** and relevant module sections; module-specific test setup remains with the owning module.
- RabbitMQ: **CQRS & Messaging**.
- Pipeline and Caching: **Infrastructure Modules**.

A page may be linked from multiple learning paths, but it has only one canonical owner. Cross-links must not create duplicate configuration references.

## Canonical ownership

- Module pages own API signatures, dependency injection, Options, defaults, configuration, health/observability integration, and testing facts.
- Architecture Guides own why/when guidance, trade-offs, project structures, and solution shape.
- Release & Migration pages own version history, breaking changes, and upgrade procedures.
- MCP and machine-context resources own structured retrieval and agent-oriented discovery.

“AI Context” will not remain a primary navigation section. Useful human guidance is rewritten or relabeled as Architecture Guides; duplicated module facts become links or compatibility indexes; unrelated AI-framework material is externalized only after a destination exists.

## Canonical module page shape

New module pages and substantively rewritten module pages follow this order where applicable:

1. Overview
2. Install
3. DI registration
4. Options
5. Examples
6. Health and observability
7. Testing
8. Related links

Sections that do not apply may be omitted. Options sections follow the detailed authoring standard in the [Documentation Authoring Guide](documentation-authoring-guide.md).

## Content baseline

- User-facing prose added or updated by this initiative is English only.
- C# examples target `net10.0` and APIs available in the repository.
- Package examples must not retain stale `9.*` pins. Prefer an unpinned install command unless a verified version is materially relevant; when a version is shown, use the verified current release.
- Existing URLs remain in place during the initial sidebar reorganization.
- Root and English sidebars remain structurally synchronized while respecting their existing relative-link forms.

## URL compatibility policy

Docsify serves hash routes and this site has no redirect plugin. When a Markdown page moves:

1. Keep the old file as a compatibility stub for at least one major Mvp24Hours release.
2. State that the page moved and link directly to the canonical replacement with a Docsify-compatible relative link.
3. Do not duplicate API, configuration, or migration content in the stub.
4. Keep old routes in link checks until the compatibility window ends.
5. Remove a stub only in a major release and record the removal in `CHANGELOG.md`.

Sidebar links use the forms already required by each context: root navigation uses
`en-us/...`; the English locale sidebar uses `/en-us/...`. Content pages use relative
links. These conventions are compatible with `relativePath: true`.

## Deferred work

These decisions are locked here, but execution stays in later tasks:

| Concern | Task |
|---------|------|
| Sidebar reorganization and label cleanup | Done in Phase 1 |
| Docsify search depth, collapse behavior, redirect/stub policy | Done in Phase 1 |
| Detailed authoring rules (property tables, test citations) | Done — see [documentation-authoring-guide.md](documentation-authoring-guide.md) |
| Per-page AI Context disposition map | Done — see [ai-context-migration-map.md](ai-context-migration-map.md) |
| Version string and release metadata alignment | 1.1 |
| Broken Getting Started feature links | 1.2 |
| Infrastructure overview and new module pages | 3.1–3.11 |
| Architecture Guides and AI/MCP separation | 5.1–5.7 |
| Final navigation wiring | 6.1 |

This sequencing prevents this policy from prematurely changing URLs or publishing empty navigation entries.

## Acceptance criteria

- The scope, exclusions, target section order, module placement, and ownership rules are explicit.
- “AI Context” is rejected as a primary human documentation domain.
- The current → target navigation map is available for task 1.7.
- The module page shape and .NET 10/English/package-version baselines are fixed.
- Later tasks can reorganize navigation without reopening these decisions.
