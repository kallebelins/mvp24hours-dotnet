# Mvp24Hours Specialized Architect Skills

> **Enterprise-ready AI agent skills for building .NET 10 applications with Mvp24Hours library**

## Overview

This skills ecosystem provides **36 skills** (1 orchestrator + **35** architect/specialist skills) organized into **15 categories**, covering the complete Mvp24Hours .NET 10 framework. Domain skills use a **MCP-first approach**, querying the Mvp24Hours MCP DevKit for canonical documentation, architecture templates, and runnable samples.

### What are these skills?

These are **portable AI agent skill files** designed to be copied into any project's `.cursor/skills/` or `.github/skills/` folder. Each skill acts as a specialized expert that:

- ✅ **Guides architecture decisions** with clear decision trees and trade-off analysis
- ✅ **Provides implementation patterns** using Mvp24Hours NuGet packages
- ✅ **References MCP resources** as the canonical source of truth
- ✅ **Links to runnable samples** for concrete examples
- ✅ **Identifies anti-patterns** and how to avoid them
- ✅ **Defines migration paths** from Minimal → Simple → Complex **structures**, then optional **blueprints**
- ✅ **Explains integration scenarios** between specialties
- ✅ **Outlines testing strategies** specific to each pattern

---

## Quick Start

### For Cursor IDE Users

1. **Copy skills to your project**:
   ```bash
   # Copy entire skills folder to your project
   cp -r skills/ /path/to/your/project/.cursor/skills/
   ```

2. **Configure Mvp24Hours MCP** (if not already configured):
   ```json
   // .cursor/mcp.json
   {
     "servers": {
       "mvp24hours": {
         "type": "stdio",
         "command": "dotnet",
         "args": ["run", "--project", "path/to/mvp24hours-dotnet/mcp/src/Mvp24Hours.Mcp/Mvp24Hours.Mcp.csproj"],
         "env": { "MVP24HOURS_REPO_ROOT": "path/to/mvp24hours-dotnet" }
       }
     }
   }
   ```

3. **Use skills in chat**:
   ```
   @skill-router Por onde começo com esta demanda?
   @demand-architect Analise esta US e proponha arquitetura e recursos
   @solution-architect I need to design a customer management API with complex business rules
   ```

### For VS Code GitHub Copilot Users

1. **Copy skills to your project**:
   ```bash
   cp -r skills/ /path/to/your/project/.github/skills/
   ```

2. **Configure MCP** in `.vscode/mcp.json` (same format as above)

3. **Use in Agent mode** with `@skillname` mention

---

## Skills Catalog

### Orchestration (1 skill)

Routes the user to one of the 35 domain skills. Does not implement.

| Skill | Type | Focus | When to Use |
|-------|------|-------|-------------|
| **[skill-router](orchestration/skill-router.md)** | Orchestrator | Catalog handoff + MCP playbook triage | Unsure which skill; ambiguous demand; “qual skill” / “por onde começo”; criar API / migrar / port |

**Signals**: [orchestration/skill-catalog.md](orchestration/skill-catalog.md). MCP playbooks: [orchestration/mcp-scenarios.md](orchestration/mcp-scenarios.md).

### 📐 Architecture Patterns (7 skills)

Comprehensive guidance on solution architecture selection and implementation.

| Skill | Type | Focus | When to Use |
|-------|------|-------|-------------|
| **[demand-architect](architecture/demand-architect.md)** | 🎯 Architect | US/RFC → structure, blueprint, resource BOM | Analyze a demanda; propose architecture and packages (no code) |
| **[solution-architect](architecture/solution-architect.md)** | 🎯 Architect | Pattern selection across 9 architectures | Starting new project, architecture review |
| **[clean-architecture-specialist](architecture/clean-architecture-specialist.md)** | 🔧 Specialist | Inward dependency flow enforcement | Long-term maintainability, framework independence |
| **[ddd-specialist](architecture/ddd-specialist.md)** | 🔧 Specialist | Rich domain models with aggregates | Complex business domains, domain expertise available |
| **[hexagonal-specialist](architecture/hexagonal-specialist.md)** | 🔧 Specialist | Ports & adapters isolation | Many external integrations, replaceable adapters |
| **[event-driven-specialist](architecture/event-driven-specialist.md)** | 🔧 Specialist | Async workflows, integration events | Loose coupling, eventual consistency acceptable |
| **[microservices-specialist](architecture/microservices-specialist.md)** | 🔧 Specialist | Independent deployable services | Team autonomy, independent scaling needs |

