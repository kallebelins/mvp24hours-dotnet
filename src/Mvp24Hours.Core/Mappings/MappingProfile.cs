//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Linq;
using System.Reflection;
using AutoMapper;
using Mvp24Hours.Core.Contract.Mappings;
using Mvp24Hours.Extensions;

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

            foreach (AssemblyName assemblyName in assembly.GetReferencedAssemblies())
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

            foreach (Type? type in types)
            {
                var instance = Activator.CreateInstance(type);
                MethodInfo? methodInfo = type.GetMethod("Mapping");
                methodInfo?.Invoke(instance, new object[] { this });
            }
        }
    }
}
