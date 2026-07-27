# CustomerAPI - CRUD - EF - Complex
N-tier project used to develop APIs where the business needs to apply complex rules, higher level of security, less data traffic, validation of sensitive data and separation of responsibilities or consumption by other technologies and projects.

## Features:
- Relational database (SQL Server, PostgreSql, MySql) with EF; 
- Native OpenAPI;
- Mapping (AutoMapper); 
- Logging (NLog); 
- Patterns for data validation (FluentValidation and Data Annotations);
- Specification pattern;
- Unit of Work (Transaction);
- Repository (Paging, List, Create, Update, Delete) - Query apply: Navigation, Filter, Paging;
- FluentAPI configuration EF;
- Facade pattern;
- Dependency injection (IoC);
- Using ActionResult for API resources (Restful);
- Middlewares for handling unmanaged failures;
- DDD concepts;

## HTTP contract and runtime defaults
- In non-production environments, native OpenAPI JSON is available at `/openapi/v1.json`, with Swagger UI at `/swagger`.
- Expected validation and not-found outcomes keep the existing Mvp24Hours business and notification envelopes; unexpected exceptions use RFC ProblemDetails.
- This controller-based sample uses controller `ActionResult` responses and declared contracts.
- Settings are strongly typed and validated on startup.
- Logging uses `ILogger<T>` with the NLog provider.

## Layers:

### Core
Heart of the application. In this project we define the business: entities, valueobjects/dtos, validations, service contracts, enumerators, messages, specifications, builders or any other business definition.

### Infrastructure
Layer used to deal with issues related to infrastructure: database, web requests, reading/writing files, or rather, any connection to machine or network resources.

### Application
Layer where we implement/develop the rules defined in the "Core". We use this project as a gateway to the business frontier, which means that we will be able to consume business rules in different technologies (desktop, web api, web services, web mvc, web forms, hosted services, etc.).

### WebAPI
Layer that lies on the project boundary. We use this project to make the resources (data and actions) of our API available. Our client will connect via HTTP requests to get resources in JSON format ("application/json").

## Database integrated with EF

These .NET 10 samples use SQL Server by default. Central Package Management (CPM) in `samples/Directory.Packages.props` controls the default `Microsoft.EntityFrameworkCore.SqlServer` and health-check package versions, so project files do not specify versions.

For another provider, add its version centrally and reference the package from the project:

- SQL Server: `Microsoft.EntityFrameworkCore.SqlServer` with `UseSqlServer`
- PostgreSQL: `Npgsql.EntityFrameworkCore.PostgreSQL` with `UseNpgsql`
- MySQL: `Pomelo.EntityFrameworkCore.MySql` with `UseMySql`

Bind and validate connection settings at startup, then configure EF from the strongly typed options in `Program.cs` or a service extension:

```csharp
var connectionStrings = builder.Configuration
    .GetSection(ConnectionStringsOptions.SectionName)
    .Get<ConnectionStringsOptions>()
    ?? throw new InvalidOperationException("ConnectionStrings configuration is required.");

builder.Services.AddDbContext<EFDBContext>(options =>
    options.UseSqlServer(connectionStrings.EFDBContext));
```

For PostgreSQL, use `options.UseNpgsql(connectionStrings.EFDBContext)`. For MySQL, use `options.UseMySql(connectionStrings.EFDBContext, ServerVersion.AutoDetect(connectionStrings.EFDBContext))`. See the [relational database guide](https://kallebelins.github.io/mvp24hours-dotnet/#/en-us/database/relational).

## Health checks

The default SQL Server sample uses `AspNetCore.HealthChecks.SqlServer` and `AspNetCore.HealthChecks.UI.Client`, with versions controlled by CPM. Optional providers use `AspNetCore.HealthChecks.Npgsql` or `AspNetCore.HealthChecks.MySql`; add their versions centrally before referencing them.

```csharp
builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionStrings.EFDBContext,
        healthQuery: "SELECT 1;",
        name: "SqlServer",
        failureStatus: HealthStatus.Degraded);
```
