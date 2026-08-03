using App.Domain.Entities;
using App.Domain.Validations;
using App.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Extensions;

namespace App.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseName = configuration["Database:Name"] ?? "AppTemplate";
        services.AddDbContext<EFDBContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddMvp24HoursDbContext<EFDBContext>();
        services.AddMvp24HoursRepositoryAsync(options =>
        {
            options.MaxQtyByQueryPage = 100;
            options.TransactionIsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;
        });
        services.AddSingleton<IValidator<Item>, ItemValidator>();
        return services;
    }
}
