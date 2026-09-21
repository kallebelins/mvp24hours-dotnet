# Agent rules

Portable Mvp24Hours project context, one file per AI coding tool, for **your own project that consumes Mvp24Hours via NuGet** — not for this repository. Every file carries the same content: it tells the agent this is a consuming project (not the Mvp24Hours repo itself), instructs it to call `@skill-router` for anything Mvp24Hours-related, lists the topics the 35 domain skills cover, and states a few accuracy rules. Formatting follows each tool's native rules convention. Pick the file for your tool, copy it into your **own** project at the listed path, and rename it if the tool expects a fixed filename.

These files assume the [Mvp24Hours skills](https://github.com/kallebelins/mvp24hours-dotnet/tree/main/skills) are also installed in your project (e.g. `.cursor/skills/`, `.github/skills/`). Without the skills installed, `@skill-router` will not resolve — the agent should fall back to general .NET/Mvp24Hours knowledge instead.

For the published, downloadable version of this table (with direct links), see [AI & MCP Resources → Agent Rules](https://kallebelins.github.io/mvp24hours-dotnet/#/en-us/ai-resources/agent-rules.md) in the documentation site.

| Tool | File | Copy to (in your project) |
|------|------|----------------------------|
| Cursor | [`cursor.mdc`](cursor.mdc) | `.cursor/rules/mvp24hours.mdc` |
| Kiro | [`kiro.md`](kiro.md) | `.kiro/steering/mvp24hours.md` |
| VS Code / GitHub Copilot | [`vscode-copilot-instructions.md`](vscode-copilot-instructions.md) | `.github/copilot-instructions.md` |
| Claude Code | [`claude-code.md`](claude-code.md) | `CLAUDE.md` (project root) |
| Windsurf | [`windsurf.md`](windsurf.md) | `.windsurf/rules/mvp24hours.md` |
| Cline | [`cline.md`](cline.md) | `.clinerules/mvp24hours.md` |
| Continue | [`continue.md`](continue.md) | `.continue/rules/mvp24hours.md` |
| JetBrains AI Assistant | [`jetbrains-ai-assistant.md`](jetbrains-ai-assistant.md) | `.aiassistant/rules/mvp24hours.md` |

## Why one file per tool instead of one shared file

Each tool parses its rules file differently — front-matter keys (`alwaysApply`, `inclusion`, `trigger`, `globs`), fixed filenames (`CLAUDE.md`, `copilot-instructions.md`), and folder conventions are not interchangeable. Copying the wrong format silently disables the rule (e.g. a `.mdc` file dropped into `.clinerules/` is not parsed by Cline). Keeping one file per tool avoids that failure mode.

## Keeping content in sync

The body content (everything after the front-matter) must stay identical across all 8 files. When updating the skill-router guidance, the skill topic list, or the accuracy rules, edit every file in this folder — there is no build step that generates them from a single source.

## Related

- [AI & MCP Resources](https://kallebelins.github.io/mvp24hours-dotnet/#/en-us/ai-resources/home.md) — MCP DevKit setup
- [Skills Catalog](https://kallebelins.github.io/mvp24hours-dotnet/#/en-us/ai-resources/skills-catalog.md) — 36 portable agent skills for Cursor and VS Code Copilot
