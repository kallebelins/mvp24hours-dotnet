# Containerization Patterns for AI Agents

> **AI Agent Instruction**: Use these patterns when containerizing Mvp24Hours-based applications. Follow Docker best practices for .NET applications.

---

## Container Strategy

| Pattern | Use Case | Complexity |
|---------|----------|------------|
| Single Container | Simple APIs, Minimal APIs | Low |
| Multi-Stage Build | Production deployments | Medium |
| Docker Compose | Development, Multi-service | Medium |
| Kubernetes | Production, Scale | High |

---

## Single-Stage Dockerfile (Development)

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["ProjectName.sln", "."]
COPY ["src/ProjectName.Core/ProjectName.Core.csproj", "src/ProjectName.Core/"]
COPY ["src/ProjectName.Infrastructure/ProjectName.Infrastructure.csproj", "src/ProjectName.Infrastructure/"]
COPY ["src/ProjectName.Application/ProjectName.Application.csproj", "src/ProjectName.Application/"]
COPY ["src/ProjectName.WebAPI/ProjectName.WebAPI.csproj", "src/ProjectName.WebAPI/"]

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build
WORKDIR "/src/src/ProjectName.WebAPI"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProjectName.WebAPI.dll"]
```

---

## Multi-Stage Dockerfile (Production)

```dockerfile
# Dockerfile
# ============================================
# Stage 1: Build
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy only project files for layer caching
COPY ["src/ProjectName.Core/ProjectName.Core.csproj", "src/ProjectName.Core/"]
COPY ["src/ProjectName.Infrastructure/ProjectName.Infrastructure.csproj", "src/ProjectName.Infrastructure/"]
COPY ["src/ProjectName.Application/ProjectName.Application.csproj", "src/ProjectName.Application/"]
COPY ["src/ProjectName.WebAPI/ProjectName.WebAPI.csproj", "src/ProjectName.WebAPI/"]
COPY ["ProjectName.sln", "."]

# Restore dependencies (cached layer)
RUN dotnet restore "src/ProjectName.WebAPI/ProjectName.WebAPI.csproj"

# Copy remaining source code
COPY . .

# Build application
WORKDIR "/src/src/ProjectName.WebAPI"
RUN dotnet build "ProjectName.WebAPI.csproj" -c Release -o /app/build --no-restore

# ============================================
# Stage 2: Publish
# ============================================
FROM build AS publish
RUN dotnet publish "ProjectName.WebAPI.csproj" -c Release -o /app/publish \
    --no-restore \
    /p:UseAppHost=false \
    /p:PublishSingleFile=false \
    /p:PublishTrimmed=false

# ============================================
# Stage 3: Runtime
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final

# Security: Run as non-root user
RUN addgroup -g 1000 appgroup && \
    adduser -u 1000 -G appgroup -D appuser

WORKDIR /app

# Copy published application
COPY --from=publish --chown=appuser:appgroup /app/publish .

# Set environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

# Switch to non-root user
USER appuser

EXPOSE 8080

