# Agent Rules

Mvp24Hours ships one project-context file per AI coding tool under [`agent-rules/`](https://github.com/kallebelins/mvp24hours-dotnet/tree/main/agent-rules) in the repository — for **your own project that consumes Mvp24Hours via NuGet**, not for the Mvp24Hours repository itself. Every file tells the agent it is working in a consuming project (so it should not assume `docs/`, `mcp/`, or `src/Tests/` exist there), instructs it to call `@skill-router` for anything Mvp24Hours-related, lists the topics the 35 domain skills cover, and states a few accuracy rules. Formatting follows each tool's native rules convention. Download the file for your tool, copy it into **your own project** at the listed path, and rename it if the tool expects a fixed filename.

This is different from the [Skills Catalog](skills-catalog.md): agent rules give a tool persistent project context (always-on), while skills are the on-demand specialists that context routes to. **Agent rules assume the skills are also installed** in your project (e.g. `.cursor/skills/`, `.github/skills/`) — without them, `@skill-router` will not resolve, and the agent should fall back to general .NET/Mvp24Hours knowledge instead of guessing at unavailable tools.

## Install by tool

| Tool | Download | Copy to (in your project) |
|------|----------|----------------------------|
| Cursor | [`cursor.mdc`](https://raw.githubusercontent.com/kallebelins/mvp24hours-dotnet/main/agent-rules/cursor.mdc) | `.cursor/rules/mvp24hours.mdc` |
| Kiro | [`kiro.md`](https://raw.githubusercontent.com/kallebelins/mvp24hours-dotnet/main/agent-rules/kiro.md) | `.kiro/steering/mvp24hours.md` |
| VS Code / GitHub Copilot | [`vscode-copilot-instructions.md`](https://raw.githubusercontent.com/kallebelins/mvp24hours-dotnet/main/agent-rules/vscode-copilot-instructions.md) | `.github/copilot-instructions.md` |
| Claude Code | [`claude-code.md`](https://raw.githubusercontent.com/kallebelins/mvp24hours-dotnet/main/agent-rules/claude-code.md) | `CLAUDE.md` (project root) |
| Windsurf | [`windsurf.md`](https://raw.githubusercontent.com/kallebelins/mvp24hours-dotnet/main/agent-rules/windsurf.md) | `.windsurf/rules/mvp24hours.md` |
| Cline | [`cline.md`](https://raw.githubusercontent.com/kallebelins/mvp24hours-dotnet/main/agent-rules/cline.md) | `.clinerules/mvp24hours.md` |
| Continue | [`continue.md`](https://raw.githubusercontent.com/kallebelins/mvp24hours-dotnet/main/agent-rules/continue.md) | `.continue/rules/mvp24hours.md` |
| JetBrains AI Assistant | [`jetbrains-ai-assistant.md`](https://raw.githubusercontent.com/kallebelins/mvp24hours-dotnet/main/agent-rules/jetbrains-ai-assistant.md) | `.aiassistant/rules/mvp24hours.md` |

Or clone/download the whole [`agent-rules/`](https://github.com/kallebelins/mvp24hours-dotnet/tree/main/agent-rules) folder and pick the files you need.

## Quick setup

1. **Download** the file for your tool from the table above (right-click the link → "Save link as", or open it and use your browser's save action — the link points to the raw file content).
2. **Also copy the [`skills/`](https://github.com/kallebelins/mvp24hours-dotnet/tree/main/skills) folder** into your project (see the [Skills Catalog](skills-catalog.md) for the exact destination per tool). The rules file routes to `@skill-router`, which only exists if the skills are installed.
3. **Create the destination folder** in your project if it does not exist yet (e.g. `.kiro/steering/`, `.windsurf/rules/`).
4. **Copy the rules file** to the destination path listed in the table, renaming it if the tool expects a fixed filename (`CLAUDE.md`, `copilot-instructions.md`).
5. **Restart or reload** the tool/extension so it picks up the new rules file.

## Why one file per tool

Each tool parses its rules file differently — front-matter keys (`alwaysApply`, `inclusion`, `trigger`, `globs`), fixed filenames, and folder conventions are not interchangeable between tools. Dropping a `.mdc` file into `.clinerules/`, for example, is silently ignored by Cline. Downloading the file matching your tool avoids that failure mode.

## What the file contains

Every agent rules file covers the same four things:

- **Project framing** — this is a project that consumes Mvp24Hours via NuGet, not the Mvp24Hours repository; do not assume its internal folder layout exists here.
- **Start here** — call `@skill-router` first for anything Mvp24Hours-related; let it classify the request and hand off, rather than picking a specialist skill yourself.
- **What you can explore via skills** — the topics the 35 domain skills cover (architecture, data, messaging, CQRS, observability, pipeline, caching, infrastructure, web API, testing, identity/security, cron jobs, modernization), so the agent knows what to ask for.
- **Accuracy rules** — guardrails specific to Mvp24Hours (use the Mvp24Hours Mediator, not MediatR; verify the package versions actually referenced in the project before assuming specific APIs; Semantic Kernel and Microsoft Agent Framework are not Mvp24Hours modules).

## Related documentation

- [AI & MCP Resources — Overview](home.md) — MCP DevKit setup for Cursor and VS Code
- [Skills Catalog](skills-catalog.md) — 36 on-demand specialist skills for Cursor and VS Code Copilot
- [`agent-rules/README.md`](https://github.com/kallebelins/mvp24hours-dotnet/blob/main/agent-rules/README.md) on GitHub — source folder and sync notes
