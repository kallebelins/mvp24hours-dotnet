# Mvp24Hours MCP DevKit

Local [Model Context Protocol](https://modelcontextprotocol.io/) server for AI agents building with Mvp24Hours. Documentation and architecture templates live in `docs/`; runnable patterns live in `samples/`. This server **indexes and exposes** them — it does not duplicate template content.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Clone of `mvp24hours-dotnet` with `docs/en-us/ai-resources/templates-manifest.json`

**Agent entry point:** [`skills/orchestration/skill-router.md`](../skills/orchestration/skill-router.md) (`@skill-router`) — catalog handoff and MCP playbook triage — or any domain skill by name. The [global installer](../scripts/README-devkit.md) registers 36 `SKILL.md` folders and keeps domain copies under `skill-router/catalog/` for router handoff.

MCP JSON templates: [`templates/vscode/`](templates/vscode/) and [`templates/cursor/`](templates/cursor/). Human-facing copy: [`docs/en-us/ai-resources/home.md`](../docs/en-us/ai-resources/home.md).

## Cursor setup

Project-level configuration is in [`.cursor/mcp.json`](../.cursor/mcp.json). Restart Cursor after changes.

**This repo:** `mcpServers` + `MVP24HOURS_REPO_ROOT` = `${workspaceFolder}`. **External project:** [`templates/cursor/mcp.external.json`](templates/cursor/mcp.external.json) (`MVP24HOURS_MCP_REPO_ROOT`) or HTTP [`templates/cursor/mcp.http.json`](templates/cursor/mcp.http.json).

## VS Code setup

Project-level MCP configuration is in [`.vscode/mcp.json`](../.vscode/mcp.json). Use **Agent mode** in Copilot Chat.

```json
{
  "servers": {
    "mvp24hours": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "mcp/src/Mvp24Hours.Mcp/Mvp24Hours.Mcp.csproj"],
      "env": { "MVP24HOURS_REPO_ROOT": "${workspaceFolder}" }
    }
  }
}
```

Restart or run **MCP: List Servers** after changing MCP configuration.

**External project — stdio with input prompt:** [`templates/vscode/mcp.external.json`](templates/vscode/mcp.external.json)

```json
{
  "inputs": [
    {
      "type": "promptString",
      "id": "mvp24hours-repo-root",
      "description": "Absolute path to your mvp24hours-dotnet clone"
    }
  ],
  "servers": {
    "mvp24hours": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "${input:mvp24hours-repo-root}/mcp/src/Mvp24Hours.Mcp/Mvp24Hours.Mcp.csproj"
      ],
      "env": {
        "MVP24HOURS_REPO_ROOT": "${input:mvp24hours-repo-root}"
      }
    }
  }
}
```

**External project — HTTP:** start the server with `-- --http --urls http://localhost:5199`, then [`templates/vscode/mcp.http.json`](templates/vscode/mcp.http.json):

```json
{
  "servers": {
    "mvp24hours": {
      "type": "http",
      "url": "http://localhost:5199"
    }
  }
}
```

Environment variable:

| Variable | Purpose |
| --- | --- |
| `MVP24HOURS_REPO_ROOT` | Absolute path to repo root (set automatically in Cursor/VS Code) |

## Run locally

**Stdio (Cursor default):**

```bash
set MVP24HOURS_REPO_ROOT=c:\Dev\Github\mvp24hours\mvp24hours-dotnet
dotnet run --project mcp/src/Mvp24Hours.Mcp/Mvp24Hours.Mcp.csproj
```

**HTTP (stateless, for remote agents):**

```bash
set MVP24HOURS_REPO_ROOT=c:\Dev\Github\mvp24hours\mvp24hours-dotnet
dotnet run --project mcp/src/Mvp24Hours.Mcp/Mvp24Hours.Mcp.csproj -- --http --urls http://localhost:5199
```

## MCP Resources

| URI | Content |
| --- | --- |
| `mvp24hours://manifest` | `templates-manifest.json` |
| `mvp24hours://docs/{path}` | Markdown under `docs/en-us/` |
| `mvp24hours://templates/{id}` | Architecture template doc |
| `mvp24hours://layers/{name}` | Layer template (`core`, `application`, …) |
| `mvp24hours://samples/{id}/readme` | Sample README |
| `mvp24hours://scenarios` | Development scenarios manifest |
| `mvp24hours://capabilities` | Feature/capability index |
| `mvp24hours://migration/{id}` | Migration playbook |
| `mvp24hours://discovery` | Language-agnostic port/discovery playbook |

## MCP Tools

| Category | Tools |
| --- | --- |
| Reference | `search_docs`, `get_doc`, `list_samples`, `get_sample_tree`, `get_sample_file` |
| Architecture | `resolve_architecture`, `get_architecture_template`, `list_layers` |
| Scaffolding | `suggest_project_structure`, `get_test_scaffold`, `get_readme_scaffold`, `get_di_registration_hints` |
| Validation | `find_source_symbol`, `find_tests_for_module`, `run_compliance_check`, `verify_doc_claim` |
| Scenarios | `list_scenarios`, `get_scenario_playbook`, `get_discovery_playbook`, `resolve_feature`, `plan_architecture_migration`, `get_migration_playbook`, `search_sample_patterns` |

## MCP Prompts

- `new-mvp24hours-api` — full scaffold workflow
- `add-smoke-tests` — CustomerApiFactory + OpenApiSmokeTests
- `review-mvp24hours-solution` — compliance review
- `migrate-architecture` — migrate between architecture templates
- `port-to-mvp24hours` — port external code via discovery (any language)
- `add-mvp24hours-feature` — add capability to existing solution
- `migrate-legacy-mvp24hours` — legacy APIs to native .NET
- `upgrade-net10-package` — SDK/package upgrade to .NET 10

## Build and test

```bash
dotnet build mcp/Mvp24Hours.Mcp.slnx --configuration Release
dotnet test mcp/Mvp24Hours.Mcp.slnx --configuration Release
```

## Troubleshooting (Windows)

If tools report missing manifest:

1. Set `MVP24HOURS_REPO_ROOT` to the repo root (folder containing `docs/` and `samples/`).
2. Verify `docs/en-us/ai-resources/templates-manifest.json` exists.
3. Run from any directory once the env var is set — the server does not depend on cwd.

## Canonical sources

- Architecture manifest: [`docs/en-us/ai-resources/templates-manifest.json`](../docs/en-us/ai-resources/templates-manifest.json)
- Scenarios manifest: [`docs/en-us/ai-resources/scenarios-manifest.json`](../docs/en-us/ai-resources/scenarios-manifest.json)
- Capabilities manifest: [`docs/en-us/ai-resources/capabilities-manifest.json`](../docs/en-us/ai-resources/capabilities-manifest.json)
- Migration playbooks: [`docs/en-us/ai-resources/migration-playbooks.json`](../docs/en-us/ai-resources/migration-playbooks.json)
- Discovery playbook: [`docs/en-us/ai-resources/discovery-playbook.md`](../docs/en-us/ai-resources/discovery-playbook.md)
- Compliance checklist: [`docs/en-us/ai-resources/compliance-checklist.md`](../docs/en-us/ai-resources/compliance-checklist.md)
- Sample test templates: [`samples/templates/`](../samples/templates/)