**Decision Matrix**: Start with `demand-architect` when the input is a **US/RFC**. Otherwise start with `solution-architect` to pick **structure** (Minimal / Simple / Complex), then a **blueprint** only if needed. Specialists implement the blueprint; they do not treat `complex-*` sample ids as Complex N-Layers.

**Key Samples** (MCP Tier):
- Structures: `minimal-crud-ef-customer-api` (**Minimal**), `simple-crud-ef-customer-api` (**Simple**), `complex-crud-ef-customer-api` (**Complex**)
- Blueprints: `complex-cqrs-ef-customer-api`, `complex-ddd-ef-customer-api`, `complex-hexagonal-customer-api`, `complex-clean-architecture-customer-api`, `complex-event-driven-rabbitmq-customer-api`, `microservices-aspire-customer`

---

### 💾 Data & Persistence (5 skills)

Data access strategies, repositories, and persistence patterns.

| Skill | Type | Focus | When to Use |
|-------|------|-------|-------------|
| **[data-architect](data/data-architect.md)** | 🎯 Architect | Persistence technology selection | Choosing between EF Core, MongoDB, Redis, Dapper |
| **[efcore-specialist](data/efcore-specialist.md)** | 🔧 Specialist | EF Core advanced patterns | Relational databases (SQL Server, PostgreSQL, MySQL) |
| **[dapper-specialist](data/dapper-specialist.md)** | 🔧 Specialist | EF writes + Dapper SQL reads | Hot lists, reports, hybrid sample |
| **[mongodb-specialist](data/mongodb-specialist.md)** | 🔧 Specialist | Document database patterns | Schema flexibility, horizontal scaling needs |
| **[redis-specialist](data/redis-specialist.md)** | 🔧 Specialist | Caching and pub/sub patterns | High-performance caching, real-time features |

**Key Samples** (MCP Tier):
- `minimal-crud-ef-customer-api` / `minimal-crud-mongodb-customer-api` — **Minimal**
- `simple-crud-ef-customer-api` / `simple-crud-ef-dapper-customer-api` / `simple-crud-mongodb-customer-api` / `simple-crud-redis-customer-api` — **Simple**
- `complex-crud-ef-customer-api` / `complex-crud-ef-dapper-customer-api` / `complex-crud-mongodb-customer-api` — **Complex** (structure)

---

### 📨 Messaging & Message Broker (3 skills)

RabbitMQ integration, sagas, and reliable messaging patterns.

| Skill | Type | Focus | When to Use |
|-------|------|-------|-------------|
| **[messaging-architect](messaging/messaging-architect.md)** | 🎯 Architect | Broker pattern selection | Async communication, service integration |
| **[rabbitmq-advanced-specialist](messaging/rabbitmq-advanced-specialist.md)** | 🔧 Specialist | Typed consumers, request/response | Advanced RabbitMQ features, scheduling |
| **[saga-orchestration-specialist](messaging/saga-orchestration-specialist.md)** | 🔧 Specialist | Distributed transactions, compensation | Multi-step workflows, eventual consistency |

**Key Samples** (MCP Tier):
- `simple-rabbitmq-customer-api` — **Simple**
- `complex-event-driven-rabbitmq-customer-api` — **Blueprint** (not Complex N-Layers)
- `complex-saga-rabbitmq-customer-api` — **Capability**

---

### 🔗 System Integration (1 skill)

Sync vs async, outbound HTTP, webhooks, anti-corruption, idempotency, BFF.

| Skill | Type | Focus | When to Use |
|-------|------|-------|-------------|
| **[integration-architect](integration/integration-architect.md)** | 🎯 Architect | How this app talks to other systems | Partner APIs, webhooks, sync vs fila, ACL, BFF |

