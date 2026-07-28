using CustomerAPI.Application.Projections;
using CustomerAPI.Application.Services;
using CustomerAPI.Domain.Aggregates;
using CustomerAPI.WebAPI.Controllers;
using CustomerAPI.WebAPI.Validations;
using FluentValidation;
using Mvp24Hours.Infrastructure.Cqrs.EventSourcing;

namespace CustomerAPI.WebAPI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMyEventSourcing(this IServiceCollection services)
    {
        // Registers InMemoryEventStore, InMemorySnapshotStore, EventCountSnapshotStrategy,
        // IEventSerializer (JsonEventSerializer), and IEventTypeResolver (DefaultEventTypeResolver)
        services.AddEventSourcingInMemory();

        // Registers IEventStoreRepository<CustomerAggregate> — wires event store + snapshot support
        services.AddEventStoreRepository<CustomerAggregate>();

        return services;
    }

    public static IServiceCollection AddMyProjection(this IServiceCollection services)
    {
        // Singleton projection — lives for the lifetime of the process
        services.AddSingleton<CustomerProjection>();
        return services;
    }

    public static IServiceCollection AddMyServices(this IServiceCollection services)
    {
        services.AddScoped<CustomerEventStoreService>();

        services.AddSingleton<IValidator<CreateCustomerRequest>, CreateCustomerRequestValidator>();
        services.AddSingleton<IValidator<RenameCustomerRequest>, RenameCustomerRequestValidator>();

        return services;
    }
}
