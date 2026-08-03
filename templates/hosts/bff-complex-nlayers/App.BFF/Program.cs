using App.Application;
using App.Application.Logic;
using App.BFF.Extensions;
using App.Core.Contract.Logic;
using App.Core.Models;
using App.Core.Validations;
using FluentValidation;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Mvp24Hours.Extensions;
using Mvp24Hours.WebAPI.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTimeProvider();
builder.Services.AddMvp24HoursWebEssential();
builder.Services.AddMvp24HoursMapService(typeof(Item).Assembly);
builder.Services.AddMvp24HoursWebJson();
builder.Services.AddMvp24HoursNativeOpenApi(options =>
{
    options.Title = "App BFF Complex N-Layers";
    options.Version = "1.0.0";
    options.EnableSwaggerUI = true;
});
builder.Services.AddMvp24HoursWebGzip();
builder.Services.AddMyServices(builder.Configuration);
builder.Services.AddMyHealthChecks();
builder.Services.AddControllers();
builder.Services.AddNativeProblemDetailsAll(builder.Environment);

var app = builder.Build();

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
