# Sample testing baseline

Teaching-oriented tests for Mvp24Hours samples. Full parity with `src/Tests` is out of scope.

## Stack

| Package | Role |
| --- | --- |
| `xunit` + `Microsoft.NET.Test.Sdk` | Test runner (same as `src/Tests`) |
| `FluentAssertions` | Readable assertions |
| `Moq` | Ports/handlers collaborators when needed |
| `Microsoft.AspNetCore.Mvc.Testing` | `WebApplicationFactory<T>` HTTP smoke tests |
| `Testcontainers.*` | Optional integration tests (SQL Server, MongoDB, RabbitMQ, Keycloak) |

Versions come from Central Package Management (`samples/Directory.Packages.props` → `src/Directory.Packages.props`).

## Layout

```text
samples/src/{sample}/
  {Host}.Test/                 # or CustomerAPI.Test
    {Host}.Test.csproj
    GlobalUsings.cs
    Unit/                      # domain, validators, specs, handlers, jobs
    Integration/               # WebApplicationFactory smoke (+ optional Testcontainers)
    Support/                   # DockerAvailability, fixtures (when needed)
```

Add the test project to the sample `.sln` / `.slnx`. Reference Application/Domain/Core for unit tests and the HTTP host for integration tests.

## Conventions

| Kind | Pattern | Example |
| --- | --- | --- |
| Method name | `Method_Scenario_Expected` | `Create_WhenEmailBlank_Throws` |
| Unit fixture | `{Type}Tests` | `CustomerAggregateTests` |
| Smoke fixture | `{Area}SmokeTests` | `HealthEndpointSmokeTests` |
| Category trait | `[Trait("Category", "Unit\|Integration")]` | Filter with `dotnet test --filter Category=Unit` |

## What each sample should ship

1. **Unit tests** for domain methods, validators, specifications, handlers, saga steps, or jobs that teach the sample’s main idea.
2. **One HTTP smoke test** per HTTP host using `WebApplicationFactory<Program>` (add `public partial class Program { }` to the host `Program.cs`).
3. Prefer **in-memory / mocked** dependencies for smoke tests so `Category=Unit` and basic smoke runs without Docker.
4. **Testcontainers** (optional) for SQL Server, MongoDB, RabbitMQ, and Keycloak samples — see [Testcontainers](#testcontainers). Skip when Docker is unavailable.

Existing migrated samples (Phases 2–4) get at least one smoke test when feasible; not every Minimal/Simple sample needs a full suite.

## WebApplicationFactory checklist

1. Append to the host `Program.cs`:

```csharp
public partial class Program { }
```

2. Prefer overriding services in `ConfigureWebHost` / `ConfigureTestServices` (InMemory EF, fake brokers) instead of requiring live infrastructure for the default smoke test.
3. Assert a cheap endpoint (`/health`, `/health/live`, OpenAPI, or a read-only API) returns a non-5xx status.

## Testcontainers

Use Testcontainers when the sample’s teaching value depends on a real provider:

| Sample area | Package | Skip helper |
| --- | --- | --- |
| SQL Server / EF | `Testcontainers.MsSql` | `DockerAvailability.IsAvailable` |
| MongoDB | `Testcontainers.MongoDb` | same |
| RabbitMQ | `Testcontainers.RabbitMq` | same |
| Keycloak | `Testcontainers.Keycloak` | same |

Copy the skip pattern from `src/Tests/**/Support/DockerAvailability.cs`:

```csharp
if (!DockerAvailability.IsAvailable)
{
    // Skip the test (Fact Attribute skip, or Assert.Skip / conditional Fact)
}
```

Or use `[Fact(Skip = "...")]` only when Docker is known permanently unavailable in that environment; prefer runtime detection so CI with Docker still runs the suite.

Mark Testcontainers fixtures with `[Trait("Category", "Integration")]`.

### Local run

```bash
# Unit + in-memory smoke (no Docker)
dotnet test --filter "Category!=Integration"

# Everything, including Testcontainers (requires Docker Desktop / engine)
dotnet test
```

### Compose vs Testcontainers

- `docker-compose.yml` in a sample is for **manual** local runs of the app.
- Testcontainers is for **automated** tests that spin up ephemeral containers.
- Do not commit real credentials; use container defaults or placeholders documented in the sample README.

## Related docs

- [Testing helpers and cookbook](../docs/en-us/testing/home.md)
- Library tests: `src/Tests/`
- Sample README template: `templates/SAMPLE_README.template.md`
