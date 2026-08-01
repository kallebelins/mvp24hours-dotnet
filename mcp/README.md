# Mvp24Hours MCP DevKit

Local [Model Context Protocol](https://modelcontextprotocol.io/) server for AI agents building with Mvp24Hours. Documentation and architecture templates live in `docs/`; runnable patterns live in `samples/`. This server **indexes and exposes** them — it does not duplicate template content.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Clone of `mvp24hours-dotnet` with `docs/en-us/ai-resources/templates-manifest.json`

## Cursor setup

Project-level configuration is in [`.cursor/mcp.json`](../.cursor/mcp.json). Restart Cursor after changes.

Environment variable:

| Variable | Purpose |
| --- | --- |
| `MVP24HOURS_REPO_ROOT` | Absolute path to repo root (set automatically in Cursor) |

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

## MCP Tools

| Category | Tools |
| --- | --- |
| Reference | `search_docs`, `get_doc`, `list_samples`, `get_sample_tree`, `get_sample_file` |
| Architecture | `resolve_architecture`, `get_architecture_template`, `list_layers` |
| Scaffolding | `suggest_project_structure`, `get_test_scaffold`, `get_readme_scaffold`, `get_di_registration_hints` |
| Validation | `find_source_symbol`, `find_tests_for_module`, `run_compliance_check`, `verify_doc_claim` |

## MCP Prompts

- `new-mvp24hours-api` — full scaffold workflow
- `add-smoke-tests` — CustomerApiFactory + OpenApiSmokeTests
- `review-mvp24hours-solution` — compliance review

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
- Compliance checklist: [`docs/en-us/ai-resources/compliance-checklist.md`](../docs/en-us/ai-resources/compliance-checklist.md)
- Sample test templates: [`samples/templates/`](../samples/templates/)