**Key Samples** (MCP Tier):
- `simple-crud-ef-customer-api` — **Simple** (typed outbound HTTP)
- `simple-rabbitmq-customer-api` — **Simple** (async after commit)
- `complex-event-driven-rabbitmq-customer-api` — **Blueprint** (integration events as architecture)
- `complex-hexagonal-customer-api` — **Blueprint** (many adapters)

**Consult after this skill**: `webapi-architect` (host), `messaging-architect` (broker), `resilience-patterns-specialist` (policies)

---

### ⚡ CQRS & Mediator (3 skills)

Command Query Responsibility Segregation and mediator patterns.

| Skill | Type | Focus | When to Use |
|-------|------|-------|-------------|
| **[cqrs-architect](cqrs/cqrs-architect.md)** | 🎯 Architect | CQRS pattern design | Read/write split, complex request pipelines |
| **[event-sourcing-specialist](cqrs/event-sourcing-specialist.md)** | 🔧 Specialist | Event store, projections, snapshots | Audit trail, temporal queries, event replay |
| **[mediator-patterns-specialist](cqrs/mediator-patterns-specialist.md)** | 🔧 Specialist | Commands, queries, behaviors | Mvp24Hours mediator implementation |

**Key Samples** (MCP Tier):
- `complex-cqrs-ef-customer-api` — **Blueprint**
- `complex-event-sourcing-customer-api` — **Capability**

**Critical**: Use `AddMvpMediator()` and `IMediatorCommand<T>`/`IMediatorQuery<T>`, **not MediatR**

---

### 📊 Observability & Resilience (2 skills)

OpenTelemetry tracing, metrics, logs, and resilience patterns.

| Skill | Type | Focus | When to Use |
|-------|------|-------|-------------|
| **[observability-architect](observability/observability-architect.md)** | 🎯 Architect | Telemetry stack design | Production monitoring, debugging distributed systems |
| **[resilience-patterns-specialist](observability/resilience-patterns-specialist.md)** | 🔧 Specialist | Circuit breaker, retry, timeout | External dependencies, unstable services |

**Key Samples** (MCP Tier):
- `simple-observability-customer-api` — **Simple** (no Minimal observability sample)

**Packages**:
- OpenTelemetry (traces, metrics, logs)
- Native .NET resilience (`Microsoft.Extensions.Resilience`)

---

### 🔄 Pipeline (Pipes & Filters) (1 skill)

Operation flows, validation, sagas, checkpoints.

| Skill | Type | Focus | When to Use |
|-------|------|-------|-------------|
| **[pipeline-architect](pipeline/pipeline-architect.md)** | 🎯 Architect | Pipe & filter workflows | Complex business processes, rollback needs |

**Key Samples** (MCP Tier):
- `minimal-pipeline-customer-api` — **Minimal**
- `simple-pipeline-customer-api` — **Simple**
- `complex-pipeline-builder-customer-api` / `complex-pipeline-customer-api` — **Complex**

**Features**: Typed pipelines, fork/join, saga orchestration, checkpoints, dependency graphs

---

### 🗃️ Caching (1 skill)

HybridCache, Redis, compression, invalidation strategies.

| Skill | Type | Focus | When to Use |
|-------|------|-------|-------------|
| **[caching-architect](caching/caching-architect.md)** | 🎯 Architect | Caching strategy design | Performance optimization, read-heavy workloads |

**Key Samples** (MCP Tier):
- `simple-hybridcache-rate-limit-api` — **Simple** (no Minimal HybridCache sample)

**Features**: L1/L2 tiers, stampede protection, tags, warming, compression

---

### 🔧 Infrastructure (1 skill)

Cross-cutting infrastructure: email, SMS, file storage, secrets, locks, background jobs.

| Skill | Type | Focus | When to Use |
|-------|------|-------|-------------|
| **[infrastructure-architect](infrastructure/infrastructure-architect.md)** | 🎯 Architect | Infrastructure service integration | Email/SMS delivery, file storage, distributed locks |

**Modules**: Email (SMTP, SendGrid), SMS (Twilio), File Storage (Azure Blob, AWS S3), Distributed Locking, Background Jobs

---

### 🌐 Web API (2 skills)

