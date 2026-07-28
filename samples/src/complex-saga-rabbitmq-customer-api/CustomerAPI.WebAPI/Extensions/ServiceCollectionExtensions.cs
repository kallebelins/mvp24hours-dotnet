using CustomerAPI.Application.Repositories;
using CustomerAPI.Application.Sagas;
using CustomerAPI.Domain.Repositories;
using Mvp24Hours.Infrastructure.Cqrs.Saga;

namespace CustomerAPI.WebAPI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMyServices(this IServiceCollection services)
    {
        // In-memory customer store (replace with EF Core / another provider for production)
        services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
        return services;
    }

    public static IServiceCollection AddMySaga(this IServiceCollection services)
    {
        services.AddSagaOrchestration(options =>
        {
            options.UseInMemoryStateStore();
            options.RegisterSagasFromAssemblyContaining<OnboardCustomerSaga>();
            // Disable the background retry/timeout poller — not needed for this sample
            options.DisableBackgroundService();
        });
        return services;
    }
}
