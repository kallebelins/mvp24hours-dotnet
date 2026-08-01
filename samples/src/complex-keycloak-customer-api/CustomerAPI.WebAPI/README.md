# Complex Keycloak Customer API

Demonstrates how to secure an ASP.NET Core Web API with Keycloak using the
`Mvp24Hours.Infrastructure.Identity.Keycloak` library: JWT bearer validation,
a current-user middleware, and an internal Admin flow that creates users,
resets passwords, and assigns realm roles — all without Duende or IdentityModel.

> **Note:** Duende IdentityServer and IdentityModel are **not** used in this sample.
> Authentication and Admin REST calls rely exclusively on `Mvp24Hours.Infrastructure.Identity.Keycloak`.

## Status

- Migration status: `migrated`
- Target framework: `net10.0`
- Mvp24Hours consumption: project references by default; published packages are optional

## Features

- JWT bearer validation via `AddKeycloakServices` (no manual `JwtBearerOptions` setup)
- `UseKeycloakCurrentUser` middleware populates `IKeycloakCurrentUser` per request
- Admin controller: create user, reset password, assign realm role via `IKeycloakUserService`
- Keycloak OIDC health check (`AddKeycloakHealthCheck`)
- Validated options (`KeycloakOptions` + `KeycloakAdminOptions`) fail-fast on startup
- In-memory customer store — keeps focus on identity, not persistence
- Native OpenAPI, ProblemDetails, NLog, health endpoint `/hc`

## Architecture

- Tier: `Complex`
- Shape: intentionally thin host (**WebAPI + Core** only — no Application layer) to keep focus on Keycloak identity integration
- **WebAPI → Core**; Mvp24Hours Keycloak packages composed at WebAPI
- A full four-layer stack would obscure the identity plumbing with unrelated CRUD code

## Layers

- `CustomerAPI.Core` — Entities (`Customer`) and DTOs (results + admin request bodies)
- `CustomerAPI.WebAPI` — ASP.NET Core host; Keycloak middleware, JWT auth, controllers

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/) (to run Keycloak locally)
- A Keycloak realm named `mvp24hours` with:
  - A **confidential** client `customer-api` (used for token validation / audience)
  - A **service-account-enabled** client `customer-admin` with `realm-admin` role (used by Admin API calls)

## Running Keycloak

Start a local Keycloak instance with Docker Compose (from `samples/src/complex-keycloak-customer-api`):

```bash
docker compose up -d
```

Keycloak listens on **8080**. Set `KEYCLOAK_ADMIN_PASSWORD` in `docker-compose.yml` and use the same value when signing in to the admin console.

Alternatively, run a standalone container:

```bash
docker run -d \
  --name keycloak \
  -p 8080:8080 \
  -e KEYCLOAK_ADMIN=admin \
  -e KEYCLOAK_ADMIN_Password=<secret> \
  quay.io/keycloak/keycloak:latest \
  start-dev
```

Then open `http://localhost:8080` and:

1. Create realm `mvp24hours`.
2. Create client `customer-api` (OpenID Connect, Standard Flow, Bearer-only). Copy Client ID to `Keycloak:ClientId` and `Keycloak:Audience`.
3. Create client `customer-admin` (OpenID Connect, Service Accounts Enabled). Assign `realm-admin` role to its service account. Copy Client ID and secret to `Keycloak:Admin:ClientId` and `Keycloak:Admin:ClientSecret`.

The sample root includes `docker-compose.yml` with a Keycloak service. Use `docker compose up -d` instead of the standalone `docker run` command above when you prefer Compose.

## Configuration

Configure secrets with environment variables, user secrets, or a secret store. Never commit credentials.

| Key | Required | Description | Example |
| --- | --- | --- | --- |
| `Keycloak:Authority` | Yes | OIDC authority URL | `http://localhost:8080/realms/mvp24hours` |
| `Keycloak:Realm` | Yes | Keycloak realm name | `mvp24hours` |
| `Keycloak:ClientId` | Yes | Client ID for token audience validation | `customer-api` |
| `Keycloak:Audience` | Yes | Expected JWT audience | `customer-api` |
| `Keycloak:RequireHttpsMetadata` | No | Set `true` in production | `false` |
| `Keycloak:ValidateIssuer` | No | Validate token issuer | `true` |
| `Keycloak:ValidateAudience` | No | Validate token audience | `true` |
| `Keycloak:Admin:AdminBaseUrl` | Yes | Keycloak Admin REST base URL | `http://localhost:8080/admin/realms/mvp24hours` |
| `Keycloak:Admin:Realm` | Yes | Realm for Admin API calls | `mvp24hours` |
| `Keycloak:Admin:ClientId` | Yes | Service-account client ID | `customer-admin` |
| `Keycloak:Admin:ClientSecret` | Yes | **Secret — never commit** | `CHANGE_ME` |
| `Keycloak:Admin:ServiceAccountEnabled` | Yes | Must be `true` | `true` |

## Run

From this sample's solution directory:

```bash
dotnet restore
dotnet run --project CustomerAPI.WebAPI/CustomerAPI.WebAPI.csproj
```

## Explore the API

- OpenAPI document: `http://localhost:5300/openapi/v1.json`
- Swagger UI: `http://localhost:5300/swagger` (Development only)
- Health endpoint: `http://localhost:5300/hc`

### Authenticated endpoints (require Bearer token)

```
GET  /api/customers          — list all customers
GET  /api/customers/{id}     — get customer by id
```

### Admin endpoints (require `realm-admin` role)

```
POST /api/admin/keycloak/users               — create Keycloak user
GET  /api/admin/keycloak/users/{userId}      — get Keycloak user
POST /api/admin/keycloak/users/reset-password — reset user password
POST /api/admin/keycloak/users/assign-role   — assign realm role
```

Obtain a token from Keycloak and pass it as `Authorization: Bearer <token>`.

## Related documentation

- [Getting started](../../../docs/en-us/getting-started.md)
- [Identity / Keycloak](../../../docs/en-us/identity/keycloak.md)
- [Architecture guidance](../../../docs/en-us/guides/architecture/home.md)

## What this sample intentionally does not cover

- Production Keycloak hardening (HTTPS, cluster, realm export/import automation)
- Persistent customer storage — EF Core is intentionally omitted to keep focus on identity
- Refresh token flows or client-side token management
- Fine-grained UMA / RPT policy enforcement (see `AddKeycloakPolicies` for that path)
- Multi-tenant realms or realm-switching at runtime
