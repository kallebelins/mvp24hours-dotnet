# Mvp24Hours Cursor DevKit

Portable kit for [Cursor](https://cursor.com/). Copy this folder's contents into any project to enable the **Mvp24Hours MCP** server and the **mvp24hours-router** Agent Skill.

## Requirements

- [Cursor](https://cursor.com/) with MCP support
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Clone of [`mvp24hours-dotnet`](https://github.com/kallebelins/mvp24hours-dotnet) (for docs, samples, and MCP source)

## Install

1. Copy `.cursor/` from this folder into your project root (merge with existing folders if needed).
2. Choose the MCP configuration scenario below and adjust `.cursor/mcp.json` if required.
3. Restart Cursor or reload the window after changes.
4. Confirm the `mvp24hours` server appears under **Settings → Tools & MCP**.

## Scenario A — mvp24hours-dotnet repo

Use when your workspace **is** the `mvp24hours-dotnet` repository root.

The default [`.cursor/mcp.json`](.cursor/mcp.json) is ready:

- `MVP24HOURS_REPO_ROOT` = `${workspaceFolder}`
- MCP project path = `mcp/src/Mvp24Hours.Mcp/Mvp24Hours.Mcp.csproj` (relative)

No changes needed.

## Scenario B — external consuming project

Use when your workspace is **another** solution (e.g. your own API) but you want Mvp24Hours MCP guidance.

### Option 1 — stdio with environment variable (recommended)

1. Set a machine-level environment variable pointing to your clone:

   ```powershell
   # Windows (PowerShell — persistent for your user)
   [Environment]::SetEnvironmentVariable("MVP24HOURS_MCP_REPO_ROOT", "C:\path\to\mvp24hours-dotnet", "User")
   ```

   ```bash
   # macOS / Linux
   export MVP24HOURS_MCP_REPO_ROOT=/path/to/mvp24hours-dotnet
   ```

2. Replace `.cursor/mcp.json` with [`templates/mcp.external.json`](templates/mcp.external.json):

   ```json
   {
     "mcpServers": {
       "mvp24hours": {
         "command": "dotnet",
         "args": [
           "run",
           "--project",
           "${env:MVP24HOURS_MCP_REPO_ROOT}/mcp/src/Mvp24Hours.Mcp/Mvp24Hours.Mcp.csproj"
         ],
         "env": {
           "MVP24HOURS_REPO_ROOT": "${env:MVP24HOURS_MCP_REPO_ROOT}"
         }
       }
     }
   }
   ```

3. Restart Cursor so the environment variable is picked up.

### Option 2 — stdio with absolute paths

Edit `.cursor/mcp.json` directly and replace both paths with your clone location:

```json
{
  "mcpServers": {
    "mvp24hours": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:/path/to/mvp24hours-dotnet/mcp/src/Mvp24Hours.Mcp/Mvp24Hours.Mcp.csproj"
      ],
      "env": {
        "MVP24HOURS_REPO_ROOT": "C:/path/to/mvp24hours-dotnet"
      }
    }
  }
}
```

Use forward slashes on Windows for consistency with Cursor variable resolution.

### Option 3 — HTTP (remote server)

1. Start the MCP server in your `mvp24hours-dotnet` clone:

   ```bash
   set MVP24HOURS_REPO_ROOT=c:\path\to\mvp24hours-dotnet
   dotnet run --project mcp/src/Mvp24Hours.Mcp/Mvp24Hours.Mcp.csproj -- --http --urls http://localhost:5199
   ```

2. Replace `.cursor/mcp.json` with [`templates/mcp.http.json`](templates/mcp.http.json):

   ```json
   {
     "mcpServers": {
       "mvp24hours": {
         "url": "http://localhost:5199"
       }
     }
   }
   ```

## Agent Skill — mvp24hours-router

After copying, the skill lives at `.cursor/skills/mvp24hours-router/`.

It triages ambiguous Mvp24Hours prompts via MCP (`list_scenarios`, `get_scenario_playbook`, etc.) and presents the recommended path **before** executing workflows.

**Auto-load:** Cursor loads the skill when your prompt matches its description (e.g. "where do I start", "create API", "migrate"; Portuguese prompts such as "por onde começo" and "criar API" also match).

## Verify setup

1. **Settings → Tools & MCP** → confirm `mvp24hours` is listed and enabled
2. Open MCP server output — no manifest errors
3. Ask in Agent chat: "where do I start with Mvp24Hours?"
4. Agent should call `list_scenarios` and present a route

## Sync with canonical source

When editing the skill in the `mvp24hours-dotnet` repository:

- **Canonical Cursor skill:** [`.cursor/skills/mvp24hours-router/`](../.cursor/skills/mvp24hours-router/)
- **This kit:** replicate changes here under `.cursor/skills/mvp24hours-router/`

## VS Code equivalent

For VS Code with GitHub Copilot, use [`vscode-devkit/`](../vscode-devkit/) instead (`.vscode/mcp.json` + `.github/skills/`).

## Troubleshooting

| Issue | Fix |
| --- | --- |
| Server not listed | Confirm `.cursor/mcp.json` uses root key `mcpServers` (not `servers`) |
| Missing manifest | Set `MVP24HOURS_REPO_ROOT` to the clone root (folder with `docs/` and `samples/`) |
| Server fails to start | Verify .NET 10 SDK and the path to `Mvp24Hours.Mcp.csproj` |
| Skill not applied | Confirm `.cursor/skills/mvp24hours-router/SKILL.md` exists; `name` must match folder name |
| Env var not resolved | Restart Cursor after setting `MVP24HOURS_MCP_REPO_ROOT` |

See also [`mcp/README.md`](../mcp/README.md) for full MCP documentation.
