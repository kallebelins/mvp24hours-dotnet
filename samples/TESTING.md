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

Add the test project to the sample `.slnx`. Reference Application/Domain/Core for unit tests and the HTTP host for integration tests.

## Conventions

| Kind | Pattern | Example |
| --- | --- | --- |
| Method name | `Method_Scenario_Expected` | `Create_WhenEmailBlank_Throws` |
| Unit fixture | `{Type}Tests` | `CustomerAggregateTests` |
| Smoke fixture | `{Area}SmokeTests` | `HealthEndpointSmokeTests` |
| Category trait | `[Trait("Category", "Unit\|Integration")]` | Filter with `dotnet test --filter Category=Unit` |

## What each sample should ship

1. **Unit tests** for domain methods, validators, specifications, handlers, saga steps, or jobs that teach the sample's main idea.
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

Use Testcontainers when the sample's teaching value depends on a real provider. Library reference implementations live under `src/Tests/**` (for example `Mvp24Hours.Application.Integration.Test` for SQL Server, `Mvp24Hours.Application.RabbitMQ.Test` for RabbitMQ, `Mvp24Hours.Infrastructure.Data.MongoDb.Test` for MongoDB, and `Mvp24Hours.Infrastructure.Identity.Keycloak.Test` for Keycloak).

| Sample area | Package | Representative sample | Skip helper |
| --- | --- | --- | --- |
| SQL Server / EF | `Testcontainers.MsSql` | `complex-cqrs-ef-customer-api` | `DockerAvailability.IsAvailable` + `[DockerFact]` |
| MongoDB | `Testcontainers.MongoDb` | `complex-crud-mongodb-customer-api` | same |
| RabbitMQ (+ SQL when outbox/inbox) | `Testcontainers.RabbitMq` (+ `Testcontainers.MsSql`) | `complex-event-driven-rabbitmq-customer-api` | same |
| Keycloak | `Testcontainers.Keycloak` | `complex-keycloak-customer-api` | same |

Copy-paste starters: [`templates/SAMPLE_TEST_DockerAvailability.cs.template`](templates/SAMPLE_TEST_DockerAvailability.cs.template), [`SAMPLE_TEST_DockerFactAttribute.cs.template`](templates/SAMPLE_TEST_DockerFactAttribute.cs.template), and provider fixtures in the representative samples listed above.

### Skip gracefully when Docker is unavailable

1. Add `Support/DockerAvailability.cs` (runs `docker info` once per process).
2. Add `Support/DockerFactAttribute.cs` — sets `FactAttribute.Skip` when Docker is down so `dotnet test` still passes on machines without Docker.
3. In collection fixtures, set `IsAvailable = false` when startup fails; guard test bodies with `if (!fixture.IsAvailable) return;` for double safety.
4. Mark fixtures and tests with `[Trait("Category", "Integration")]`.

```csharp
[DockerFact]
public async Task MyIntegrationTest()
{
    if (!fixture.IsAvailable) return;
    // ...
}
```

Prefer runtime detection over hard-coded `[Fact(Skip = "...")]` so CI with Docker still runs the suite.

### SQL Server (EF Core samples)

**When to use:** CRUD/CQRS/DDD/pipeline samples that call `UseSqlServer` and teach persistence behavior.

**Pattern:**

1. `SqlServerContainerFixture : IAsyncLifetime` starts `MsSqlBuilder` (see library `SqlServerContainerFixture`).
2. `SqlServerCustomerApiFactory : WebApplicationFactory<Program>` overrides `ConnectionStrings:EFDBContext` via `ConfigureAppConfiguration`.
3. Call `EnsureCreatedAsync()` (or migrations) in the test before HTTP calls — the host skips migrate/seed in `Testing` environment.
4. Assert create/read HTTP flows against real SQL.

Reference: `samples/src/complex-cqrs-ef-customer-api/CustomerAPI.Test/`.

### MongoDB samples

**When to use:** `minimal-crud-mongodb`, `simple-crud-mongodb`, `complex-crud-mongodb` when document-store behavior matters.

**Pattern:**

1. `MongoDbContainerFixture` starts `MongoDbBuilder("mongo:6.0")`.
2. Factory overrides `ConnectionStrings:MongoDbContext`.
3. Use a unique database name per run if tests mutate shared data (library fixtures use `Guid`-suffixed names).

Reference: `samples/src/complex-crud-mongodb-customer-api/CustomerAPI.Test/`.

### RabbitMQ samples

**When to use:** `simple-rabbitmq`, `complex-event-driven-rabbitmq`, `complex-saga-rabbitmq` when broker wiring must be verified.

**Two levels:**

| Level | Purpose |
| --- | --- |
| Smoke (no Docker) | `ReplaceRabbitMQWithInMemory()` in `WebApplicationFactory` — default for OpenAPI/health smoke tests. |
| Testcontainers | Real broker — publish/consume or health checks with `RabbitMqBuilder("rabbitmq:3.13-management")`. |

Event-driven samples that use inbox/outbox also need SQL Server; start both containers and override both connection strings. Disable `IHostedService` registrations in tests so background consumers do not race assertions.

Reference: `samples/src/complex-event-driven-rabbitmq-customer-api/CustomerAPI.Test/`.

### Keycloak samples

**When to use:** `complex-keycloak-customer-api` JWT and Admin API flows.

**Pattern:**

1. Ship a realm JSON under `CustomerAPI.Test/Fixtures/` with `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>`.
2. `KeycloakContainerFixture` uses `KeycloakBuilder(...).WithRealm(realmPath)`.
3. Override all `Keycloak:*` configuration keys in `WebApplicationFactory` to point at the ephemeral realm.
4. Obtain tokens via `IKeycloakTokenService` from a fixture-built `ServiceProvider`, then call protected endpoints.

Reference: `samples/src/complex-keycloak-customer-api/CustomerAPI.Test/` (realm aligned with `src/Tests/Mvp24Hours.Infrastructure.Identity.Keycloak.Test`).

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
- Sample test templates: `templates/SAMPLE_TEST*.template`
