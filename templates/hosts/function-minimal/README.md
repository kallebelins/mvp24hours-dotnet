# Function host — Minimal

Single-project Azure Functions **isolated worker** (.NET 10) with HTTP and timer triggers.

## Projects

- `App.Function` — `Program.cs`, `ItemFunctions` (GET/POST), `HeartbeatFunction` (timer)

## Run locally

```bash
cd App.Function
func start
# or
dotnet run
```

- GET items: `http://localhost:7071/api/items`
- POST item: `http://localhost:7071/api/items` with `{ "name": "Sample", "note": "optional" }`

## Storage

`local.settings.json` sets `AzureWebJobsStorage` to `UseDevelopmentStorage=true`. Use Azurite locally or set the value to empty for timer-only runs during development.

## Related

- [`function-simple`](../function-simple) — Core + Application layers
- [`function-complex`](../function-complex) — + Infrastructure repository
