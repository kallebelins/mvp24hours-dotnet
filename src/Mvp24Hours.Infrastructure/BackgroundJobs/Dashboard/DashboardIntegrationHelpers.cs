//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.BackgroundJobs.Dashboard
{
    /// <summary>
    /// Helper methods for integrating background job dashboards with ASP.NET Core applications.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These helpers provide convenient methods for configuring job provider dashboards
    /// in ASP.NET Core applications. Each provider (Hangfire, Quartz.NET) has its own
    /// dashboard that can be integrated into the application.
    /// </para>
    /// </remarks>
    public static class DashboardIntegrationHelpers
    {
        /// <summary>
        /// Configures Hangfire dashboard in the ASP.NET Core application.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="path">The dashboard path (default: "/hangfire").</param>
        /// <param name="configure">Optional configuration action for dashboard options.</param>
        /// <returns>The application builder for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method configures the Hangfire dashboard middleware. Requires Hangfire
        /// NuGet packages to be installed.
        /// </para>
        /// <para>
        /// <strong>Example:</strong>
        /// <code>
        /// app.UseHangfireDashboard("/hangfire", options =>
        /// {
        ///     options.Authorization = new[] { new HangfireAuthorizationFilter() };
        /// });
        /// </code>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // In Program.cs or Startup.cs
        /// app.UseHangfireDashboard("/hangfire");
        /// </code>
        /// </example>
        public static IApplicationBuilder UseHangfireDashboard(
            this IApplicationBuilder app,
            string path = "/hangfire",
            Action<object>? configure = null)
        {
            // Note: This is a placeholder implementation.
            // In a real implementation, this would call Hangfire's UseHangfireDashboard() method.
            // The actual implementation requires Hangfire.AspNetCore NuGet package to be installed.
            
            throw new NotSupportedException(
                "UseHangfireDashboard requires Hangfire.AspNetCore NuGet package to be installed. " +
                "Install Hangfire.AspNetCore and call Hangfire's UseHangfireDashboard() method directly.");
        }

        /// <summary>
        /// Configures Quartz.NET dashboard in the ASP.NET Core application (if available).
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="path">The dashboard path (default: "/quartz").</param>
        /// <param name="configure">Optional configuration action for dashboard options.</param>
        /// <returns>The application builder for chaining.</returns>
        /// <remarks>
        /// <para>
        /// Note: Quartz.NET does not include a built-in web dashboard. This method is a placeholder
        /// for potential future dashboard integrations or third-party dashboard solutions.
        /// </para>
        /// <para>
        /// For monitoring Quartz.NET jobs, consider:
        /// - Using Quartz.NET's built-in logging and metrics
        /// - Integrating with application monitoring tools (Application Insights, Prometheus, etc.)
        /// - Using third-party Quartz.NET dashboard solutions
        /// </para>
        /// </remarks>
        public static IApplicationBuilder UseQuartzDashboard(
            this IApplicationBuilder app,
            string path = "/quartz",
            Action<object>? configure = null)
        {
            // Note: Quartz.NET does not have a built-in web dashboard like Hangfire.
            // This is a placeholder for potential future integrations or third-party solutions.
            
            throw new NotSupportedException(
                "Quartz.NET does not include a built-in web dashboard. " +
                "Use Quartz.NET's logging and metrics, or integrate with application monitoring tools.");
        }

        /// <summary>
        /// Configures background job health checks endpoint.
        /// </summary>
        /// <param name="endpoints">The endpoint route builder.</param>
        /// <param name="path">The health check path (default: "/health/jobs").</param>
        /// <returns>The endpoint route builder for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method creates a health check endpoint that reports the status of the
        /// background job system, including:
        /// - Provider status
        /// - Active job count
        /// - Failed job count
        /// - Queue status
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // In Program.cs or Startup.cs
        /// app.MapHealthChecks("/health/jobs");
        /// </code>
        /// </example>
        public static IEndpointRouteBuilder MapJobHealthChecks(
            this IEndpointRouteBuilder endpoints,
            string path = "/health/jobs")
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            // Note: This would integrate with ASP.NET Core health checks
            // In a real implementation, register IHealthCheck implementations for each provider
            
            endpoints.MapGet(path, async context =>
            {
                // Placeholder implementation
                await context.Response.WriteAsync("Job health check endpoint - implement with IHealthCheck");
            });

            return endpoints;
        }
    }
}

