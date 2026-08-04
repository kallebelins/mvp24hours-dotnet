# API host — Complex N-Layers

This host shape is implemented by the blueprint:

**[`templates/blueprints/complex-nlayers`](../../blueprints/complex-nlayers)**

Copy that folder to start a new ASP.NET Core API with Core / Application / Infrastructure / WebAPI layers and a placeholder `Item` resource.

## Local dependencies

This host template now includes `docker-compose.yml` with production-like dependencies used by mvp24hours patterns:

- SQL Server
- Redis
- RabbitMQ
- Keycloak
- Jaeger
- Prometheus
- Grafana

Start local dependencies:

```bash
docker compose up -d
```

See also: [architecture templates catalog](../../README.md).
