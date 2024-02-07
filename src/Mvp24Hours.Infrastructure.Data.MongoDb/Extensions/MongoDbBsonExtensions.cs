//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Infrastructure.Data.MongoDb;
using System;
using System.Linq;
using System.Reflection;

namespace Mvp24Hours.Extensions
{
    public static class MongoDbBsonExtensions
    {
        /// <summary>
        /// Apply configuration models
        /// </summary>
        public static Mvp24HoursContext ApplyConfigurationsFromAssembly(this Mvp24HoursContext context, Assembly assembly)
        {
            var types = assembly.GetExportedTypes()
                .Where(t => t.InheritsOrImplements(typeof(IBsonClassMap)))
                .ToList();

            foreach (var type in types)
            {
                var instance = Activator.CreateInstance(type);
                var methodInfo = type.GetMethod("Configure");
                methodInfo?.Invoke(instance, null);
            }

            return context;
        }
    }
}
