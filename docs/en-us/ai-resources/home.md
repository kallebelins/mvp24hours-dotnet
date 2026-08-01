# AI & MCP Resources

Mvp24Hours separates documentation by audience:

- Human architecture decisions live in [Architecture Guides](../guides/architecture/home.md).
- Packages, APIs, options, and DI registration live in canonical module documentation.
- Machine retrieval should use the **local MCP DevKit** when working in this repository.

## Local MCP DevKit

The [`mcp/`](../../mcp/) project ships a [Model Context Protocol](https://modelcontextprotocol.io/) server that indexes `docs/`, `samples/`, and `src/` without duplicating template content.

### Cursor setup

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

### Run manually

**Stdio (Cursor default):**

```bash
export MVP24HOURS_REPO_ROOT=/path/to/mvp24hours-dotnet   # Windows: set MVP24HOURS_REPO_ROOT=...
dotnet run --project mcp/src/Mvp24Hours.Mcp/Mvp24Hours.Mcp.csproj
```

**HTTP (stateless, for remote agents):**

```bash
dotnet run --project mcp/src/Mvp24Hours.Mcp/Mvp24Hours.Mcp.csproj -- --http --urls http://localhost:5199/mcp
```

### Machine-readable indexes

| Resource | Path |
| --- | --- |
| Architecture manifest | [`templates-manifest.json`](templates-manifest.json) |
| Layer templates | [`layers/`](layers/) |
| Compliance checklist | [`compliance-checklist.md`](compliance-checklist.md) |
| MCP server docs | [`mcp/README.md`](../../mcp/README.md) |

### MCP capabilities

- **Resources:** `mvp24hours://manifest`, `mvp24hours://docs/{path}`, `mvp24hours://templates/{id}`, `mvp24hours://layers/{name}`, `mvp24hours://samples/{id}/readme`
- **Tools:** doc search, sample catalog, architecture resolution, scaffolding hints, compliance checks, source symbol lookup
- **Prompts:** `new-mvp24hours-api`, `add-smoke-tests`, `review-mvp24hours-solution`

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
