# Keycloak identity integration

`Mvp24Hours.Infrastructure.Identity.Keycloak` integrates ASP.NET Core applications with Keycloak on .NET 10. It includes JWT bearer authentication, role claim transformation, UMA decision and RPT authorization, Admin REST clients, local-user synchronization, and health checks.

> The package uses a first-party OIDC/OAuth client implementation. It has no dependency on Duende IdentityServer, IdentityModel, or Keycloak.AuthServices.

## Install

```bash
dotnet add package Mvp24Hours.Infrastructure.Identity.Keycloak
```

## Configure Keycloak

Create an API client in the target realm and configure its audience in emitted access tokens. For Admin REST operations, create a separate confidential client with service accounts enabled and grant only the required `realm-management` roles.

```json
{
  "Keycloak": {
    "Authority": "https://identity.example.com/realms/acme",
    "Realm": "acme",
    "ClientId": "orders-api",
    "Audience": "orders-api",
    "RequireHttpsMetadata": true,
    "ValidateIssuer": true,
    "ValidateAudience": true,
    "TokenClockSkew": "00:00:05",
    "DiscoveryCacheTtl": "1.00:00:00",
    "Authorization": {
      "PolicyEnforcementMode": "Enforcing",
      "UseDecisionEndpoint": true,
      "UseRptEndpoint": false,
      "PermissionClaimType": "permissions",
      "ResourceServerClientId": "orders-api"
    },
    "Admin": {
      "AdminBaseUrl": "https://identity.example.com/admin/realms/acme",
      "Realm": "acme",
      "ClientId": "orders-admin",
      "ClientSecret": "load-from-a-secret-provider",
      "ServiceAccountEnabled": true,
      "Timeout": "00:00:30",
      "RetryCount": 3
    }
  }
}
```

`Authority` is the realm URL, while `AdminBaseUrl` is the realm Admin REST URL. HTTPS metadata, issuer validation, and audience validation should remain enabled outside isolated local development.

## Register the integration

For applications that use authentication, authorization, and the Admin API:

```csharp
using Mvp24Hours.Infrastructure.Identity.Keycloak.HealthChecks;
using Mvp24Hours.Infrastructure.Identity.Keycloak.WebAPI.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKeycloakServices(builder.Configuration);
builder.Services
    .AddHealthChecks()
    .AddKeycloakHealthCheck(tags: ["identity", "ready"]);

var app = builder.Build();

app.UseAuthentication();
app.UseKeycloakCurrentUser();
app.UseAuthorization();

app.MapHealthChecks("/health/ready");
app.Run();
```

Call `UseKeycloakCurrentUser` after authentication and before endpoints that consume the current-user abstraction.

Smaller applications can register only what they use:

```csharp
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddKeycloakAuthorization(builder.Configuration);
// Or:
builder.Services.AddKeycloakAdminServices(builder.Configuration);
```

All three option objects are validated at startup. Invalid URLs, missing required identifiers, non-positive timeouts, and conflicting decision/RPT modes fail fast.

## Authentication and current user

`AddKeycloakAuthentication` configures the standard JWT bearer handler. Incoming claim names are not remapped. Realm roles and roles for `ResourceServerClientId` are added using `RealmRoleClaimType`, which defaults to `ClaimTypes.Role`.

```csharp
app.MapGet(
    "/me",
    (IKeycloakCurrentUser currentUser) =>
        currentUser.IsAuthenticated
            ? Results.Ok(currentUser.User)
            : Results.Unauthorized())
    .RequireAuthorization();
```

`IKeycloakTokenService` provides client-credentials tokens, refresh, introspection, and revocation using endpoints discovered from the realm. The resource-owner password grant exists only to support automated tests and should not be used for interactive production authentication.

## Role and resource authorization

Use normal ASP.NET Core role authorization after registering Keycloak authentication:

```csharp
app.MapGet("/orders", () => Results.Ok())
    .RequireAuthorization(policy => policy.RequireRole("orders-reader"));
```

For a Keycloak UMA decision:

```csharp
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization.Decision;

builder.Services.AddKeycloakAuthorization(
    decisionRequirements: new()
    {
        ["orders:read"] = [new DecisionRequirement("orders", "read")]
    });

app.MapGet("/orders/{id}", (string id) => Results.Ok(id))
    .RequireAuthorization("orders:read");
```

For RPT enforcement, set `UseDecisionEndpoint` to `false`, set `UseRptEndpoint` to `true`, configure `ResourceServerClientId`, and register `RptRequirement` values instead. Decision and RPT modes cannot both be enabled.

`AddKeycloakPolicies(typeof(Program).Assembly)` discovers roles and `resource#scope` policy values from `[Authorize]` attributes when attribute-driven registration is preferred.

## Admin REST API

The Admin integration registers three scoped contracts:

- `IKeycloakUserService`: user CRUD, enable/disable, password reset, roles, and group membership.
- `IKeycloakRoleService`: realm and client role CRUD and lookup.
- `IKeycloakGroupService`: group CRUD, child groups, and member lookup.

```csharp
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin.Requests;

app.MapPost(
    "/provision-user",
    async (
        CreateUserRequest request,
        IKeycloakUserService users,
        CancellationToken cancellationToken) =>
    {
        var result = await users.CreateUserAsync(request, cancellationToken);
        return result.HasErrors ? Results.BadRequest(result.Messages) : Results.Ok(result);
    })
    .RequireAuthorization("provision-users");
```

The Admin client obtains service-account tokens automatically and retries transient failures. Avoid assigning broad realm-management roles when a narrower set is sufficient.

## Synchronize a local user store

Implement `IUserKeycloakService` and register the implementation:

```csharp
builder.Services.AddKeycloakUserSync<MyUserKeycloakService>();
```

After successful JWT validation, the package checks whether the Keycloak subject exists locally. It calls `CreateOrUpdateLocalUserAsync` when no local user exists. Keep this operation idempotent and enforce a unique local mapping for the Keycloak subject because simultaneous requests can trigger synchronization.

## Health checks

```csharp
builder.Services
    .AddHealthChecks()
    .AddKeycloakHealthCheck(
        name: "keycloak",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["identity", "ready"],
        timeout: TimeSpan.FromSeconds(5));
```

The check verifies that OIDC discovery metadata can be loaded. It does not prove that every Admin API permission or authorization policy is correctly configured.

## Testing

Unit tests can run without Docker:

```bash
dotnet test --filter "Category=Unit"
```

The package integration suite starts Keycloak with Testcontainers and therefore requires Docker:

```bash
dotnet test --filter "Category=Integration"
```

Use unique names for test users, roles, and groups, and clean up resources that a test creates. Do not use production realms or credentials in integration tests.

## Security checklist

- Keep `ClientSecret` in a secret provider or environment-specific configuration.
- Require HTTPS metadata in deployed environments.
- Validate both issuer and audience.
- Use separate API and Admin clients.
- Grant the Admin service account only the roles it needs.
- Prefer authorization code with PKCE for interactive clients; do not use the password grant.

See the [Keycloak server documentation](https://www.keycloak.org/documentation) and [Admin REST API](https://www.keycloak.org/docs-api/latest/rest-api/index.html) for server-side configuration details.

> **Sample:** [`complex-keycloak-customer-api`](../../../samples/src/complex-keycloak-customer-api/CustomerAPI.WebAPI/README.md) — JWT bearer validation and Admin create-user / reset-password / assign-role flows without Duende or IdentityModel packages.
