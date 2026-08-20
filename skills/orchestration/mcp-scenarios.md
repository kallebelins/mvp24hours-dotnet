# MCP scenario heuristics (for skill-router)

Guess a candidate `scenarioId` from user language. Playbooks always come from MCP:

- `list_scenarios` — ids, titles, prompts, inputs
- `get_scenario_playbook` — steps and tools
- `resolve_feature` — capability docs, samples, compliance rules
- `mvp24hours://capabilities` — full capability index

Never trust hardcoded playbook details. After guessing, call `get_scenario_playbook`.

Consultation (no `scenarioId`) uses [skill-catalog.md](skill-catalog.md) and `@skill` handoff — do not invent a scenario id.

## Scenario intent signals

| Candidate scenarioId | PT signals | EN signals |
| --- | --- | --- |
| `greenfield-api` | criar API, nova API, do zero, scaffold, greenfield, projeto novo | new API, from scratch, scaffold, greenfield, bootstrap |
| `architecture-migration` | migrar arquitetura, mudar template, simple para complex, reorganizar camadas | migrate architecture, change template, restructure layers |
| `port-to-mvp24hours` | portar código, migrar de Java/Python, sistema externo, discovery | port code, external legacy, migrate from other stack, discovery |
| `add-feature` | adicionar feature, integrar, incluir capability, no meu projeto | add feature, integrate, in my project, add capability |
| `legacy-migration` | TelemetryHelper, MediatR, Swashbuckle, APIs legadas, modernizar | legacy APIs, modernize, replace Swashbuckle |
| `upgrade-net10` | upgrade .NET 10, atualizar SDK, net10, migrar pacotes | upgrade .NET 10, update SDK, net10 |
| `review-solution` | revisar, compliance, checklist, validar solução | review, compliance, checklist, audit |

## Standalone MCP prompt (no scenario in list_scenarios)

| Signals PT/EN | MCP prompt | When |
| --- | --- | --- |
| smoke test, WebApplicationFactory, OpenAPI test, integration tests | `add-smoke-tests` | Tests only, not full greenfield or review |

For broader test strategy, use `add-feature` and `resolve_feature` with keyword `testing`.

## Consultation (handoff, no scenario)

| Intent | Handoff |
| --- | --- |
| demand / US / RFC → architecture + BOM | `@demand-architect` then `resolve_architecture`, `resolve_feature`, `list_samples` |
| sync vs async, webhook, partner HTTP, BFF, ACL | `@integration-architect` |
| OpenAPI-first, versionamento, Problem Details | `@api-contract-architect` |
| secrets, LGPD/PII técnico, security headers, Key Vault | `@security-architect` |
| Dapper, SQL reads, hybrid EF | `@dapper-specialist` |
| which architecture / sample / template | `@solution-architect` plus `resolve_architecture`, `list_samples`, `get_architecture_template` |
| how does X work / API reference | `search_docs`, `get_doc`, `verify_doc_claim`, `find_source_symbol` (no skill required) |

Do not invent a new MCP `scenarioId` for a US/RFC proposal.

## Capability routing (add-feature)

Pass the named technology as `featureKeyword` to `resolve_feature`.

Examples: `cqrs`, `rabbitmq`, `keycloak`, `observability`, `saga`, `ddd`, `mongodb`, `redis`, `openapi`, `testing`, `http`, `dapper`

If no match: `search_docs` → `search_sample_patterns` → ask. MCP owns the capability index.

## Disambiguation

| Conflict | Resolution |
| --- | --- |
| Demand vs greenfield execute | US/RFC + BOM, no code → `@demand-architect`; "criar API" + implement now → `greenfield-api` |
| New vs existing | "criar API" → `greenfield-api`; "no meu projeto X" → `add-feature` or `review-solution` |
| External vs legacy Mvp24Hours | other stack → `port-to-mvp24hours`; already Mvp24Hours with obsolete APIs → `legacy-migration` |
| Architecture change vs feature add | template/layer restructure → `architecture-migration`; single capability → `add-feature` |
| Two plausible scenarios | Present both with one-line difference from `get_scenario_playbook`; ask **one** question |
| Skill vs playbook | Pattern advice → catalog handoff; generate/migrate the solution → MCP scenario |

## Example prompts → flow

| User prompt | Guess | Triage |
| --- | --- | --- |
| "Integração sync vs async / webhook / HttpClient" | `@integration-architect` | `get_doc` / `resolve_feature(rabbitmq)` |
| "Analise esta demanda e proponha arquitetura e recursos" | `@demand-architect` | `resolve_architecture` + `resolve_feature` + `list_samples` |
| "Quero criar uma API CRUD pequena com EF" | `greenfield-api` | `list_scenarios` → `get_scenario_playbook(greenfield-api)` |
| "Adicionar RabbitMQ no CustomerAPI" | `add-feature` | `get_scenario_playbook(add-feature)` → `resolve_feature(rabbitmq)` |
| "Sistema Java → Mvp24Hours" | `port-to-mvp24hours` | `get_scenario_playbook(port-to-mvp24hours)` |
| "Revisa se segue o checklist" | `review-solution` | `get_scenario_playbook(review-solution)` |
| "Como funciona AddMvpMediator?" | docs | `search_docs("AddMvpMediator")` → `verify_doc_claim` |
| "Adicionar smoke tests" | `add-smoke-tests` | MCP prompt `add-smoke-tests` |
