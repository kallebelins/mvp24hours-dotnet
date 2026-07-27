using CustomerAPI.WebAPI.Extensions;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Extensions;
using Mvp24Hours.WebAPI.Extensions;
using NLog.Web;
using NLog;



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
        options.Title = "Customer Pipeline Ports and Adapters API";
        options.Version = "1.0.0";
        options.EnableSwaggerUI = true;
    });
    builder.Services.AddMvp24HoursWebGzip();
    builder.Services.AddMvp24HoursPipelineAsync(options =>
    {
        options.IsBreakOnFail = true;
    });
    builder.Services.AddMyOptions(builder.Configuration);
    builder.Services.AddMyServices();
    builder.Services.AddMyHealthChecks();
    builder.Services.AddControllers();
    builder.Services.AddMvc();
    builder.Services.AddNativeProblemDetailsAll(builder.Environment);



    var app = builder.Build();



    app.UseNativeProblemDetailsHandling();
    app.UseStaticFiles();
    app.UseRouting();
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
