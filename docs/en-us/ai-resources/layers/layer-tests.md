# Tests Layer Template

Test projects mirror the production boundary they verify. Follow the [Sample testing baseline](https://github.com/kallebelins/mvp24hours-dotnet/blob/main/samples/TESTING.md).

## Folder layout

```text
{Product}.Test/
├── GlobalUsings.cs
├── Unit/
│   └── {Area}/
│       └── {Type}Tests.cs
├── Integration/
│   ├── {Product}ApiFactory.cs
│   └── OpenApiSmokeTests.cs
└── Support/
    ├── DockerAvailability.cs
    └── {Container}Fixture.cs    # Testcontainers when needed
```

## Naming conventions

- Test class: `{Type}Tests` or `{Area}SmokeTests`
- Test method: `{Method}_{Scenario}_{Expected}`
- Category trait: `[Trait("Category", "Unit")]` or `"Integration"`

## Integration smoke pattern

1. `WebApplicationFactory<Program>` swaps EF for InMemory in `Testing` environment.
2. `OpenApiSmokeTests` asserts GET `/openapi/v1.json` status is below 500.
3. Host exposes `public partial class Program { }`.

Copy templates from [`samples/templates/`](https://github.com/kallebelins/mvp24hours-dotnet/blob/main/samples/templates/):

- `SAMPLE_TEST_CustomerApiFactory.cs.template`
- `SAMPLE_TEST_OpenApiSmokeTests.cs.template`
- `SAMPLE_TEST.csproj.template`

## Canonical sample

[`complex-crud-ef-customer-api/CustomerAPI.Test`](https://github.com/kallebelins/mvp24hours-dotnet/blob/main/samples/src/complex-crud-ef-customer-api/CustomerAPI.Test) — factory + OpenAPI smoke.

Testcontainers references:

- SQL: `complex-cqrs-ef-customer-api`
- MongoDB: `complex-crud-mongodb-customer-api`

## Related documentation

- [Testing Cookbook](../../testing/home.md)
- [Sample TESTING.md](https://github.com/kallebelins/mvp24hours-dotnet/blob/main/samples/TESTING.md)