HTTP composition root on **Minimal, Simple, and Complex** hosts (not structure Minimal only). ASP.NET Minimal APIs vs controllers is a separate choice. Public **contract** (OpenAPI, versions, Problem Details) is a second skill.

| Skill | Type | Focus | When to Use |
|-------|------|-------|-------------|
| **[webapi-architect](webapi/webapi-architect.md)** | 🎯 Architect | HTTP host, OpenAPI registration, Problem Details pipeline | Any `*-api` host |
| **[api-contract-architect](webapi/api-contract-architect.md)** | 🎯 Architect | Consumer-facing contract, versioning, error envelope | OpenAPI-first, breaking changes, smoke `/openapi/v1.json` |

**Key Samples** (MCP Tier):
- `minimal-crud-ef-customer-api` — **Minimal** (one host, Map*)
- `simple-crud-ef-customer-api` — **Simple** (`{Product}.WebAPI`, controllers)
- `complex-crud-ef-customer-api` — **Complex** (modular WebAPI host, controllers)
- `complex-cqrs-ef-customer-api` — **Blueprint** (`MapNativeCommand`); `complex-keycloak-customer-api` — **Capability**

**Features**: `AddMvp24HoursWebEssential`, Native OpenAPI (`AddMvp24HoursNativeOpenApi()`), Problem Details, TypedResults and/or controllers

---

### 🧪 Testing (1 skill)

Unit, integration, fakes, test harnesses, Testcontainers.

| Skill | Type | Focus | When to Use |
|-------|------|-------|-------------|
| **[testing-architect](testing/testing-architect.md)** | 🎯 Architect | Testing strategy design | Test pyramid, integration testing, test doubles |

**Features**: Mvp24Hours test fakes, `WebApplicationFactory`, Testcontainers, RabbitMQ test harness

---

### 🔐 Identity & Security (2 skills)

Keycloak/JWT versus application security (secrets, headers, PII, encryption).

| Skill | Type | Focus | When to Use |
|-------|------|-------|-------------|
| **[identity-architect](identity/identity-architect.md)** | 🎯 Architect | Identity provider integration | Authentication, authorization, Keycloak |
| **[security-architect](security/security-architect.md)** | 🎯 Architect | Secrets, headers, rate limits, masking, field encryption | AppSec / PII technical controls; not IdP setup |

**Key Samples** (MCP Tier):
- `complex-keycloak-customer-api` — **Capability** (prefix `complex-` is not structure Complex)

---

### ⏰ CronJob (1 skill)

Scheduled tasks, hosted services, overlap prevention.

| Skill | Type | Focus | When to Use |
|-------|------|-------|-------------|
| **[cronjob-architect](cronjob/cronjob-architect.md)** | 🎯 Architect | Background job scheduling | Periodic tasks, cron-based workers |

**Key Samples** (MCP Tier):
- `simple-cronjob-worker` — **Simple** (no Minimal CronJob sample)

---

### 🚀 Modernization & Transformation (5 skills)

Native .NET 10 APIs **and** a pipeline for existing systems: analyze → propose → port (foreign stack) or rewrite (Mvp24Hours template-to-template).

| Skill | Type | Focus | When to Use |
|-------|------|-------|-------------|
| **[architecture-analyst](modernization/architecture-analyst.md)** | 🎯 Architect | Discovery report / compliance review | Analyze legacy or existing Mvp24Hours apps (no implementation) |
| **[architecture-proposal-architect](modernization/architecture-proposal-architect.md)** | 🎯 Architect | ADR, structure then blueprint | Target template, tree, phases, next skill |
| **[port-transpilation-specialist](modernization/port-transpilation-specialist.md)** | 🔧 Specialist | Semantic port to Mvp24Hours | Any language → .NET 10 (`port-to-mvp24hours`) |
| **[architecture-rewrite-architect](modernization/architecture-rewrite-architect.md)** | 🎯 Architect | Template-to-template rewrite | Already on Mvp24Hours (`architecture-migration`) |
| **[dotnet-modernization-specialist](modernization/dotnet-modernization-specialist.md)** | 🔧 Specialist | Native .NET 10 APIs | HybridCache, TimeProvider, OpenAPI, package 9→10 |

