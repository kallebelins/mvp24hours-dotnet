# Skills Catalog

Mvp24Hours ships **36 portable AI agent skills** (1 orchestrator + 35 architect/specialist skills) that you can copy into any project's `.cursor/skills/` or `.github/skills/` folder. Each skill is a specialized expert that guides architecture decisions, implementation patterns, anti-patterns, migration paths, and testing strategy for one area of the framework, using a **MCP-first approach** against the [local MCP DevKit](home.md).

This page summarizes the catalog so it is discoverable from the published site. The full skill files, decision matrix, and capability matrix live in [`skills/README.md`](https://github.com/kallebelins/mvp24hours-dotnet/tree/main/skills) in the repository — that file is the canonical source; this page does not duplicate its content.

## Quick start

1. Copy the `skills/` folder into your project (`.cursor/skills/` for Cursor, `.github/skills/` for VS Code Copilot).
2. Configure the Mvp24Hours MCP server — see [Local MCP DevKit setup](home.md#local-mcp-devkit) for the Cursor and VS Code JSON configuration.
3. Mention `@skill-router` when you are unsure which skill fits, or `@skill-name` to go straight to a domain skill (e.g. `@efcore-specialist`, `@demand-architect`).

## Categories at a glance

| Category | Skills | Start with |
|----------|--------|------------|
| Orchestration | 1 | `@skill-router` — routes an ambiguous demand to one catalog skill or an MCP playbook |
| Architecture | 7 | `@demand-architect` (US/RFC in hand) or `@solution-architect` (constraints known) |
| Data & Persistence | 5 | `@data-architect` — chooses between EF Core, MongoDB, Redis, Dapper |
| Messaging & Message Broker | 3 | `@messaging-architect` — broker pattern selection |
| System Integration | 1 | `@integration-architect` — sync vs async, webhooks, BFF, anti-corruption |
| CQRS & Mediator | 3 | `@cqrs-architect` — read/write split and Mvp24Hours mediator |
| Observability & Resilience | 2 | `@observability-architect` — telemetry stack design |
| Pipeline (Pipes & Filters) | 1 | `@pipeline-architect` — in-process operation flows |
| Caching | 1 | `@caching-architect` — HybridCache, L1/L2, invalidation |
| Infrastructure | 1 | `@infrastructure-architect` — email, SMS, file storage, locks |
| Web API | 2 | `@webapi-architect` (host) or `@api-contract-architect` (consumer contract) |
| Testing | 1 | `@testing-architect` — test pyramid, fakes, Testcontainers |
| Identity & Security | 2 | `@identity-architect` (Keycloak/JWT) or `@security-architect` (secrets, PII) |
| CronJob | 1 | `@cronjob-architect` — scheduled hosted services |
| Modernization & Transformation | 5 | `@architecture-analyst` (discovery) → `@architecture-proposal-architect` (ADR) → port or rewrite |

**Not sure where to start?** Ask `@skill-router` — it asks one clarifying question when two or more paths fit, and does not implement until a route is confirmed.

## Two axes: structure vs blueprint vs capability

Skills separate two independent decisions. Pick the **structure** first, then a **blueprint** or **capability** only when it solves a concrete problem.

| Axis | Meaning | Examples |
|------|---------|----------|
| **Structure** | Host/project layout: Minimal, Simple N-Layers, Complex N-Layers | `@solution-architect` |
| **Blueprint** | Pattern on top of a structure: CQRS, DDD, Hexagonal, Clean Architecture, Event-Driven, Microservices | `@cqrs-architect`, `@ddd-specialist`, `@hexagonal-specialist` |
| **Capability** | Feature sample: event sourcing, saga, Keycloak | `@event-sourcing-specialist`, `@saga-orchestration-specialist`, `@identity-architect` |

See [Architecture Guides](../guides/architecture/home.md) for the human-facing version of the same structure/blueprint decision.

## Common workflows

**New project**

```
0. @skill-router          - if the path is unclear
1. @demand-architect       - analyze a US/RFC; structure + resource BOM
2. @solution-architect     - deepen the architecture pattern
3. @data-architect         - choose persistence strategy
4. @webapi-architect       - design HTTP endpoints
5. @testing-architect      - define test strategy
```

**Architecture migration**

```
1. @architecture-analyst            - inventory or compliance review
2. @architecture-proposal-architect - structure first, optional blueprint
3. @port-transpilation-specialist (foreign stack) OR @architecture-rewrite-architect (already Mvp24Hours)
4. @dotnet-modernization-specialist - only for native APIs / package bump
```

See [`skills/README.md`](https://github.com/kallebelins/mvp24hours-dotnet/tree/main/skills) for the "add feature" and "production readiness" workflows, and the full decision matrix mapping every need to a primary and supporting skill.

## Full catalog and skill files

This page is a summary. For the complete, authoritative catalog — every skill listed with type, focus, samples by MCP tier, decision matrix, capability matrix, and package reference — see:

- [`skills/README.md`](https://github.com/kallebelins/mvp24hours-dotnet/tree/main/skills) on GitHub (canonical catalog)
- [`skills/SKILL_TEMPLATE.md`](https://github.com/kallebelins/mvp24hours-dotnet/blob/main/skills/SKILL_TEMPLATE.md) if you plan to revise or extend a skill

## Related documentation

- [AI & MCP Resources — Overview](home.md) — MCP DevKit setup for Cursor and VS Code
- [Agent Rules](agent-rules.md) — always-on project context files for Cursor, Kiro, VS Code Copilot, Claude Code, and other tools (complements this on-demand skill catalog)
- [Architecture Guides](../guides/architecture/home.md) — human-facing structure and blueprint decisions
- [Getting Started](../getting-started.md)
