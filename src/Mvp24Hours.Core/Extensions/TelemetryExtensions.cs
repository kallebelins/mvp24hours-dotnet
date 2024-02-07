//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Infrastructure.Logging;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using System;

namespace Mvp24Hours.Extensions
{
    public static class TelemetryExtensions
    {
        public static IServiceCollection AddMvp24HoursTelemetry(this IServiceCollection services, TelemetryLevels level, params Action<string>[] actions)
        {
            TelemetryHelper.Add(level, actions);
            return services;
        }

        public static IServiceCollection AddMvp24HoursTelemetry(this IServiceCollection services, TelemetryLevels level, params Action<string, object[]>[] actions)
        {
            TelemetryHelper.Add(level, actions);
            return services;
        }

        public static IServiceCollection AddMvp24HoursTelemetry(this IServiceCollection services, TelemetryLevels level, params ITelemetryService[] telemetryServices)
        {
            TelemetryHelper.Add(level, telemetryServices);
            return services;
        }

        public static IServiceCollection AddMvp24HoursTelemetryFiltered(this IServiceCollection services, string serviceName, params Action<string>[] actions)
        {
            TelemetryHelper.AddFilter(serviceName, actions);
            return services;
        }

        public static IServiceCollection AddMvp24HoursTelemetryFiltered(this IServiceCollection services, string serviceName, params Action<string, object[]>[] actions)
        {
            TelemetryHelper.AddFilter(serviceName, actions);
            return services;
        }

        public static IServiceCollection AddMvp24HoursTelemetryFiltered(this IServiceCollection services, string serviceName, params ITelemetryService[] telemetryServices)
        {
            TelemetryHelper.AddFilter(serviceName, telemetryServices);
            return services;
        }

        public static IServiceCollection AddMvp24HoursTelemetryIgnore(this IServiceCollection services, params string[] ignoreServiceNames)
        {
            TelemetryHelper.AddIgnoreService(ignoreServiceNames);
            return services;
        }
    }
}
