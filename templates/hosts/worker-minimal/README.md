# Worker host — Minimal

Single-project **CronJob worker** with health endpoints.

## Projects

- `App.Worker` — `ItemHeartbeatJob`, `/health` endpoints

## Packages

Uses `Mvp24Hours.Infrastructure.CronJob` with dual PackageReference/ProjectReference pattern and `AspNetCore.HealthChecks.UI.Client`.

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

- Health: `http://localhost:5300/health`
- Liveness: `http://localhost:5300/health/live`

## Related

- Sample: [`samples/src/simple-cronjob-worker`](../../../samples/src/simple-cronjob-worker)
- [`worker-simple`](../worker-simple) — Core + Application layers
- [`worker-complex`](../worker-complex) — + Infrastructure store