**MCP**: `get_discovery_playbook`, `resolve_architecture`, `plan_architecture_migration`, `get_migration_playbook`, `run_compliance_check`. **Transpilation** = semantic port, not a compiler.

---

## Two axes: structure vs blueprint vs capability

`list_samples` returns an official **Tier**. Do not infer it from the sample id prefix.

| Axis | Meaning | Templates / examples |
|------|---------|----------------------|
| **Structure — Minimal** | One host, feature folders | `minimal-api` · `minimal-crud-ef-customer-api` |
| **Structure — Simple** | Core + Application + Infrastructure + WebAPI | `simple-nlayers` · `simple-crud-ef-customer-api` |
| **Structure — Complex** | Modular monolith; Application must not reference Infrastructure | `complex-nlayers` · `complex-crud-ef-customer-api` |
| **Blueprint** | Pattern (CQRS, DDD, Hexagonal, Clean, Event-Driven, Microservices) | `complex-cqrs-ef-customer-api` is **Blueprint**, not Complex N-Layers |
| **Capability** | Feature sample (event sourcing, saga, Keycloak) | `complex-saga-rabbitmq-customer-api` is **Capability** |

Choose structure first (`@demand-architect` from a US, otherwise `@solution-architect`), then a blueprint only when it solves a concrete problem.

## Decision Matrix: Which Skill Should I Consult?

### I am not sure which skill...

```
→ @skill-router (handoff or one clarifying question)
```

### I'm starting a new project...

```
Do you have a US / RFC / demanda text?

├─ Yes — analyze first
│  → @demand-architect (structure + blueprint? + resource BOM)
│  → then @solution-architect / specialists to implement
│
└─ Constraints already clear
   ├─ Small CRUD API, fast delivery
   │  → @solution-architect (recommends Minimal API)
   │
   ├─ Standard business app
   │  → @solution-architect (recommends Simple N-Layers)
   │  → @data-architect (choose EF Core vs MongoDB)
   │
   ├─ Complex domain with business rules
   │  → @solution-architect (recommends DDD or CQRS)
   │  → @ddd-specialist (rich domain modeling)
   │
   ├─ Microservices architecture
   │  → @microservices-specialist
   │  → @messaging-architect (service communication)
   │  → @observability-architect (distributed tracing)
   │
   └─ Event-driven integration
      → @event-driven-specialist
      → @messaging-architect
      → @saga-orchestration-specialist (workflows)
```

### I have an existing project...

```
What do you need help with?

├─ Adding CQRS to existing app
│  → @cqrs-architect
│  → @mediator-patterns-specialist
│
├─ Improving performance
│  → @caching-architect (caching strategy)
│  → @data-architect (query optimization)
│  → @dapper-specialist (EF + SQL reads)
│
├─ Adding messaging/RabbitMQ
│  → @integration-architect (sync vs async first)
│  → @messaging-architect
│  → @rabbitmq-advanced-specialist
│
├─ Partner HTTP, webhooks, BFF, ACL
│  → @integration-architect
│  → @webapi-architect (inbound host)
│  → @hexagonal-specialist (many adapters)
│
├─ Implementing observability
│  → @observability-architect
│  → @resilience-patterns-specialist
│
├─ Adding tests
│  → @testing-architect
│
├─ Analyzing a demanda / US / RFC (existing app or new feature)
│  → @demand-architect (BOM; do not re-pick structure without evidence)
│
├─ Analyzing a legacy or existing system
│  → @architecture-analyst (report only)
│  → @architecture-proposal-architect (ADR)
│
├─ Porting non-Mvp24Hours code (any language)
│  → @port-transpilation-specialist
│
├─ Rewriting between Mvp24Hours templates
│  → @architecture-rewrite-architect
│  → @solution-architect (structure vs blueprint vocabulary)
│
├─ Native APIs / net10 package bump (same template)
│  → @dotnet-modernization-specialist
│
└─ Migrating architecture (legacy wording)
   → same as rewrite / proposal pipeline above
```

### I need implementation guidance...

