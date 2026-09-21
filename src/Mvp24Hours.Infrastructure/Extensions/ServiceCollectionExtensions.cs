//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Mappings;
using Mvp24Hours.Helpers;

namespace Mvp24Hours.Extensions;

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
        services.AddAutoMapper(cfg => cfg.AddProfile(new MappingProfile(local)), local);

        return services;
    }

    /// <summary>
    /// Add time zone 
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Deprecated.</b> This method registers nothing in the container: it only mutates the static
    /// <c>TimeZoneHelper.TimeZoneIds</c> list, and <c>TimeZoneHelper</c> caches the resolved
    /// <see cref="TimeZoneInfo"/> on first use — so calling it after the first
    /// <c>GetTimeZoneNow()</c> has no effect. Register a clock with an explicit timezone instead:
    /// <c>services.AddTimeProvider(TimeProvider.System, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"))</c>,
    /// which registers both <c>IClock</c> and <c>TimeProvider</c>.
    /// </para>
    /// </remarks>
    [Obsolete("Use IClock (Mvp24Hours.Core.Contract.Infrastructure) or TimeProvider. Will be removed in v12.")]
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
