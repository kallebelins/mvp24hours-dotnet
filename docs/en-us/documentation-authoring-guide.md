# Documentation Authoring Guide

Status: **Accepted for documentation v1**

Use these rules when adding or substantially revising English Mvp24Hours documentation. The [Documentation Scope and Information Architecture](documentation-ia-policy.md) defines where content belongs; this guide defines how to write and verify it.

**Authority:** when prose disagrees with implementation, `src/**` and `src/Tests/**` win.

## Evidence first

Treat the repository as the specification, in this order:

1. Public source under `src/Mvp24Hours.*/**`.
2. Behavior demonstrated under `src/Tests/**`.
3. Testing helpers under `src/Mvp24Hours.*/Testing/**`.
4. Existing documentation and release notes.

Do not infer APIs from roadmap entries, historical examples, or package names.

Before documenting a feature:

- Locate the public type or extension method in source.
- Locate tests that demonstrate defaults, validation, registration, and behavior.
- Confirm the package containing the API.
- Check whether the API is obsolete, experimental, or version-specific.
- Cite the matching test project or test files in the **Testing** section, and keep a short **Implementation evidence** note under Related links.

Example:

```markdown
## Testing

Behavior and defaults are covered by:

- `src/Tests/Mvp24Hours.Infrastructure.Caching.Test/` — cache options and DI registration

## Related links

- [Caching overview](caching-advanced.md)

### Implementation evidence

- Source: `src/Mvp24Hours.Infrastructure.Caching/`
- Tests: `src/Tests/Mvp24Hours.Infrastructure.Caching.Test/`
```

Repository paths are evidence for maintainers; user instructions must still be understandable without opening the repository.

### Primary test map

| Area | Test project |
|------|----------------|
| Core / Aspire / Options validation | `Mvp24Hours.Core.Test` |
| Application services | `Mvp24Hours.Application.Test`, `*.Integration.Test`, `*.SQLServer/PostgreSql/MySql.Test` |
| CQRS | `Mvp24Hours.Infrastructure.Cqrs.Test` |
| EF Core | `Mvp24Hours.Infrastructure.Data.EFCore.Test` |
| MongoDB | `Mvp24Hours.Infrastructure.Data.MongoDb.Test`, `Mvp24Hours.Application.MongoDb.Test` |
| RabbitMQ | `Mvp24Hours.Application.RabbitMQ.Test` |
| Pipe | `Mvp24Hours.Application.Pipe.Test` |
| Caching / Redis | `Mvp24Hours.Infrastructure.Caching.Test`, `Mvp24Hours.Application.Redis.Test` |
| CronJob | `Mvp24Hours.Infrastructure.CronJob.Test` |
| WebAPI | `Mvp24Hours.WebAPI.Test` |
| Infrastructure cross-cutting | `Mvp24Hours.Infrastructure.Test` |
| HTTP patterns | `Mvp24Hours.Patterns.Test` |

Prefer `*OptionsTest.cs`, `*Extensions*Test*.cs`, and integration tests over inventing scenarios.

## Language and platform baseline

- Write user-facing prose in English under `docs/en-us/**`.
- Do not add or sync `docs/pt-br/**` in this initiative.
- Target `net10.0` and C# 14 in new or updated C# examples.
- Use APIs present in the repository. Never invent names, overloads, configuration sections, defaults, or behavior.
- Do not document roadmap features absent from `src/`.
- Do not treat Semantic Kernel, Semantic Kernel Graph, or Agent Framework as Mvp24Hours product features.
- Do not retain stale `9.*` package pins. Prefer `dotnet add package Package.Name` unless a verified version is necessary; if a version is shown, use the verified current release.
- Explain version-specific or breaking behavior explicitly and link to Release & Migration.

## Canonical ownership and duplication

| Content type | Owner | Must contain | Must not contain |
|--------------|-------|--------------|------------------|
| Module pages | Product module docs | API signatures, DI, Options, defaults, health/observability hooks, testing facts | Architecture trade-off essays |
| Architecture Guides | `guides/architecture/**` | Why/when, project structures, blueprints, decision guidance | Full Options tables or complete configuration matrices |
| Release & Migration | `release.md`, `migration.md`, modernization migration pages | Version history, breaking changes, upgrade steps | Full module configuration reference |
| AI & MCP Resources | External MCP + `llms_*`, Cursor rule | Structured retrieval, agent discovery | Human-first tutorials replacing module docs |

