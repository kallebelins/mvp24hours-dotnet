using App.Application.Logic;
using App.Core.Contract.Data;
using App.Core.Contract.Logic;
using App.Infrastructure.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddHttpClient("external-dependency")
            .AddStandardResilienceHandler();
        services.AddSingleton<IItemRepository, InMemoryItemRepository>();
        services.AddSingleton<IItemService, ItemService>();
    })
    .Build();

host.Run();
