# Worker host — Simple

CronJob worker with **Core + Application + Worker** layers.

## Projects

- `App.Core` — `IItemProcessor`
- `App.Application` — `ItemProcessor`
- `App.Worker` — `ItemProcessingJob` CronJob + health

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

- [`worker-minimal`](../worker-minimal)
- [`worker-complex`](../worker-complex)