| Need | Primary Skill | Supporting Skills |
|------|---------------|-------------------|
| **Unsure which skill / ambiguous start** | `@skill-router` | Then the handed-off skill |
| **US / RFC → architecture + packages** | `@demand-architect` | `@solution-architect` |
| **Partner APIs / webhooks / sync vs async** | `@integration-architect` | `@webapi-architect`, `@messaging-architect` |
| **EF Core repositories** | `@efcore-specialist` | `@data-architect` |
| **Dapper hybrid SQL reads** | `@dapper-specialist` | `@data-architect`, `@efcore-specialist` |
| **MongoDB document store** | `@mongodb-specialist` | `@data-architect` |
| **RabbitMQ typed consumers** | `@rabbitmq-advanced-specialist` | `@messaging-architect` |
| **CQRS handlers** | `@mediator-patterns-specialist` | `@cqrs-architect` |
| **Domain aggregates** | `@ddd-specialist` | `@solution-architect` |
| **OpenTelemetry traces** | `@observability-architect` | - |
| **Circuit breakers** | `@resilience-patterns-specialist` | `@observability-architect` |
| **Pipeline workflows** | `@pipeline-architect` | - |
| **HybridCache** | `@caching-architect` | `@dotnet-modernization-specialist` |
| **Keycloak JWT auth** | `@identity-architect` | `@webapi-architect` |
| **OpenAPI / API versioning / Problem Details contract** | `@api-contract-architect` | `@webapi-architect` |
| **Secrets, headers, PII technical controls** | `@security-architect` | `@identity-architect` |
| **Cron jobs** | `@cronjob-architect` | - |
| **.NET 10 features** | `@dotnet-modernization-specialist` | Relevant architect |
| **Legacy analysis** | `@architecture-analyst` | `@architecture-proposal-architect` |
| **Port / transpile to Mvp24Hours** | `@port-transpilation-specialist` | `@architecture-analyst`, `@webapi-architect` |
| **Template rewrite** | `@architecture-rewrite-architect` | `@solution-architect` |

---

## MCP Integration

### Prerequisites

All skills require the **Mvp24Hours MCP DevKit** to be configured. The MCP server provides:

- 📚 **Documentation resources**: `mvp24hours://docs/{path}`
- 🏗️ **Architecture templates**: `mvp24hours://templates/{id}`
- 📦 **Sample projects**: `mvp24hours://samples/{id}/readme`
- 🎯 **Scenarios and playbooks**: `mvp24hours://scenarios`

### MCP Tools Used by Skills

| Tool | Purpose | Example |
|------|---------|---------|
| `search_docs` | Find relevant documentation | `search_docs "query": "cqrs handlers"` |
| `get_doc` | Retrieve specific doc page | `get_doc "path": "docs/en-us/cqrs/commands.md"` |
| `get_architecture_template` | Get template details | `get_architecture_template "templateId": "ddd"` |
| `list_samples` | List available samples | `list_samples` |
| `get_sample_tree` | Browse sample structure | `get_sample_tree "sampleId": "complex-cqrs-ef-customer-api"` |
| `get_sample_file` | Read sample file | `get_sample_file "sampleId": "...", "filePath": "..."` |
| `resolve_architecture` | Match constraints to pattern | `resolve_architecture "situation": "..."` |
| `plan_architecture_migration` | Layer diff between templates | `plan_architecture_migration "sourceTemplateId": "simple-nlayers" "targetTemplateId": "complex-nlayers"` |
| `get_migration_playbook` | Structured migration playbook | `get_migration_playbook "playbookId": "crud-to-cqrs"` |
| `get_discovery_playbook` | Language-agnostic port discovery | `get_discovery_playbook` |
| `get_scenario_playbook` | Get implementation playbook | `get_scenario_playbook "scenarioId": "port-to-mvp24hours"` |

### Setting Up MCP

**Cursor**:
```json
// .cursor/mcp.json
{
  "servers": {
    "mvp24hours": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "<path-to-mcp>/Mvp24Hours.Mcp.csproj"],
      "env": { "MVP24HOURS_REPO_ROOT": "<path-to-mvp24hours-dotnet-repo>" }
    }
  }
}
```

**VS Code**:
```json
// .vscode/mcp.json  (same format as above)
```

