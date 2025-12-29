//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Mappings;
using Mvp24Hours.Helpers;
using System.Reflection;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add mapping services, use Assembly.GetExecutingAssembly()
        /// </summary>
        public static IServiceCollection AddMvp24HoursMapService(this IServiceCollection services, Assembly assemblyMap)
        {
            if (assemblyMap == null)
            {
                throw new System.ArgumentNullException(nameof(assemblyMap), "Assembly Map is required.");
            }

            Assembly local = assemblyMap;
            services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile(new MappingProfile(local));
            }, local);

            return services;
        }

        /// <summary>
        /// Add time zone 
        /// </summary>
        public static IServiceCollection AddMvp24HoursTimeZone(this IServiceCollection services, bool clearList, params string[] args)
        {
            if (clearList)
            {
                TimeZoneHelper.TimeZoneIds.Clear();
            }

            if (args.AnySafe())
            {
                TimeZoneHelper.TimeZoneIds.AddRange(args);
            }
            return services;
        }
    }
}
