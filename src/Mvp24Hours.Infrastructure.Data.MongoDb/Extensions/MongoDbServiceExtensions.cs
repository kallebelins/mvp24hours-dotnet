//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Infrastructure.Data.MongoDb;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using System;

namespace Mvp24Hours.Extensions
{
    public static class MongoDbServiceExtensions
    {
        /// <summary>
        /// Add database context services
        /// </summary>
        public static IServiceCollection AddMvp24HoursDbContext(this IServiceCollection services,
            Action<MongoDbOptions> options = null,
            Func<IServiceProvider, Mvp24HoursContext> dbFactory = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            return services.AddMvp24HoursDbContext<Mvp24HoursContext>(options, dbFactory, lifetime);
        }

        /// <summary>
        /// Add database context services
        /// </summary>
        public static IServiceCollection AddMvp24HoursDbContext<DbContext>(this IServiceCollection services,
            Action<MongoDbOptions> options = null,
            Func<IServiceProvider, DbContext> dbFactory = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped) where DbContext : Mvp24HoursContext
        {
            if (options != null)
            {
                services.Configure(options);
            }
            else
            {
                services.Configure<MongoDbOptions>(options => { });
            }

            if (dbFactory != null)
            {
                services.Add(new ServiceDescriptor(typeof(Mvp24HoursContext), dbFactory, lifetime));
            }
            else
            {
                services.Add(new ServiceDescriptor(typeof(Mvp24HoursContext), typeof(DbContext), lifetime));
            }

            return services;
        }

        /// <summary>
        /// Add repository
        /// </summary>
        public static IServiceCollection AddMvp24HoursRepository(this IServiceCollection services,
            Action<MongoDbRepositoryOptions> repositoryOptions = null,
            Type repository = null,
            Type unitOfWork = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            if (repositoryOptions != null)
            {
                services.Configure(repositoryOptions);
            }
            else
            {
                services.Configure<MongoDbRepositoryOptions>(repositoryOptions => { });
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
            Action<MongoDbRepositoryOptions> repositoryOptions = null,
            Type repositoryAsync = null,
            Type unitOfWorkAsync = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            if (repositoryOptions != null)
            {
                services.Configure(repositoryOptions);
            }
            else
            {
                services.Configure<MongoDbRepositoryOptions>(repositoryOptions => { });
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