ENTRYPOINT ["dotnet", "ProjectName.WebAPI.dll"]
```

---

## .dockerignore

```dockerignore
# .dockerignore
**/.git
**/.gitignore
**/.vs
**/.vscode
**/.idea
**/bin
**/obj
**/node_modules
**/*.md
**/Dockerfile*
**/.dockerignore
**/docker-compose*.yml
**/*.user
**/*.suo
**/*.sln.docstates
**/TestResults
**/.coverage
**/coverage
**/*.log
**/docs
**/tests
**/*.Tests
```

---

## Docker Compose (Development)

```yaml
# docker-compose.yml
version: '3.8'

services:
  api:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: projectname-api
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=ProjectNameDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True
      - ConnectionStrings__MongoDB=mongodb://mongodb:27017
      - ConnectionStrings__Redis=redis:6379
      - RabbitMQ__HostName=rabbitmq
      - RabbitMQ__UserName=guest
      - RabbitMQ__Password=guest
    depends_on:
      sqlserver:
        condition: service_healthy
      mongodb:
        condition: service_started
      redis:
        condition: service_started
      rabbitmq:
        condition: service_healthy
    networks:
      - projectname-network
    healthcheck:
      test: ["CMD", "wget", "--no-verbose", "--tries=1", "--spider", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 10s

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: projectname-sqlserver
    ports:
      - "1433:1433"
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong@Passw0rd
      - MSSQL_PID=Developer
    volumes:
      - sqlserver-data:/var/opt/mssql
    networks:
      - projectname-network
    healthcheck:
      test: /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -C -Q "SELECT 1"
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 30s

  mongodb:
    image: mongo:7
    container_name: projectname-mongodb
    ports:
      - "27017:27017"
    volumes:
      - mongodb-data:/data/db
    networks:
      - projectname-network

  redis:
    image: redis:7-alpine
    container_name: projectname-redis
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data
    networks:
      - projectname-network
    command: redis-server --appendonly yes

  rabbitmq:
    image: rabbitmq:3-management-alpine
    container_name: projectname-rabbitmq
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      - RABBITMQ_DEFAULT_USER=guest
      - RABBITMQ_DEFAULT_PASS=guest
    volumes:
      - rabbitmq-data:/var/lib/rabbitmq
    networks:
      - projectname-network
    healthcheck:
      test: rabbitmq-diagnostics -q ping
      interval: 30s
      timeout: 10s
      retries: 5
      start_period: 30s

volumes:
  sqlserver-data:
  mongodb-data:
  redis-data:
  rabbitmq-data:

networks:
  projectname-network:
    driver: bridge
```

---

## Docker Compose (Production)

```yaml
# docker-compose.prod.yml
version: '3.8'

services:
  api:
    image: ${DOCKER_REGISTRY}/projectname-api:${TAG:-latest}
    container_name: projectname-api
    restart: unless-stopped
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=${SQL_CONNECTION_STRING}
      - ConnectionStrings__MongoDB=${MONGODB_CONNECTION_STRING}
      - ConnectionStrings__Redis=${REDIS_CONNECTION_STRING}
      - RabbitMQ__HostName=${RABBITMQ_HOST}
      - RabbitMQ__UserName=${RABBITMQ_USER}
      - RabbitMQ__Password=${RABBITMQ_PASSWORD}
      - JwtSettings__SecretKey=${JWT_SECRET}
    deploy:
      mode: replicated
      replicas: 3
      resources:
        limits:
          cpus: '0.5'
          memory: 512M
        reservations:
          cpus: '0.25'
          memory: 256M
      update_config:
        parallelism: 1
        delay: 10s
        failure_action: rollback
      rollback_config:
        parallelism: 1
        delay: 10s
    healthcheck:
      test: ["CMD", "wget", "--no-verbose", "--tries=1", "--spider", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 30s
    networks:
      - projectname-network
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"

  nginx:
    image: nginx:alpine
    container_name: projectname-nginx
    restart: unless-stopped
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf:ro
      - ./nginx/ssl:/etc/nginx/ssl:ro
    depends_on:
      - api
    networks:
      - projectname-network

networks:
  projectname-network:
    driver: bridge
```

---

## Health Checks Configuration

```csharp
// Extensions/HealthCheckExtensions.cs
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ProjectName.WebAPI.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddCustomHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHealthChecks()
            // Database health checks
            .AddSqlServer(
                configuration.GetConnectionString("DefaultConnection")!,
                name: "sqlserver",
                tags: new[] { "database", "sql" })
            .AddMongoDb(
                configuration.GetConnectionString("MongoDB")!,
                name: "mongodb",
                tags: new[] { "database", "nosql" })
            .AddRedis(
                configuration.GetConnectionString("Redis")!,
                name: "redis",
                tags: new[] { "cache" })
            // RabbitMQ health check
            .AddRabbitMQ(
                configuration.GetConnectionString("RabbitMQ")!,
                name: "rabbitmq",
                tags: new[] { "messaging" })
            // Custom health check
            .AddCheck<CustomHealthCheck>("custom", tags: new[] { "custom" });

        return services;
    }
}

public class CustomHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Add custom health check logic
        var isHealthy = true;

        if (isHealthy)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Service is healthy"));
        }

        return Task.FromResult(HealthCheckResult.Unhealthy("Service is unhealthy"));
    }
}
```

### Health Check Endpoints

```csharp
// Program.cs
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("database"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // Always returns healthy if app is running
});
```

---

## Environment-Specific Configuration

### appsettings.Docker.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=sqlserver;Database=ProjectNameDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True",
    "MongoDB": "mongodb://mongodb:27017",
    "Redis": "redis:6379",
    "RabbitMQ": "amqp://guest:guest@rabbitmq:5672"
  }
}
```

### Program.cs Configuration

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Support Docker environment
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Docker.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Configure Kestrel for containers
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});
```

---

## Nginx Configuration

```nginx
# nginx/nginx.conf
events {
    worker_connections 1024;
}

http {
    upstream api_servers {
        least_conn;
        server api:8080;
    }

    server {
        listen 80;
        server_name _;

        location / {
            return 301 https://$host$request_uri;
        }

        location /health {
            proxy_pass http://api_servers/health;
            proxy_http_version 1.1;
        }
    }

    server {
        listen 443 ssl http2;
        server_name _;

        ssl_certificate /etc/nginx/ssl/certificate.crt;
        ssl_certificate_key /etc/nginx/ssl/private.key;

        ssl_protocols TLSv1.2 TLSv1.3;
        ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256;
        ssl_prefer_server_ciphers off;

        location / {
            proxy_pass http://api_servers;
            proxy_http_version 1.1;
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection keep-alive;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
            proxy_cache_bypass $http_upgrade;
        }
    }
}
```

---

## CI/CD Docker Build

### GitHub Actions

```yaml
# .github/workflows/docker-build.yml
name: Docker Build and Push

on:
  push:
    branches: [main]
    tags: ['v*']
  pull_request:
    branches: [main]

env:
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository }}

jobs:
  build:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write

    steps:
      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v3

      - name: Log in to Container Registry
        if: github.event_name != 'pull_request'
        uses: docker/login-action@v3
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Extract metadata
        id: meta
        uses: docker/metadata-action@v5
        with:
          images: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}
          tags: |
            type=ref,event=branch
            type=ref,event=pr
            type=semver,pattern={{version}}
            type=semver,pattern={{major}}.{{minor}}
            type=sha

      - name: Build and push
        uses: docker/build-push-action@v5
        with:
          context: .
          push: ${{ github.event_name != 'pull_request' }}
          tags: ${{ steps.meta.outputs.tags }}
          labels: ${{ steps.meta.outputs.labels }}
          cache-from: type=gha
          cache-to: type=gha,mode=max
```

---

## Common Docker Commands

```bash
# Build image
docker build -t projectname-api:latest .

# Run container
docker run -d -p 8080:8080 --name projectname-api projectname-api:latest

# Development with docker-compose
docker-compose up -d
docker-compose logs -f api
docker-compose down

# Production deployment
docker-compose -f docker-compose.prod.yml up -d

# View logs
docker logs projectname-api -f

# Execute commands in container
docker exec -it projectname-api /bin/sh

# Health check
curl http://localhost:8080/health

# Clean up
docker system prune -af
docker volume prune -f
```

---

## Related Documentation

- [Architecture Templates](architecture-templates.md)
- [Observability Patterns](observability-patterns.md)
- [Modernization Patterns](modernization-patterns.md)

