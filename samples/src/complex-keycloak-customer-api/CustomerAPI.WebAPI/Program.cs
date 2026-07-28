using CustomerAPI.WebAPI.Extensions;
using HealthChecks.UI.Client;
using Mvp24Hours.Extensions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Mvp24Hours.Infrastructure.Identity.Keycloak.WebAPI.Extensions;
using Mvp24Hours.WebAPI.Extensions;
using NLog;
using NLog.Web;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    logger.Debug("Application Starting Up");

    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
    builder.Host.UseNLog();

    // Phase 1 patterns
    builder.Services.AddTimeProvider();
    builder.Services.AddMvp24HoursWebEssential();
    builder.Services.AddMvp24HoursWebJson();
    builder.Services.AddMvp24HoursNativeOpenApi(options =>
    {
        options.Title = "Complex Keycloak Customer API";
        options.Version = "1.0.0";
        options.EnableSwaggerUI = true;
    });
    builder.Services.AddMvp24HoursWebGzip();

    // Keycloak: JWT bearer validation + Admin REST services
    builder.Services.AddMyKeycloak(builder.Configuration);

    // Application services (in-memory store, etc.)
    builder.Services.AddMyServices();

    // Health checks (Keycloak OIDC probe)
    builder.Services.AddMyHealthChecks();

    builder.Services.AddControllers();
    builder.Services.AddNativeProblemDetailsAll(builder.Environment);

    var app = builder.Build();

    app.UseNativeProblemDetailsHandling();
    app.UseStaticFiles();
    app.UseRouting();

    // Authentication / Keycloak current-user context / Authorization
    app.UseAuthentication();
    app.UseKeycloakCurrentUser();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/hc", new HealthCheckOptions
    {
        Predicate = _ => true,
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    if (!app.Environment.IsProduction())
    {
        app.MapMvp24HoursNativeOpenApi();
    }

    await app.RunAsync();
}
catch (Exception exception)
{
    logger.Error(exception, "Stopped program because of exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}
