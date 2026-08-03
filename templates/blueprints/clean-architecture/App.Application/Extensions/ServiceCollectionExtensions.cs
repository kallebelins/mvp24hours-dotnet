using App.Application.Items.Commands.CreateItem;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.Cqrs.Extensions;

namespace App.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssemblyContaining<CreateItemCommandHandler>();
            options.WithDefaultBehaviors();
            options.RegisterValidationBehavior = true;
        });
        services.AddSingleton<IValidator<CreateItemCommand>, CreateItemCommandValidator>();
        return services;
    }
}
