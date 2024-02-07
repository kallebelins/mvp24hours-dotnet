//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class ServiceCollectionExtentions
    {
        /// <summary>
        /// Checks if the type has already been added to the service collection
        /// </summary>
        public static bool Exists<T>(this IServiceCollection services)
        {
            return services.Exists(typeof(T));
        }

        /// <summary>
        /// Checks if the type has already been added to the service collection
        /// </summary>
        public static bool Exists(this IServiceCollection services, Type type)
        {
            return services.Any(descriptor => descriptor.ServiceType == type);
        }

        /// <summary>
        /// Remove type added to service collection
        /// </summary>
        public static IServiceCollection Remove<T>(this IServiceCollection services)
        {
            services.Remove(typeof(T));
            return services;
        }

        /// <summary>
        /// Remove type added to service collection
        /// </summary>
        public static IServiceCollection Remove(this IServiceCollection services, Type type)
        {
            var serviceDescriptor = services.FirstOrDefault(descriptor => descriptor.ServiceType == type);
            if (serviceDescriptor != null)
            {
                services.Remove(serviceDescriptor);
            }

            return services;
        }

        /// <summary>
        /// Search and add all instances defined by the type entered
        /// <code>services.AddAllTypes<IGenerator>(new[] { typeof(GenerateInvoiceHandler).GetTypeInfo().Assembly }</code>
        /// </summary>
        public static void AddAllTypes<T>(this IServiceCollection services
            , Assembly[] assemblies
            , bool additionalRegisterTypesByThemself = false
            , ServiceLifetime lifetime = ServiceLifetime.Transient
        )
        {
            var typesFromAssemblies = assemblies.SelectMany(a =>
                a.DefinedTypes.Where(x => x.GetInterfaces().AnySafe(i => i == typeof(T))));
            foreach (var type in typesFromAssemblies)
            {
                services.Add(new ServiceDescriptor(typeof(T), type, lifetime));
                if (additionalRegisterTypesByThemself)
                {
                    services.Add(new ServiceDescriptor(type, type, lifetime));
                }
            }
        }

        /// <summary>
        /// Search and add all instances defined by the type entered
        /// <code>services.AddAllGenericTypes(typeof(IRequest<>), new[] {typeof(GenerateInvoiceHandler).GetTypeInfo().Assembly})</code>
        /// </summary>
        public static void AddAllGenericTypes(this IServiceCollection services
            , Type t
            , Assembly[] assemblies
            , bool additionalRegisterTypesByThemself = false
            , ServiceLifetime lifetime = ServiceLifetime.Transient
        )
        {
            var genericType = t;
            var typesFromAssemblies = assemblies.SelectMany(a => a.DefinedTypes.Where(x => x.GetInterfaces()
                .AnySafe(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericType)));

            foreach (var type in typesFromAssemblies)
            {
                services.Add(new ServiceDescriptor(t, type, lifetime));
                if (additionalRegisterTypesByThemself)
                {
                    services.Add(new ServiceDescriptor(type, type, lifetime));
                }
            }
        }
    }
}
