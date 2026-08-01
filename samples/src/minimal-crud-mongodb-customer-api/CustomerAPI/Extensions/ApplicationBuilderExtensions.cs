using CustomerAPI.Data;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.WebAPI.Extensions;

namespace CustomerAPI.Extensions;

/// <summary>
/// Configures the Minimal API host middleware and development seed hooks.
/// </summary>
public static class ApplicationBuilderExtensions
{
    public static WebApplication Configure(this WebApplication app)
    {
        app.UseNativeProblemDetailsHandling();
        app.MapHealthChecks("/hc", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        return app;
    }

    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        if (app.Environment.IsEnvironment("Testing"))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        await unitOfWork.SeedAsync(timeProvider);
    }
}