**Environment Variable**:
```bash
# Windows
set MVP24HOURS_REPO_ROOT=C:\Dev\Github\mvp24hours\mvp24hours-dotnet

# Linux/Mac
export MVP24HOURS_REPO_ROOT=/path/to/mvp24hours-dotnet
```

---

## Skill File Structure

Each skill follows this layered structure (~300-500 lines):

```markdown
# [Skill Name] - Mvp24Hours [Architect|Specialist]

> **Role**: [Mission statement]  
> **MCP Integration**: [MCP usage guidance]

## Role & Expertise
[Core mission and responsibilities]

## Core Competencies
[Key areas of expertise]

## Decision Framework
### When to Use [This Pattern]
[Decision tree with clear criteria]

### vs Alternative Approaches
[Comparison with trade-offs]

## Architecture Patterns
[Pattern descriptions with MCP references]

## Implementation Guide
[Step-by-step with Mvp24Hours APIs]
[Code examples using Mvp24Hours packages]

## Anti-Patterns & Pitfalls
[Common mistakes and correct approaches]

## Migration Paths
[Progressive complexity paths with MCP tools]

## Integration Scenarios
[How this specialty integrates with others]

## Testing Strategy
[Testing approaches specific to this specialty]

## Best Practices Checklist
- [ ] Practice 1
- [ ] Practice 2

## MCP Workflow Examples
[Concrete MCP query examples]

## Further Resources
[MCP resources, related docs, specialist skills]
```

---

## Capability Matrix

| Capability | Architect Skill | Specialist Skills | Key Packages |
|------------|----------------|-------------------|--------------|
| **Orchestration** | skill-router | — | — |
| **Architecture Patterns** | demand-architect, solution-architect | clean-architecture, ddd, hexagonal, event-driven, microservices | Mvp24Hours.Core |
| **Data Access** | data-architect | efcore, dapper, mongodb, redis | Mvp24Hours.Infrastructure.Data.EFCore, .MongoDb, .Caching.Redis |
| **Messaging** | messaging-architect | rabbitmq-advanced, saga-orchestration | Mvp24Hours.Infrastructure.RabbitMQ |
| **System Integration** | integration-architect | hexagonal (ACL), resilience-patterns | Mvp24Hours.Infrastructure, .RabbitMQ, .Cqrs, .WebAPI |
| **CQRS** | cqrs-architect | event-sourcing, mediator-patterns | Mvp24Hours.Infrastructure.Cqrs |
| **Observability** | observability-architect | resilience-patterns | Mvp24Hours.Core (built-in telemetry) |
| **Pipeline** | pipeline-architect | - | Mvp24Hours.Infrastructure.Pipe |
| **Caching** | caching-architect | - | Mvp24Hours.Infrastructure.Caching, .Caching.Redis |
| **Infrastructure** | infrastructure-architect | - | Mvp24Hours.Infrastructure |
| **Web API** | webapi-architect, api-contract-architect | - | Mvp24Hours.WebAPI |
| **Testing** | testing-architect | - | Mvp24Hours.Infrastructure (test helpers) |
| **Identity** | identity-architect | - | Mvp24Hours.Infrastructure.Identity.Keycloak |
| **Application security** | security-architect | - | Mvp24Hours.Infrastructure, .WebAPI, EF encryption |
| **CronJob** | cronjob-architect | - | Mvp24Hours.Infrastructure.CronJob |
| **Modernization** | architecture-analyst, architecture-proposal-architect, architecture-rewrite-architect | port-transpilation, dotnet-modernization | All packages (.NET 10) |

---

## Package Reference

### Core Packages
```bash
dotnet add package Mvp24Hours.Core
dotnet add package Mvp24Hours.Application
dotnet add package Mvp24Hours.Infrastructure
dotnet add package Mvp24Hours.WebAPI
```

### Data & Persistence
```bash
dotnet add package Mvp24Hours.Infrastructure.Data.EFCore
dotnet add package Mvp24Hours.Infrastructure.Data.MongoDb
dotnet add package Mvp24Hours.Infrastructure.Caching
dotnet add package Mvp24Hours.Infrastructure.Caching.Redis
```

### CQRS & Messaging
```bash
dotnet add package Mvp24Hours.Infrastructure.Cqrs
dotnet add package Mvp24Hours.Infrastructure.RabbitMQ
```

