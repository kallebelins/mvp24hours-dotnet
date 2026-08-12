# Mvp24Hours architecture templates

Compilable scaffolding for .NET 10. Copy a folder, rename `App` / `Item`, and implement your domain.

**Samples teach** full scenarios with a Customer API. **Templates bootstrap** a new solution with a placeholder `Item` resource.

## How to use

1. Copy the template folder out of this repository (or clone and start from it).
2. Rename projects and namespaces: `App` → your service name (for example `Orders`).
3. Rename the placeholder: `Item` → your aggregate/entity.
4. The template automatically uses local `src/` projects inside this monorepo and published NuGet packages when copied elsewhere.

If you need to force NuGet mode while still inside the monorepo, you can override the default:

```bash
dotnet build -p:Mvp24HoursUseProjectReferences=false -p:Mvp24HoursPackageVersion=10.8.0
```

## Automated rename script

Use the PowerShell script to rename folder names, file names, and text content recursively in the copied template.

Script path:

- `scripts/Replace-TextEverywhere.ps1`

Recommended flow:

1. Copy one template folder to your new repository.
2. Run a dry-run with `-WhatIf` to preview all changes.
3. Run again without `-WhatIf` to apply changes.

Dry-run example:

```powershell
pwsh ./scripts/Replace-TextEverywhere.ps1 \
	-DestinationPath "./my-new-service" \
	-SearchText "App" \
	-ReplaceText "Orders" \
	-WhatIf
```

Apply changes:

```powershell
pwsh ./scripts/Replace-TextEverywhere.ps1 \
	-DestinationPath "./my-new-service" \
	-SearchText "App" \
	-ReplaceText "Orders"
```

Notes:

- Search is case-insensitive.
- Replacement preserves common case patterns (APP, App, app).
- The script processes the destination folder and all subfolders/files.
- Common generated folders are ignored (`bin`, `obj`, `.git`, `.vs`, `node_modules`).

## Rename checklist

- Solution and project file names (`App.*.csproj`)
- Root namespaces and folder names
- Assembly / OpenAPI titles
- `Item` entity, DTOs, ports, handlers, and controller routes
- Connection string keys and health-check names

## Catalog

### Blueprints (architecture shapes)

| Template | Shape | Teaching sample |
| --- | --- | --- |
| [blueprints/complex-nlayers](blueprints/complex-nlayers) | Complex N-Layers (Facade + services) | `samples/src/complex-crud-ef-customer-api` |
| [blueprints/clean-architecture](blueprints/clean-architecture) | Clean Architecture + Mediator | `samples/src/complex-clean-architecture-customer-api` |
| [blueprints/hexagonal](blueprints/hexagonal) | Ports and adapters | `samples/src/complex-hexagonal-customer-api` |
| [blueprints/cqrs](blueprints/cqrs) | CQRS commands/queries | `samples/src/complex-cqrs-ef-customer-api` |
| [blueprints/ddd](blueprints/ddd) | Rich aggregate + domain events | `samples/src/complex-ddd-ef-customer-api` |
| [blueprints/event-driven](blueprints/event-driven) | Integration events (in-memory outbox) | `samples/src/complex-event-driven-rabbitmq-customer-api` |

### Hosts (deployment entry points)

| Template | Host | Notes |
| --- | --- | --- |
| [hosts/api-complex-nlayers](hosts/api-complex-nlayers) | ASP.NET Core API | Points at `blueprints/complex-nlayers` |
| [hosts/bff-complex-nlayers](hosts/bff-complex-nlayers) | BFF API | Aggregation-friendly; no required DbContext |
| [hosts/function-minimal](hosts/function-minimal) | Azure Functions | Single project |
| [hosts/function-simple](hosts/function-simple) | Azure Functions | Core + Application + Function |
| [hosts/function-complex](hosts/function-complex) | Azure Functions | + Infrastructure |
| [hosts/worker-minimal](hosts/worker-minimal) | Worker / CronJob | Single project |
| [hosts/worker-simple](hosts/worker-simple) | Worker / CronJob | Core + Application + Worker |
| [hosts/worker-complex](hosts/worker-complex) | Worker / CronJob | + Infrastructure |

## Build all templates

```bash
cd templates
dotnet build Mvp24Hours.Templates.slnx --configuration Release
```

HTTP blueprints use **EF Core InMemory** by default so they run without Docker. Swap to SQL Server (or another provider) when you need durable storage.

## Production-ready baseline included

Templates now include a robust baseline based on mvp24hours:

- Native OpenAPI for HTTP hosts
- FluentValidation wiring in domain/application boundaries
- Request observability middleware for HTTP hosts
- Resilient HttpClient setup with `Microsoft.Extensions.Http.Resilience`
- Hybrid caching registration (`Mvp24Hours.Infrastructure.Caching`)
- HTTP middleware hardening for WebAPI/BFF: rate limiting, idempotency, output cache
- Keycloak identity baseline for WebAPI/BFF templates
- CronJob observability + health checks for worker templates

For HTTP templates, tune middleware behavior in `HttpHardening` section of each `appsettings*.json`.

## Docker compose per template

Each template folder now includes its own `docker-compose.yml` with the required local dependencies for that template shape.

Typical stacks by host type:

- Blueprints and HTTP hosts: SQL Server, Redis, RabbitMQ, Keycloak, Jaeger, Prometheus, Grafana
- Worker hosts: observability + messaging dependencies according to complexity level
- Function hosts: Azurite + observability dependencies

Run from the template folder:

```bash
docker compose up -d
```

## Related documentation

- [Architecture guides](../docs/en-us/guides/architecture/home.md)
- [Decision matrix](../docs/en-us/guides/architecture/decision-matrix.md)
- [Samples catalog](../samples/README.md)
