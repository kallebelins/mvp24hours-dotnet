using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Mvp24Hours.Extensions;
using Mvp24Hours.WebAPI.Extensions;
using NLog;
using NLog.Web;
using WebStatus.Extensions;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    logger.Debug("WebStatus starting up");

    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
    builder.Host.UseNLog();

    builder.Services.AddTimeProvider();
    builder.Services.AddMvp24HoursWebEssential();
    builder.Services.AddMvp24HoursWebJson();
    builder.Services.AddMvp24HoursNativeOpenApi(options =>
    {
        options.Title = "Mvp24Hours WebStatus";
        options.Version = "1.0.0";
        options.EnableSwaggerUI = true;
    });
    builder.Services.AddMyOptions(builder.Configuration);
    builder.Services.AddMyHealthCatalog(builder.Configuration);
    builder.Services.AddNativeProblemDetailsAll(builder.Environment);

    var app = builder.Build();

    app.UseNativeProblemDetailsHandling();
    app.UseRouting();

    app.UseMvp24HoursHealthChecks();

    app.MapHealthChecks("/hc", new HealthCheckOptions
    {
        Predicate = _ => true,
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapHealthChecksUI(options => options.UIPath = "/healthchecks-ui");

    if (!app.Environment.IsProduction())
    {
        app.MapMvp24HoursNativeOpenApi();
    }

    app.MapGet("/", () => Results.Redirect("/healthchecks-ui"));

    await app.RunAsync();
}
catch (Exception exception)
{
    logger.Error(exception, "WebStatus stopped because of an exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}
