# Function host — Simple

Azure Functions isolated worker with **Core + Application + Function** layers and DI.

## Projects

- `App.Core` — `Item` model, `IItemService`
- `App.Application` — `ItemService` (in-memory store)
- `App.Function` — HTTP triggers calling `IItemService`

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

- GET: `http://localhost:7071/api/items`
- POST: `http://localhost:7071/api/items`

## Related

- [`function-minimal`](../function-minimal) — single project
- [`function-complex`](../function-complex) — + Infrastructure repository
