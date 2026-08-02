# Mvp24Hours VS Code DevKit

Portable kit for [GitHub Copilot](https://code.visualstudio.com/docs/copilot/overview) in VS Code (v1.102+). Copy this folder's contents into any project to enable the **Mvp24Hours MCP** server and the **mvp24hours-router** Agent Skill.

## Requirements

- [VS Code 1.102+](https://code.visualstudio.com/) with GitHub Copilot
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Clone of [`mvp24hours-dotnet`](https://github.com/kallebelins/mvp24hours-dotnet) (for docs, samples, and MCP source)

## Install

1. Copy `.vscode/` and `.github/` from this folder into your project root (merge with existing folders if needed).
2. Choose the MCP configuration scenario below and adjust `.vscode/mcp.json` if required.
3. Reload VS Code or run **MCP: List Servers** from the Command Palette.
4. Start the `mvp24hours` server and switch Copilot Chat to **Agent** mode.

## Scenario A — mvp24hours-dotnet repo

Use when your workspace **is** the `mvp24hours-dotnet` repository root.

The default [`.vscode/mcp.json`](.vscode/mcp.json) is ready:

- `MVP24HOURS_REPO_ROOT` = `${workspaceFolder}`
- MCP project path = `mcp/src/Mvp24Hours.Mcp/Mvp24Hours.Mcp.csproj` (relative)

No changes needed.

## Scenario B — external consuming project

Use when your workspace is **another** solution (e.g. your own API) but you want Mvp24Hours MCP guidance.

### Option 1 — stdio with input prompt (recommended)

Replace `.vscode/mcp.json` with [`templates/mcp.external.json`](templates/mcp.external.json):

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

### Option 2 — HTTP (remote server)

1. Start the MCP server in your `mvp24hours-dotnet` clone:

   ```bash
   set MVP24HOURS_REPO_ROOT=c:\path\to\mvp24hours-dotnet
   dotnet run --project mcp/src/Mvp24Hours.Mcp/Mvp24Hours.Mcp.csproj -- --http --urls http://localhost:5199
   ```

2. Replace `.vscode/mcp.json` with [`templates/mcp.http.json`](templates/mcp.http.json):

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

## Agent Skill — mvp24hours-router

After copying, the skill lives at `.github/skills/mvp24hours-router/`.

It triages ambiguous Mvp24Hours prompts via MCP (`list_scenarios`, `get_scenario_playbook`, etc.) and presents the recommended path **before** executing workflows.

**Invoke manually:** type `/mvp24hours-router` in Copilot Chat (Agent mode).

**Auto-load:** Copilot loads the skill when your prompt matches its description (e.g. "por onde começo", "criar API", "migrar").

## Verify setup

1. Command Palette → **MCP: List Servers** → start `mvp24hours`
2. Confirm no manifest errors in the server output
3. Copilot Chat → **Agent** mode → `/mvp24hours-router` or ask "por onde começo com Mvp24Hours?"
4. Agent should call `list_scenarios` and present a route

## Sync with canonical source

When editing the skill in the `mvp24hours-dotnet` repository:

- **Canonical VS Code skill:** [`.github/skills/mvp24hours-router/`](../.github/skills/mvp24hours-router/)
- **This kit:** replicate changes here under `.github/skills/mvp24hours-router/`

## Cursor equivalent

For Cursor IDE, use [`cursor-devkit/`](../cursor-devkit/) (`.cursor/mcp.json` + `.cursor/skills/`) instead.

## Troubleshooting

| Issue | Fix |
| --- | --- |
| MCP tools not available | Switch Copilot Chat to **Agent** mode |
| Missing manifest | Set `MVP24HOURS_REPO_ROOT` to the clone root (folder with `docs/` and `samples/`) |
| Server fails to start | Verify .NET 10 SDK and the path to `Mvp24Hours.Mcp.csproj` |
| Skill not listed | Confirm `.github/skills/mvp24hours-router/SKILL.md` exists; `name` must match folder name |

See also [`mcp/README.md`](../mcp/README.md) for full MCP documentation.
