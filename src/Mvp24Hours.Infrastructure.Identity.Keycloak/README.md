# Mvp24Hours.Infrastructure.Identity.Keycloak

First-party Keycloak integration for ASP.NET Core applications targeting .NET 10. The package provides JWT bearer authentication, Keycloak role mapping, UMA decision and RPT authorization, Admin REST API clients, local-user synchronization, and health checks.

This package does **not** depend on Duende IdentityServer, IdentityModel, or Keycloak.AuthServices. OIDC discovery and OAuth token operations are implemented by the package and use the ASP.NET Core authentication stack.

## Install

```bash
dotnet add package Mvp24Hours.Infrastructure.Identity.Keycloak
```

## Quick start

Configure `appsettings.json`:

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
    "Authorization": {
      "PolicyEnforcementMode": "Enforcing",
      "UseDecisionEndpoint": true,
      "UseRptEndpoint": false,
      "ResourceServerClientId": "orders-api"
    },
    "Admin": {
      "AdminBaseUrl": "https://identity.example.com/admin/realms/acme",
      "Realm": "acme",
      "ClientId": "orders-admin",
      "ClientSecret": "store-this-in-a-secret-provider",
      "ServiceAccountEnabled": true
    }
  }
}
```

Register the complete integration:

```csharp
using Mvp24Hours.Infrastructure.Identity.Keycloak.HealthChecks;
using Mvp24Hours.Infrastructure.Identity.Keycloak.WebAPI.Extensions;

builder.Services.AddKeycloakServices(builder.Configuration);
builder.Services.AddHealthChecks().AddKeycloakHealthCheck();

var app = builder.Build();
app.UseAuthentication();
app.UseKeycloakCurrentUser();
app.UseAuthorization();
app.MapHealthChecks("/health");
```

Use `AddKeycloakAuthentication`, `AddKeycloakAuthorization`, or `AddKeycloakAdminServices` when the application needs only part of the integration.

## Configuration

The default configuration section is `Keycloak`.

| Setting | Purpose | Default |
|---|---|---|
| `Authority` | Realm URL used as the JWT issuer and OIDC authority. | Required |
| `Realm` | Keycloak realm name. | Required |
| `ClientId` | OIDC client identifier. | Required |
| `ClientSecret` | Confidential-client secret used by token operations. | `null` |
| `Audience` | Expected access-token audience. | Required when audience validation is enabled |
| `RequireHttpsMetadata` | Rejects non-HTTPS authority metadata. | `true` |
| `ValidateIssuer` / `ValidateAudience` | Enables issuer and audience validation. | `true` |
| `TokenClockSkew` | Allowed JWT clock difference. | 5 seconds |
| `DiscoveryCacheTtl` | OIDC discovery document cache duration. | 24 hours |
| `MetadataAddress` | Optional discovery endpoint override. | `null` |

`Keycloak:Authorization` selects role, decision, or RPT behavior. Decision and RPT endpoints are mutually exclusive. `Keycloak:Admin` configures the Admin REST client and requires a confidential service-account client when `ServiceAccountEnabled` is `true`.

All options are validated at application startup. Keep client secrets outside source control.

## Authentication

`AddKeycloakAuthentication` configures ASP.NET Core JWT bearer authentication. It keeps original JWT claim names and maps realm and client roles to the configured role claim type.

```csharp
builder.Services.AddKeycloakAuthentication(builder.Configuration);

app.UseAuthentication();
app.UseKeycloakCurrentUser();
app.UseAuthorization();
```

Resolve `IKeycloakCurrentUser` to access the parsed Keycloak user, identifier, and authentication state. `IKeycloakTokenService` exposes client credentials, refresh, introspection, and revocation operations. Its password-grant operation is intended only for automated tests.

## Authorization

Role policies can use standard ASP.NET Core authorization:

```csharp
[Authorize(Roles = "orders-reader")]
app.MapGet("/orders", () => Results.Ok());
```

For Keycloak Authorization Services, register explicit resource/scope policies:

```csharp
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization.Decision;

builder.Services.AddKeycloakAuthorization(
    decisionRequirements: new()
    {
        ["orders:read"] = [new DecisionRequirement("orders", "read")]
    });
```

Use `RptRequirement` instead when `UseRptEndpoint` is enabled. `AddKeycloakPolicies(assembly)` can discover `resource#scope` policy names from authorization attributes.

## Admin API

`AddKeycloakServices` and `AddKeycloakAdminServices` register:

- `IKeycloakUserService` for user CRUD, passwords, roles, and group membership.
- `IKeycloakRoleService` for realm and client roles.
- `IKeycloakGroupService` for groups and their members.

```csharp
var result = await userService.CreateUserAsync(
    new CreateUserRequest
    {
        Username = "alex",
        Email = "alex@example.com",
        Enabled = true
    },
    cancellationToken);
```

Admin operations return `IBusinessResult<T>` so callers can handle Keycloak errors without depending on HTTP response types.

## User synchronization

Implement `IUserKeycloakService` for the application's local user store and register it:

```csharp
builder.Services.AddKeycloakUserSync<MyUserKeycloakService>();
```

After JWT validation, the integration checks for a local user and invokes `CreateOrUpdateLocalUserAsync` when needed. Implementations should be idempotent because authentication requests can run concurrently.

## Health

```csharp
builder.Services
    .AddHealthChecks()
    .AddKeycloakHealthCheck(
        name: "keycloak",
        tags: ["identity", "ready"],
        timeout: TimeSpan.FromSeconds(5));
```

The check resolves the OIDC discovery document and reports unhealthy when Keycloak metadata is unavailable or invalid.

## Testing

Unit tests do not require Keycloak:

```bash
dotnet test --filter "Category=Unit"
```

Integration tests use Testcontainers and require a running Docker engine:

```bash
dotnet test --filter "Category=Integration"
```

Use unique user, role, and group names in parallel integration tests and delete created resources during cleanup.

## License

Licensed under the [MIT License](../../LICENSE).
