# Infrastructure Layer Template

Infrastructure implements persistence, messaging, caching, and external service adapters defined by Core/Application contracts.

## Folder layout

```text
{Product}.Infrastructure/
├── Data/
│   ├── {Product}DbContext.cs
│   ├── Configurations/
│   ├── Migrations/
│   └── Seed/
├── Repositories/
├── Integrations/
└── Extensions/
    └── ServiceCollectionExtensions.cs
```

## Naming conventions

- DbContext: `{Product}DbContext.cs`
- EF configurations: `{Entity}Configuration.cs` in `Configurations/`
- Repository implementations: `{Entity}Repository.cs`
- DI extension: `Add{Product}Infrastructure(this IServiceCollection services, IConfiguration configuration)`

## Dependency rule

Infrastructure depends on **Core or Domain** (and optionally narrow Application contracts for adapter wiring). It is **composed at the WebAPI/Worker host**, not referenced from Application.

## DI registration snippet

```csharp
public static IServiceCollection Add{Product}Infrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.AddDbContext<{Product}DbContext>(options =>
        options.UseSqlServer(configuration.GetConnectionString("Default")));
    services.AddScoped<I{Entity}Repository, {Entity}Repository>();
    return services;
}
```

## Canonical sample

[`complex-crud-ef-customer-api`](https://github.com/kallebelins/mvp24hours-dotnet/blob/main/samples/src/complex-crud-ef-customer-api) — EF Core context, configurations, migrations, and repository implementations.

## Related documentation

- [Database Context](../../database/use-context.md)
- [Repository](../../database/use-repository.md)
- [Unit of Work](../../database/use-unitofwork.md)
