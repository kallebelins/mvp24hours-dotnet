using CustomerAPI.Core.DTOs.Admin;
using CustomerAPI.Core.Validations.Admin;
using CustomerAPI.WebAPI.Data;
using FluentValidation;
using Mvp24Hours.Infrastructure.Identity.Keycloak.HealthChecks;
using Mvp24Hours.Infrastructure.Identity.Keycloak.WebAPI.Extensions;

namespace CustomerAPI.WebAPI.Extensions;

/// <summary>
/// Registers sample-specific services, Keycloak identity, and health checks.
/// </summary>
public static class ServiceBuilderExtensions
{
    /// <summary>
    /// Registers Keycloak authentication, authorization, and Admin API services.
    /// </summary>
    public static IServiceCollection AddMyKeycloak(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddKeycloakServices(configuration);
        return services;
    }

    /// <summary>
    /// Registers in-memory store and other application-level services.
    /// </summary>
    public static IServiceCollection AddMyServices(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryCustomerStore>();

        services.AddSingleton<IValidator<CreateKeycloakUserDto>, CreateKeycloakUserDtoValidator>();
        services.AddSingleton<IValidator<ResetPasswordDto>, ResetPasswordDtoValidator>();
        services.AddSingleton<IValidator<AssignRoleDto>, AssignRoleDtoValidator>();

        return services;
    }

    /// <summary>
    /// Registers health checks including the Keycloak OIDC discovery probe.
    /// </summary>
    public static IServiceCollection AddMyHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddKeycloakHealthCheck(
                name: "keycloak",
                failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
                tags: ["identity"]);
        return services;
    }
}
