# Function host — Complex

Azure Functions isolated worker with **Core + Application + Infrastructure + Function** layers.

## Projects

- `App.Core` — `Item`, `IItemRepository`, `IItemService`
- `App.Application` — `ItemService`
- `App.Infrastructure` — `InMemoryItemRepository`
- `App.Function` — DI wiring, HTTP + timer triggers

## Run

```bash
dotnet run --project App.Function
```

## Related

- [`function-simple`](../function-simple)
- [`function-minimal`](../function-minimal)
