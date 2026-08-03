# Worker host — Simple

CronJob worker with **Core + Application + Worker** layers.

## Projects

- `App.Core` — `IItemProcessor`
- `App.Application` — `ItemProcessor`
- `App.Worker` — `ItemProcessingJob` CronJob + health

## Run

```bash
dotnet run --project App.Worker
```

## Related

- [`worker-minimal`](../worker-minimal)
- [`worker-complex`](../worker-complex)
