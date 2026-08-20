# AI & MCP Resources

Mvp24Hours separates documentation by audience:

- Human architecture decisions live in [Architecture Guides](../guides/architecture/home.md).
- Packages, APIs, options, and DI registration live in canonical module documentation.
- Machine retrieval should use the **local MCP DevKit** when working in this repository.

## Local MCP DevKit

The [`mcp/`](../../mcp/) project ships a [Model Context Protocol](https://modelcontextprotocol.io/) server that indexes `docs/`, `samples/`, and `src/` without duplicating template content.

**Agent entry point:** [`skills/orchestration/skill-router.md`](../../skills/orchestration/skill-router.md) (`@skill-router`) or any domain skill (`@efcore-specialist`, `@demand-architect`, …). The [global installer](../../scripts/README-devkit.md) registers all 36 skills as `SKILL.md` folders and keeps a `catalog/` copy inside `skill-router` for handoff.

### Cursor setup (this repo)

Project configuration: [`.cursor/mcp.json`](../../.cursor/mcp.json)

```json
{
  "mcpServers": {
    "mvp24hours": {
      "command": "dotnet",
      "args": ["run", "--project", "mcp/src/Mvp24Hours.Mcp/Mvp24Hours.Mcp.csproj"],
      "env": { "MVP24HOURS_REPO_ROOT": "${workspaceFolder}" }
    }
  }
}
```

Restart Cursor after changing MCP configuration.

**External Cursor project:** set user env `MVP24HOURS_MCP_REPO_ROOT` to the clone, then use [`mcp/templates/cursor/mcp.external.json`](../../mcp/templates/cursor/mcp.external.json) (`mcpServers`). HTTP: [`mcp/templates/cursor/mcp.http.json`](../../mcp/templates/cursor/mcp.http.json).

### VS Code setup (this repo)

Project configuration: [`.vscode/mcp.json`](../../.vscode/mcp.json)

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

Use **Agent mode** in Copilot Chat. Run **MCP: List Servers** after changing configuration.

### VS Code — external consuming project

Use when the workspace is **another** solution but you want Mvp24Hours MCP.

#### Option 1 — stdio with input prompt (recommended)

Replace `.vscode/mcp.json` with [`mcp/templates/vscode/mcp.external.json`](../../mcp/templates/vscode/mcp.external.json):

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

VS Code prompts for the clone path on first server start and stores the value.

#### Option 2 — HTTP (remote server)

1. Start the MCP server in your `mvp24hours-dotnet` clone:

   ```bash
   set MVP24HOURS_REPO_ROOT=c:\path\to\mvp24hours-dotnet
   dotnet run --project mcp/src/Mvp24Hours.Mcp/Mvp24Hours.Mcp.csproj -- --http --urls http://localhost:5199
   ```

2. Replace `.vscode/mcp.json` with [`mcp/templates/vscode/mcp.http.json`](../../mcp/templates/vscode/mcp.http.json):

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

### Run manually

**Stdio (Cursor default):**

```bash
export MVP24HOURS_REPO_ROOT=/path/to/mvp24hours-dotnet   # Windows: set MVP24HOURS_REPO_ROOT=...
dotnet run --project mcp/src/Mvp24Hours.Mcp/Mvp24Hours.Mcp.csproj
```

**HTTP (stateless, for remote agents):**

```bash
dotnet run --project mcp/src/Mvp24Hours.Mcp/Mvp24Hours.Mcp.csproj -- --http --urls http://localhost:5199
```

### Machine-readable indexes

| Resource | Path |
| --- | --- |
| Architecture manifest | [`templates-manifest.json`](templates-manifest.json) |
| Scenarios manifest | [`scenarios-manifest.json`](scenarios-manifest.json) |
| Capabilities manifest | [`capabilities-manifest.json`](capabilities-manifest.json) |
| Migration playbooks | [`migration-playbooks.json`](migration-playbooks.json) |
| Discovery playbook | [`discovery-playbook.md`](discovery-playbook.md) |
| Layer templates | [`layers/`](layers/) |
| Compliance checklist | [`compliance-checklist.md`](compliance-checklist.md) |
| MCP server docs | [`mcp/README.md`](../../mcp/README.md) |

### MCP capabilities

- **Resources:** `mvp24hours://manifest`, `mvp24hours://docs/{path}`, `mvp24hours://templates/{id}`, `mvp24hours://layers/{name}`, `mvp24hours://samples/{id}/readme`, `mvp24hours://scenarios`, `mvp24hours://capabilities`, `mvp24hours://migration/{id}`, `mvp24hours://discovery`
- **Tools:** doc search, sample catalog, architecture resolution, scaffolding hints, compliance checks, source symbol lookup, scenario playbooks, feature resolution, migration planning, sample pattern search
- **Prompts:** `new-mvp24hours-api`, `add-smoke-tests`, `review-mvp24hours-solution`, `migrate-architecture`, `port-to-mvp24hours`, `add-mvp24hours-feature`, `migrate-legacy-mvp24hours`, `upgrade-net10-package`

## Current machine context (fallback)

- [Compact LLM context](../../llms_compact_en.txt)
- [Complete LLM context](../../llms_complete_en.txt)
- [Cursor rule](../../mvp24hours.mdc)

These files are compatibility resources. Canonical human documentation wins if generated context conflicts with source or tests.

## Retrieval order for agents

1. Use the **local Mvp24Hours MCP** when configured (stdio or HTTP).
2. Use canonical module documentation and Architecture Guides.
3. Use the compatibility downloads above only as an offline fallback.
4. Verify exact APIs against `src/` and behavior against `src/Tests/`.

## External AI frameworks

Semantic Kernel and Microsoft Agent Framework are not Mvp24Hours product modules. Their former templates are no longer first-class library documentation.

Semantic Kernel Graph documentation lives at [skgraph.dev](https://skgraph.dev/) and the docs repository [semantic-kernel-graph-docs](https://github.com/kallebelins/semantic-kernel-graph-docs).
