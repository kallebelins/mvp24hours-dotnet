using App.Core.Entities;
using App.Infrastructure.Data;
using App.WebAPI.Extensions;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Extensions;
using Mvp24Hours.WebAPI.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTimeProvider();
builder.Services.AddMvp24HoursWebEssential();
builder.Services.AddMvp24HoursMapService(typeof(Item).Assembly);
builder.Services.AddMvp24HoursWebJson();
builder.Services.AddMvp24HoursNativeOpenApi(options =>
{
    options.Title = "App Complex N-Layers API";
    options.Version = "1.0.0";
    options.EnableSwaggerUI = true;
});
builder.Services.AddMvp24HoursWebGzip();
builder.Services.AddMyServices();
builder.Services.AddMyDbContext(builder.Configuration);
builder.Services.AddMyHealthChecks();
builder.Services.AddControllers();
builder.Services.AddNativeProblemDetailsAll(builder.Environment);

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<EFDBContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseNativeProblemDetailsHandling();
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

public partial class Program { }
