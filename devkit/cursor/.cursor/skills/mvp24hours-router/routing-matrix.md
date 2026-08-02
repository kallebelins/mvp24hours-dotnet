# Intent Heuristics (MCP-backed)

This file helps **guess** a candidate `scenarioId` from user language. It does **not** define playbooks — those always come from MCP:

- `list_scenarios` — ids, titles, prompts, inputs
- `get_scenario_playbook` — steps and tools
- `resolve_feature` — capability docs, samples, compliance rules
- `mvp24hours://capabilities` — full capability index (resource)

Copy this skill folder to any project with Mvp24Hours MCP configured. No repo paths required.

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

After guessing, call `get_scenario_playbook` to load title, inputs, and steps from MCP.

## Standalone MCP prompt (no scenario in list_scenarios)

| Signals PT/EN | MCP prompt | When |
| --- | --- | --- |
| smoke test, WebApplicationFactory, OpenAPI test, integration tests | `add-smoke-tests` | User wants tests only, not full greenfield or review |

For broader test strategy, route to `add-feature` and call `resolve_feature` with keyword `testing`.

## Consultation routes (no scenario)

| Intent | MCP tools |
| --- | --- |
| which architecture / which sample / which template | `resolve_architecture`, `list_samples`, `get_architecture_template` |
| how does X work / documentation / API reference | `search_docs`, `get_doc`, `verify_doc_claim`, `find_source_symbol` |

Use `search_docs` with query `decision matrix` or `architecture` when user asks which template to pick.

## Capability routing (add-feature)

When user names a technology or feature, pass it as `featureKeyword` to `resolve_feature`:

Examples: `cqrs`, `rabbitmq`, `keycloak`, `observability`, `saga`, `ddd`, `mongodb`, `redis`, `openapi`, `testing`

If `resolve_feature` returns no match:

1. `search_docs` with the user's term
2. `search_sample_patterns` with a symbol or API name
3. Ask the user to clarify the capability

Do not maintain a static capability table — MCP owns the index.

## Disambiguation

| Conflict | Resolution |
| --- | --- |
| New vs existing | "criar API" → `greenfield-api`; "no meu projeto X" → `add-feature` or `review-solution` |
| External vs legacy Mvp24Hours | other stack → `port-to-mvp24hours`; already Mvp24Hours with obsolete APIs → `legacy-migration` |
| Architecture change vs feature add | template/layer restructure → `architecture-migration`; single capability → `add-feature` |
| Two plausible scenarios | Present both with one-line difference from `get_scenario_playbook`; ask **one** question |

## Example prompts → MCP flow

Portuguese user prompts below are intentional routing examples.

| User prompt | Guess | Triage MCP calls |
| --- | --- | --- |
| "Quero criar uma API CRUD pequena com EF" | `greenfield-api` | `list_scenarios` → `get_scenario_playbook(greenfield-api)` |
| "Adicionar RabbitMQ no CustomerAPI" | `add-feature` | `get_scenario_playbook(add-feature)` → `resolve_feature(rabbitmq)` |
| "Sistema Java → Mvp24Hours" | `port-to-mvp24hours` | `get_scenario_playbook(port-to-mvp24hours)` |
| "Revisa se segue o checklist" | `review-solution` | `get_scenario_playbook(review-solution)` |
| "Como funciona AddMvpMediator?" | consultation | `search_docs("AddMvpMediator")` → `verify_doc_claim` |
| "Adicionar smoke tests" | `add-smoke-tests` | MCP prompt `add-smoke-tests` (no scenario) |
