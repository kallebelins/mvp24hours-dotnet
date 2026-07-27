# Minimal API Structure

Use one host project for a small, cohesive service. Organize by feature before introducing projects that do not yet enforce a real boundary.

```text
Service/
├── Program.cs
├── Features/
│   └── Orders/
│       ├── Contracts.cs
│       ├── Endpoints.cs
│       └── Validation.cs
├── Domain/
│   └── Order.cs
├── Data/
│   ├── ServiceDbContext.cs
│   └── Configurations/
├── Infrastructure/
│   └── ServiceCollectionExtensions.cs
└── appsettings.json
```

Compose the service in `Program.cs`; keep endpoint mapping and DI registration in focused extensions. Add projects when compile-time dependency rules, independent ownership, or reusable hosts justify them.

Use these canonical pages for implementation:

- [Minimal APIs with TypedResults](../../../modernization/minimal-apis.md)
- [Native OpenAPI](../../../modernization/native-openapi.md)
- [EF Core registration](../../../database/use-context.md)
- [Web API Advanced](../../../webapi-advanced.md)
- [ProblemDetails](../../../modernization/problem-details.md)
- [Testing Cookbook](../../../testing/home.md)
