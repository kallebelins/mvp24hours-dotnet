using CustomerAPI.WebAPI.Extensions;
using Mvp24Hours.Extensions;
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

    builder.Services.AddTimeProvider();
    builder.Services.AddMvp24HoursWebEssential();
    builder.Services.AddMvp24HoursWebJson();
    builder.Services.AddMvp24HoursNativeOpenApi(options =>
    {
        options.Title = "Complex Event Sourcing Customer API";
        options.Version = "1.0.0";
        options.EnableSwaggerUI = true;
    });
    builder.Services.AddMvp24HoursWebGzip();

    builder.Services.AddMyEventSourcing();
    builder.Services.AddMyProjection();
    builder.Services.AddMyServices();

    builder.Services.AddControllers();
    builder.Services.AddNativeProblemDetailsAll(builder.Environment);

    var app = builder.Build();

    app.UseNativeProblemDetailsHandling();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthorization();

    app.MapControllers();

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

public partial class Program { }