Architecture Guides must link to module docs rather than repeat complete Options tables or setup references. Pattern index pages should summarize and route readers to canonical pages, not become another source of API truth.

## Canonical page structure

New module pages and substantial rewrites follow this order where applicable:

1. Overview
2. Install
3. DI registration
4. Options
5. Examples
6. Health and observability
7. Testing
8. Related links

Omit sections that genuinely do not apply. Do not add empty placeholders. Every substantively rewritten module page that exposes public `*Options` or `AddMvp*` / `AddMvp24Hours*` extensions must include DI registration, Options, and Testing.

### Overview

Explain the problem the module solves, when to use it, and its important boundaries. Name the owning package.

### Install

Show the minimum package command:

```bash
dotnet add package Mvp24Hours.Package.Name
```

List additional provider packages separately and explain when each is required.

### DI registration

Always show the primary registration extension and a configuration lambda when the API supports one. Prefer `Program.cs` + `builder.Services`. Avoid new `Startup.cs` examples.

```csharp
// Program.cs
builder.Services.AddMvp24HoursFeature(options =>
{
    options.Enabled = true;
});
```

The type and member names must match source (`AddMvpMediator`, `AddMvp24HoursDbContext`, and similar). If registration is available only through `IConfiguration`, only through a lambda, or through multiple provider-specific extensions, state that accurately. Include required `using` namespaces when they are not obvious.

### Options

Every public Options type touched by the page requires a property table with exactly these columns:

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Enabled` | `bool` | `true` | Enables the feature. |

Rules:

- Use the exact public property name from source for **Name**.
- Use the CLR type for **Type**, including nullable and generic forms.
- Copy **Default** from initializers, constructors, constants, presets, or `*OptionsTest` files. Use `null`, `false`, `0`, `TimeSpan.Zero`, `[]`, `{}`, or “No default” precisely.
- Mark `required` members and validation constraints.
- Prefer the source XML summary for **Description** when it is accurate.
- Document nested Options types in their own tables under a nested heading.
- Document public presets such as `Production()` or `Development()` and describe which values they change.
- Distinguish library defaults from recommended production values.
- Normalize legacy first-column labels such as `Property` or `Option` to **Name** when a page is touched.

### Configuration binding

When an Options type is bindable from `IConfiguration`, show both the DI binding and a validated `appsettings.json` section. Derive the section path from source (`SectionName` constants, `.Bind(...)`, `GetSection(...)`, or binder extensions). Do not invent section names.

```csharp
services
    .AddOptions<FeatureOptions>()
    .BindConfiguration("Feature")
    .ValidateOnStart();
```

```json
{
  "Feature": {
    "Enabled": true
  }
}
```

Known bindable sections observed in source include `CronJobs:Global`, `CronJobs:{JobTypeName}`, `InboxOutbox`, `Infrastructure`, `Mvp24Hours:Observability`, `Mvp24Hours:Logging`, and `Mvp24Hours:OpenTelemetry:Logging`. Verify each section against the Options type you are documenting before copying it. If only code configuration is supported, omit JSON and say so when readers might reasonably expect binding.

### Examples

- Start with the smallest complete usage.
- Add advanced examples only when verified by source and tests.
- Keep unrelated application code out of the example.
- Do not present hypothetical code as a working Mvp24Hours API. Label conceptual pseudocode explicitly.
- Prefer library conventions from source: `IEntityBase<TKey>`, `IRepositoryAsync<T>`, `IBusinessResult<T>`, `IMediatorCommand<T>`, and the actual `AddMvp*` / `AddMvp24Hours*` extensions.

### Health and observability

Document actual health check registrations, tags, failure status, timeouts, metrics, logging, tracing, and activity sources exposed by the module. Link to the canonical Observability & Resilience pages instead of copying their full setup.

### Testing

Show the supported fake, in-memory provider, test harness, fixture, or assertion helper when one exists. Identify whether the example is a unit or integration test and link to the canonical Testing guide. Do not recommend mocks that bypass a provided testing abstraction without explaining why.

## Deprecation and compatibility

Mark deprecated APIs clearly before legacy content, and include the replacement plus a migration link:

```markdown
> **DEPRECATED:** This API is deprecated and will be removed in a future version.
>
> Migrate to the replacement documented in the [Migration Guide](observability/migration.md).
```

Also:

- Do not recommend obsolete APIs in new examples.
- Label legacy sections `(Legacy - Deprecated)` when retained temporarily.
- Mention `[Obsolete]` when present in source.
- When moving a page, retain a short compatibility stub at the old URL for at least one major release.
- A compatibility stub states the new destination and contains no second copy of the reference content.

```markdown
# Page moved