### Infrastructure Services
```bash
dotnet add package Mvp24Hours.Infrastructure.Pipe
dotnet add package Mvp24Hours.Infrastructure.CronJob
dotnet add package Mvp24Hours.Infrastructure.Identity.Keycloak
```

**NuGet Feed**: https://www.nuget.org/packages?q=Mvp24Hours

---

## Contributing

### Completed Skills

All **36 catalog skills** are complete (orchestrator 1, architecture 7, data 5, messaging 3, CQRS 3, observability 2, plus pipeline, caching, infrastructure, webapi 2, testing, identity, security, cronjob, integration, modernization 5). The orchestrator does not follow [SKILL_TEMPLATE.md](SKILL_TEMPLATE.md).

See [COMPLETION_STATUS.md](COMPLETION_STATUS.md). To revise a skill, follow [SKILL_TEMPLATE.md](SKILL_TEMPLATE.md) and verify APIs with MCP (`get_doc`, `find_source_symbol`, `get_sample_tree`).

### Template for New Skills

See any of the completed skills for the full structure. Key requirements:

1. **MCP-First Approach**: Every section references MCP resources
2. **Decision Frameworks**: Clear "when to use" criteria
3. **Implementation Guide**: Step-by-step with Mvp24Hours APIs
4. **Anti-Patterns**: Common mistakes + correct approaches
5. **Migration Paths**: Simple → complex with MCP tools
6. **Integration Scenarios**: How it works with other specialties
7. **Testing Strategy**: Specific to the pattern
8. **MCP Workflow Examples**: Concrete query examples
9. **Sample References**: Link to relevant samples via MCP
10. **300-500 lines**: Comprehensive but focused

---

## External Resources

### Official Documentation
- **Website**: https://kallebelins.github.io/mvp24hours-dotnet/#/
- **GitHub**: https://github.com/kallebelins/mvp24hours-dotnet
- **Samples**: `samples/README.md` in the repository
- **MCP Server**: `mcp/README.md` in the repository

### Community
- **Issues**: https://github.com/kallebelins/mvp24hours-dotnet/issues
- **Discussions**: https://github.com/kallebelins/mvp24hours-dotnet/discussions

### Version Information
- **Current Source**: .NET 10 (`net10.0`)
- **Package Version**: 9.1.21 (stable), 10.8.0 (source, verify before using)
- **Migration Guide**: `docs/en-us/migration.md`

---

## License

These skills are provided as-is to assist with Mvp24Hours development. They reference the open-source Mvp24Hours library (MIT License).

**Mvp24Hours Library**: https://github.com/kallebelins/mvp24hours-dotnet (MIT License)

---

## Quick Reference

### Most Common Workflows

**New Project**:
```
0. @skill-router - If the path is unclear
1. @demand-architect - Analyze US/RFC; structure + resource BOM
2. @solution-architect - Deepen architecture pattern
3. @data-architect - Choose persistence strategy
4. @webapi-architect - Design HTTP endpoints
5. @testing-architect - Define test strategy
```

**Add Feature**:
```
1. @demand-architect - Map US to capabilities (keep current structure)
2. Identify category (CQRS, Messaging, Pipeline, etc.)
3. Consult relevant architect for pattern selection
4. Consult specialist for deep implementation
5. @testing-architect for test approach
```

**Architecture Migration**:
```
1. @architecture-analyst — inventory or compliance review
2. @architecture-proposal-architect — structure first, optional blueprint
3. @port-transpilation-specialist (external) OR @architecture-rewrite-architect (already Mvp24Hours)
4. @dotnet-modernization-specialist if only native APIs / package bump
5. run_compliance_check for the matching scenario
```

**Production Readiness**:
```
1. @observability-architect - Add telemetry
2. @resilience-patterns-specialist - Add resilience policies
3. @testing-architect - Verify test coverage
4. @infrastructure-architect - Configure infrastructure services
```

---

**Remember**: These skills are designed to work together. If the specialty is unclear, start with `@skill-router`. Otherwise start with broad architects for pattern selection, then dive into specialists for implementation details. Always use MCP tools to access canonical Mvp24Hours documentation and samples.
