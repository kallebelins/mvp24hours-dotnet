# <Sample name>

<One-paragraph description of the problem demonstrated by this sample.>

## Status

- Migration status: `<migrated | planned | deprecated>`
- Target framework: `net10.0`
- Mvp24Hours consumption: project references by default; published packages are optional

## Features

- <Primary capability>
- <Data store, broker, or external integration>
- <Native OpenAPI, ProblemDetails, observability, or other cross-cutting features>

## Architecture

- Tier: `<Minimal | Simple | Complex | Blueprint>`
- Shape: <Minimal API, N-layers, Clean Architecture, CQRS, Hexagonal, etc.>
- Why this shape fits: <brief explanation>

## Layers

- `<Project or folder>` — <responsibility>
- `<Project or folder>` — <responsibility>
- `<Project or folder>` — <responsibility>

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- <SQL Server, PostgreSQL, MySQL, MongoDB, Redis, RabbitMQ, Keycloak, Docker, etc.>

## Configuration

Configure secrets with environment variables, user secrets, or a secret store. Never commit credentials.

| Key | Required | Description | Example |
| --- | --- | --- | --- |
| `<Section:Key>` | Yes | <purpose> | `<safe example>` |

## Run

From this sample's solution directory:

```bash
dotnet restore
dotnet run --project <path-to-host.csproj>
```

### Docker Compose

<Add commands and service notes when the sample includes compose.yaml, or remove this subsection.>

```bash
docker compose up -d
```

### Database providers

<Document the default provider and exact steps for SQL Server, PostgreSQL, or MySQL. Remove providers that do not apply.>

## Explore the API

- OpenAPI document: `<development URL>/openapi/v1.json`
- <HTTP file, Scalar/other UI, health endpoint, or worker behavior>

## Related documentation

- [Getting started](../../../docs/en-us/getting-started.md)
- [Architecture guidance](../../../docs/en-us/guides/architecture/home.md)
- <Add direct links to every Mvp24Hours feature demonstrated by this sample.>

## What this sample intentionally does not cover

- <Production concern omitted to keep the sample focused>
- <Alternative architecture or provider>
- <Security, scale, or deployment limitation that readers must not infer is solved>
