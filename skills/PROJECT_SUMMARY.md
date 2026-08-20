# Mvp24Hours Skills Ecosystem - Project Summary

> **Status**: Catalog complete (36 named skills: 1 orchestrator + 35 domain)  
> **Completion**: api-contract, security, and dapper specialists added August 2026; demand intake and integration retained

## Objective

Portable MCP-first specialist skills for Mvp24Hours .NET 10, one markdown file per skill under `skills/`.

## Delivered

- **36 skill files** across 15 categories (see [README.md](README.md))
- Orchestration: `skill-router` (handoff to one domain skill; ask when ambiguous)
- Demand intake: `demand-architect` (US/RFC → structure + resource BOM)
- System integration: `integration-architect` (sync vs async, webhooks, ACL, BFF)
- HTTP contract: `api-contract-architect`; appsec: `security-architect`; SQL reads: `dapper-specialist`
- Documentation: README, SKILL_TEMPLATE, COMPLETION_STATUS, SKILLS_GENERATION_GUIDE, this summary
- Modernization: discovery, proposal, semantic port, template rewrite, plus native .NET 10 APIs

Phase 1 (`data-architect`, `efcore-specialist`, `cqrs-architect`, `messaging-architect`) existed before the remaining category skills were written.

## How to use

Copy `skills/` into a consuming project's `.cursor/skills/` (or VS Code equivalent) and configure Mvp24Hours MCP.

## Maintenance

Re-query MCP when library APIs change. Do not treat generation-guide code snippets as source of truth if they disagree with `get_doc` / `find_source_symbol`.

**Updated**: August 2026
