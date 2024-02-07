//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using AutoMapper;
using Mvp24Hours.Core.Contract.Mappings;
using Mvp24Hours.Extensions;
using System;
using System.Linq;
using System.Reflection;

namespace Mvp24Hours.Core.Mappings
{
    /// <summary>
    /// 
    /// </summary>
    public class MappingProfile : Profile
    {
        public MappingProfile(Assembly assembly)
        {
            ApplyMappingsFromAssembly(assembly);

            foreach (var assemblyName in assembly.GetReferencedAssemblies())
            {
                Assembly assemblyLoaded = Assembly.Load(assemblyName);
                ApplyMappingsFromAssembly(assemblyLoaded);
            }
        }

        private void ApplyMappingsFromAssembly(Assembly assembly)
        {
            var types = assembly.GetExportedTypes()
                .Where(t => t.GetInterfaces().AnySafe(i => i == typeof(IMapFrom)))
                .ToList();

            foreach (var type in types)
            {
                var instance = Activator.CreateInstance(type);
                var methodInfo = type.GetMethod("Mapping");
                methodInfo?.Invoke(instance, new object[] { this });
            }
        }
    }
}