This guide is now maintained at [Architecture Guides](guides/architecture/home.md).
```

## Links and Docsify

Docsify runs with `relativePath: true` in `docs/index.html`.

| Context | Pattern | Example |
|---------|---------|---------|
| Same folder | bare filename | `[IA Policy](documentation-ia-policy.md)` |
| Cross-folder | relative path | `[Observability](observability/home.md)` |
| English sidebar | leading slash + locale | `[Home](/en-us/home.md)` |
| Root sidebar | no leading slash | `[Home](en-us/home.md)` |
| Section anchor | hash/id fragment | `[SQL Server](database/relational?id=sql-server)` |

Rules:

- Prefer relative in-page links over absolute `/en-us/...` paths.
- Do not introduce machine-specific absolute paths such as `D:\Github\...`.
- Use Docsify heading fragments only after verifying the generated anchor.
- Use descriptive link text.
- Keep old URLs valid with compatibility stubs when files move.
- Update both `_sidebar.md` files together when navigation changes, preserving their established locale-relative forms until task 1.8 standardizes them.

## Code, packages, and formatting

- Add a language identifier to every fenced code block: `csharp`, `json`, `bash`, `xml`, `yaml`, or another accurate identifier.
- Prefer compilable C# over fragments. Use comments or ellipses only where omission is clear.
- Use backticks for types, members, packages, configuration keys, and command names.
- Match C# naming and casing from source.
- Do not include secrets, live credentials, production endpoints, or unsafe certificate-validation examples.
- Use headings in a consistent hierarchy without skipping levels.
- Prefer short lists and focused tables over duplicated prose.

| Rule | Detail |
|------|--------|
| TFM in prose | `net10.0` |
| Install examples | unpinned `dotnet add package` unless version matters |
| Version pins | only when showing a breaking migration; align with `CHANGELOG.md` and the verified published package |
| Microsoft dependencies | align with repo CPM in `src/Directory.Packages.props` |
| Forbidden | `Version="9.*"`, machine-local paths, invented section names |

## Review checklist

Before marking a documentation task complete, verify:

- [ ] Prose is English and examples target `net10.0`.
- [ ] Matching test project was read and cited in Testing.
- [ ] Every named API exists in public source.
- [ ] Behavior and defaults are supported by relevant tests.
- [ ] Package and namespace ownership are correct.
- [ ] DI registration uses an existing extension and overload, preferably with a lambda.
- [ ] Every touched Options type has a complete `Name | Type | Default | Description` table.
- [ ] Presets, nested Options, required members, and validation are documented.
- [ ] `appsettings.json` is included only when binding and section names are verified.
- [ ] Deprecated APIs are labeled and replacements are linked.
- [ ] Module facts are not duplicated into Architecture Guides.
- [ ] Relative links and heading anchors resolve in Docsify.
- [ ] Moved URLs have compatibility stubs.
- [ ] Code fences have language identifiers.
- [ ] No stale `9.*` package pins or machine-specific paths remain.
- [ ] Related links include implementation evidence from source and tests.

## Related links

- [Documentation Scope and Information Architecture](documentation-ia-policy.md)
- [Microsoft Docs style guide](https://learn.microsoft.com/en-us/style-guide/welcome/)
- [Docsify configuration](https://docsify.js.org/#/configuration)
