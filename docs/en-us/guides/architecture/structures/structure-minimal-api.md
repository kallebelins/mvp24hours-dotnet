---
templateId: minimal-api
tier: Minimal
shape: structure
layers: [Host, Features, Domain, Data, Infrastructure]
dependencyRule: Single host project; add projects only when compile-time boundaries justify them
samplePath: samples/src/minimal-crud-ef-customer-api
mvp24hoursModules: [webapi, database, modernization/minimal-apis, modernization/native-openapi, testing]
---

---
templateId: minimal-api
tier: Minimal
shape: Minimal API single host
dependencyRule: Single host with feature folders until compile-time boundaries are justified
samplePath: samples/src/minimal-crud-ef-customer-api
mvp24hoursModules:
  - webapi
  - database
  - observability
layers:
  - Host
  - Features
  - Domain
  - Data
  - Infrastructure
---

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
