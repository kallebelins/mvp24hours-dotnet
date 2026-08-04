# Worker host — Complex

CronJob worker with **Core + Application + Infrastructure + Worker** layers.

## Projects

- `App.Core` — `IItemProcessor`, `IItemStore`
- `App.Application` — `ItemProcessor`
- `App.Infrastructure` — `InMemoryItemStore`
- `App.Worker` — CronJob host + health

## Production baseline included

- CronJob observability
- CronJob health checks
- Resilient HttpClient defaults

## Local dependencies

```bash
docker compose up -d
```

## Run

```bash
dotnet run --project App.Worker
```

## Related

- [`worker-simple`](../worker-simple)
- [`worker-minimal`](../worker-minimal)
