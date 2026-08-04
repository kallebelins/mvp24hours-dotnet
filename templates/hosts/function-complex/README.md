# Function host — Complex

Azure Functions isolated worker with **Core + Application + Infrastructure + Function** layers.

## Projects

- `App.Core` — `Item`, `IItemRepository`, `IItemService`
- `App.Application` — `ItemService`
- `App.Infrastructure` — `InMemoryItemRepository`
- `App.Function` — DI wiring, HTTP + timer triggers

## Production baseline included

- Application Insights worker integration
- Resilient HttpClient defaults

## Local dependencies

```bash
docker compose up -d
```

## Run

```bash
dotnet run --project App.Function
```

## Related

- [`function-simple`](../function-simple)
- [`function-minimal`](../function-minimal)
