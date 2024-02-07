//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.Redis.Test.Support.Entities;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Caching;
using System;

namespace Mvp24Hours.Application.Redis.Test.Setup
{
    public static class Startup
    {
        public static IServiceProvider Initialize()
        {
            var services = new ServiceCollection()
                .AddSingleton(ConfigurationHelper.AppSettings);

            // caching
            services.AddScoped<IRepositoryCache<Customer>, RepositoryCache<Customer>>();
            services.AddScoped<IRepositoryCacheAsync<Customer>, RepositoryCacheAsync<Customer>>();

            // caching.redis
            services.AddMvp24HoursCaching();
            services.AddMvp24HoursCachingRedis(ConfigurationHelper.GetSettings("ConnectionStrings:RedisDbContext"));

            return services.BuildServiceProvider();
        }
    }
}
