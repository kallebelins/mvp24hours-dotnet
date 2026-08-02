---
name: mvp24hours-router
description: >-
  Detects user intent in Mvp24Hours prompts (.NET, CQRS, samples) and recommends
  the correct scenario, MCP prompt, and next steps via the Mvp24Hours MCP DevKit.
  Use when the request is ambiguous, when starting Mvp24Hours work, or when the
  user asks "where do I start", "which path", "por onde começo", "qual caminho",
  "criar API", "migrar", "port code", "add feature", "review solution",
  "upgrade .NET 10", or mentions MCP/scenarios/playbooks.
---

# Mvp24Hours Router

**Route-only triage** skill that is **portable**: works in any project with **Mvp24Hours MCP** configured. It does not depend on local paths, manifests, or docs — every canonical route comes from MCP tools and resources.

Cursor equivalent: [`devkit/cursor/`](../../../devkit/cursor/)

## Prerequisite

- **GitHub Copilot** with **Agent mode** enabled in Copilot Chat (MCP tools only run in Agent mode).
- The **Mvp24Hours MCP** server must be available (stdio or HTTP). If it is not configured, tell the user and stop — do not invent scenarios or paths.

Discover the server by the name configured in `.vscode/mcp.json` (common: `mvp24hours`). Check with **MCP: List Servers** (Command Palette) or invoke MCP tools directly in Agent mode.

## When to apply

- User starts Mvp24Hours work without a clear scenario
- Request is ambiguous (create vs migrate vs add feature vs docs lookup)
- User asks "where do I start", "which path", "por onde começo", "qual caminho"
- User mentions MCP, scenarios, playbooks, or DevKit workflows

Do **not** apply when the user already named a scenario and asked to execute it.

## Workflow

```mermaid
flowchart TD
    start[Receive prompt] --> mcpList["MCP list_scenarios"]
    mcpList --> classify[Classify intent by signals]
    classify --> playbook["MCP get_scenario_playbook"]
    playbook --> enrich{Needs enrichment?}
    enrich -->|Feature| resolveFeat["MCP resolve_feature"]
    enrich -->|Architecture| resolveArch["MCP resolve_architecture"]
    enrich -->|Docs| searchDocs["MCP search_docs"]
    enrich -->|No| present[Present route]
    resolveFeat --> present
    resolveArch --> present
    searchDocs --> present
    present --> wait[Wait for confirmation]
    wait -->|Confirmed| execute[Execute MCP playbook]
    wait -->|Adjust| classify
```

### Step 1 — Bootstrap from MCP (always first)

1. `list_scenarios` — official scenario ids, titles, prompts, inputs
2. Match user intent to a candidate `scenarioId` (heuristics in [routing-matrix.md](routing-matrix.md))
3. `get_scenario_playbook` with candidate `scenarioId` — steps, tools, required inputs

Optional enrichment during triage:

- Feature in existing solution → `resolve_feature` with `featureKeyword`
- Architecture/sample choice → `resolve_architecture`, `list_samples`
- Docs/API question → `search_docs`, `get_doc`, `verify_doc_claim`
- Full manifests → resource `mvp24hours://scenarios` or `mvp24hours://capabilities`

### Step 2 — Intent heuristics (guess only)

Use the table below to pick a **candidate** `scenarioId`. Always confirm against `list_scenarios` + `get_scenario_playbook` — never trust hardcoded details.

| Signals | Candidate scenarioId | MCP prompt (from playbook) |
| --- | --- | --- |
| new API, from scratch, scaffold, greenfield | `greenfield-api` | from playbook |
| change architecture, template migration | `architecture-migration` | from playbook |
| port external code, other stack, discovery | `port-to-mvp24hours` | from playbook |
| add capability to existing solution | `add-feature` | from playbook |
| TelemetryHelper, MediatR, Swashbuckle, legacy APIs | `legacy-migration` | from playbook |
| upgrade SDK/packages, net10 | `upgrade-net10` | from playbook |
| review, compliance, checklist | `review-solution` | from playbook |
| smoke tests, WebApplicationFactory only | *(no scenario)* | MCP prompt `add-smoke-tests` |
| which architecture / which sample | *(consultation)* | `resolve_architecture`, `list_samples` |
| API/doc question | *(consultation)* | `search_docs`, `get_doc`, `verify_doc_claim` |

Disambiguation rules: [routing-matrix.md](routing-matrix.md)

### Step 3 — Present route (stop here)

Use playbook data returned by MCP — do not copy steps from memory:

```markdown
## Recommended route

**Detected intent:** [one-sentence summary]
**Scenario:** [scenarioId] — [Title from get_scenario_playbook]
**MCP prompt:** [Prompt from get_scenario_playbook, or prompt name for standalone routes]

### Next steps (after your confirmation)
1. [step.title from playbook — tool: step.tool]
2. ...

### Inputs I need from you
- [inputs from playbook]

Should I follow this path? (or tell me what to adjust)
```

For consultation-only routes, omit the scenario line and list MCP tools to call.

### Step 4 — Execute (after explicit confirmation only)

Follow steps from `get_scenario_playbook` or invoke the matching MCP prompt workflow. Use `verify_doc_claim` and `run_compliance_check` as the playbook specifies.

## Route-only constraints

**Do not call** until the user confirms:

- `suggest_project_structure`, `get_test_scaffold`, `run_compliance_check`
- Any file creation or code changes tied to a playbook

**May call** during triage (read-only):

- `list_scenarios`, `get_scenario_playbook`, `resolve_feature`, `resolve_architecture`
- `search_docs`, `list_samples`, `get_doc`, `find_source_symbol`
- Resources: `mvp24hours://scenarios`, `mvp24hours://capabilities`, `mvp24hours://discovery`

## MCP unavailable

If MCP tools fail or the server is not configured:

1. Tell the user the Mvp24Hours MCP DevKit is required for routing
2. Do not guess scenarios, templates, or doc paths
3. Do not reference repository files the consuming project may not have

## Anti-patterns

- Do not hardcode playbook steps — always fetch via `get_scenario_playbook`
- Do not hardcode capability lists — use `resolve_feature` or resource `mvp24hours://capabilities`
- Do not suggest MediatR, Swashbuckle, TelemetryHelper, or MultiLevelCache
- Do not execute playbook tools before user confirms the route
