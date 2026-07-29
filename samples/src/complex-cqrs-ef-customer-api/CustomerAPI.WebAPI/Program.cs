using CustomerAPI.Core.Entities;
using CustomerAPI.Infrastructure.Data;
using CustomerAPI.WebAPI.Extensions;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Extensions;
using Mvp24Hours.WebAPI.Extensions;
using NLog;
using NLog.Web;



Logger logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();



try
{
    logger.Debug("Application Starting Up");



    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);



    builder.Logging.ClearProviders();
    builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
    builder.Host.UseNLog();



    builder.Services.AddTimeProvider();
    builder.Services.AddMvp24HoursWebEssential();
    builder.Services.AddMvp24HoursMapService(typeof(Customer).Assembly);
    builder.Services.AddMvp24HoursWebJson();
    builder.Services.AddMvp24HoursNativeOpenApi(options =>
    {
        options.Title = "Customer CQRS EF API";
        options.Version = "1.0.0";
        options.EnableSwaggerUI = true;
    });
    builder.Services.AddMvp24HoursWebGzip();
    builder.Services.AddMyOptions(builder.Configuration);
    builder.Services.AddMyServices();
    builder.Services.AddMyDbContext(builder.Configuration);
    builder.Services.AddMyHealthChecks(builder.Configuration);
    builder.Services.AddControllers();
    builder.Services.AddNativeProblemDetailsAll(builder.Environment);



    WebApplication app = builder.Build();



    if (!app.Environment.IsEnvironment("Testing"))
    {
        await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
        {
            EFDBContext db = scope.ServiceProvider.GetRequiredService<EFDBContext>();
            TimeProvider timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
            await db.Database.MigrateAsync();
            await EFDBContextSeed.SeedAsync(db, timeProvider);
        }
    }
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

public partial class Program { }
