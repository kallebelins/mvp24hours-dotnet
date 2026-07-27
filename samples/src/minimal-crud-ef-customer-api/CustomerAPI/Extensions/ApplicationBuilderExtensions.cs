using CustomerAPI.Data;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.WebAPI.Extensions;

namespace CustomerAPI.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// 
        /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
        public static async Task MigrateDatabaseAsync(this WebApplication app)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<EFDBContext>();
            var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
            await db.Database.EnsureCreatedAsync();
            db.Seed(timeProvider);
        }
    }
}
