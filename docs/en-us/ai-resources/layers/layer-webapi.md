# WebAPI Layer Template

The WebAPI project is the HTTP composition root: DI registration, middleware, controllers or minimal endpoints, and configuration binding.

## Folder layout

```text
{Product}.WebAPI/
├── Program.cs
├── Controllers/          # or Features/{Feature}/Endpoints.cs for minimal APIs
├── Extensions/
│   └── ServiceCollectionExtensions.cs
├── Middleware/
├── appsettings.json
└── appsettings.Development.json
```

## Naming conventions

- Controllers: `{Entity}Controller.cs`
- DI extension: `Add{Product}WebApi(this IServiceCollection services)`
- Endpoint groups: map in `Program.cs` or dedicated `Endpoints.cs` per feature

## Dependency rule

WebAPI references Application and Infrastructure (and Core only when needed for registration edge cases). It is the **composition root**.

## Program.cs pattern

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .Add{Product}Infrastructure(builder.Configuration)
    .Add{Product}Application()
    .AddMvp24HoursWebApi(builder.Configuration);

var app = builder.Build();
app.MapControllers();
app.Run();

public partial class Program { }
```

The `public partial class Program { }` declaration enables `WebApplicationFactory<Program>` integration tests.

## Canonical sample

[`complex-crud-ef-customer-api`](https://github.com/kallebelins/mvp24hours-dotnet/blob/main/samples/src/complex-crud-ef-customer-api/CustomerAPI.WebAPI) — controllers, DI extensions, and OpenAPI.

Minimal API reference: [`minimal-crud-ef-customer-api`](https://github.com/kallebelins/mvp24hours-dotnet/blob/main/samples/src/minimal-crud-ef-customer-api/CustomerAPI).

## Related documentation

- [Web API](../../webapi.md)
- [Web API Advanced](../../webapi-advanced.md)
- [Native OpenAPI](../../modernization/native-openapi.md)
