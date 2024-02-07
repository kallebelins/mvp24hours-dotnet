//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Data.EFCore;
using Mvp24Hours.Infrastructure.Data.EFCore.Configuration;
using System;

namespace Mvp24Hours.Extensions
{
    public static class EFCoreServiceExtensions
    {
        /// <summary>
        /// Add database context
        /// </summary>
        public static IServiceCollection AddMvp24HoursDbContext<TDbContext>(this IServiceCollection services,
            Func<IServiceProvider, TDbContext> dbFactory = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped) where TDbContext : DbContext
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcoreserviceextensions-addmvp24hoursdbcontext-execute");

            if (dbFactory != null)
            {
                services.Add(new ServiceDescriptor(typeof(DbContext), dbFactory, lifetime));
            }
            else
            {
                services.Add(new ServiceDescriptor(typeof(DbContext), typeof(TDbContext), lifetime));
            }

            return services;
        }

        /// <summary>
        /// Add repository
        /// </summary>
        public static IServiceCollection AddMvp24HoursRepository(this IServiceCollection services,
            Action<EFCoreRepositoryOptions> options = null,
            Type repository = null,
            Type unitOfWork = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcoreserviceextensions-addmvp24hoursrepository-execute");

            if (options != null)
            {
                services.Configure(options);
            }
            else
            {
                services.Configure<EFCoreRepositoryOptions>(options => { });
            }

            if (unitOfWork != null)
            {
                services.Add(new ServiceDescriptor(typeof(IUnitOfWork), unitOfWork, lifetime));
            }
            else
            {
                services.Add(new ServiceDescriptor(typeof(IUnitOfWork), typeof(UnitOfWork), lifetime));
            }

            if (repository != null)
            {
                services.Add(new ServiceDescriptor(typeof(IRepository<>), repository, lifetime));
            }
            else
            {
                services.Add(new ServiceDescriptor(typeof(IRepository<>), typeof(Repository<>), lifetime));
            }

            return services;
        }

        /// <summary>
        /// Add repository
        /// </summary>
        public static IServiceCollection AddMvp24HoursRepositoryAsync(this IServiceCollection services,
            Action<EFCoreRepositoryOptions> options = null,
            Type repositoryAsync = null,
            Type unitOfWorkAsync = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcoreserviceextensions-addmvp24hoursrepositoryasync-execute");

            if (options != null)
            {
                services.Configure(options);
            }
            else
            {
                services.Configure<EFCoreRepositoryOptions>(options => { });
            }

            if (unitOfWorkAsync != null)
            {
                services.Add(new ServiceDescriptor(typeof(IUnitOfWorkAsync), unitOfWorkAsync, lifetime));
            }
            else
            {
                services.Add(new ServiceDescriptor(typeof(IUnitOfWorkAsync), typeof(UnitOfWorkAsync), lifetime));
            }

            if (repositoryAsync != null)
            {
                services.Add(new ServiceDescriptor(typeof(IRepositoryAsync<>), repositoryAsync, lifetime));
            }
            else
            {
                services.Add(new ServiceDescriptor(typeof(IRepositoryAsync<>), typeof(RepositoryAsync<>), lifetime));
            }

            return services;
        }
    }
}
