# Containerization

Containerize the deployable host, not every class-library project. The current source line targets .NET 10.

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish ./src/Product.WebAPI/Product.WebAPI.csproj     -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Product.WebAPI.dll"]
```

## Production checklist

- Pin production base images by your supply-chain policy and rebuild for security updates.
- Run as a non-root user, expose only required ports, and keep secrets outside the image.
- Use environment variables or a secret provider for configuration.
- Add liveness/readiness endpoints from the [Health Checks Catalog](../../infrastructure/health-checks.md).
- Emit structured logs to stdout and export traces/metrics through [Observability](../../observability/home.md).
- Set CPU/memory limits and graceful shutdown timeouts; propagate cancellation.
- Use multi-stage builds, a `.dockerignore`, and a locked/central package policy.
- Do not put database migrations in every replica startup without coordination.

For multi-service local development and service defaults, see [.NET Aspire](../../modernization/aspire.md). Container orchestration manifests are deployment-environment concerns and should not duplicate application configuration references.

## Related

- [Architecture Guides](../architecture/home.md)
- [Decision Matrix](../architecture/decision-matrix.md)
- [Health Checks Catalog](../../infrastructure/health-checks.md)
- [Observability](../../observability/home.md)
