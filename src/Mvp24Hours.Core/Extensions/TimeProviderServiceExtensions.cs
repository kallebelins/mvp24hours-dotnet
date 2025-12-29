//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
#nullable enable
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Core.Infrastructure.Clock;
using System;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for registering TimeProvider and IClock in DI container.
    /// </summary>
    /// <remarks>
    /// <para>
    /// TimeProvider is the .NET 8+ standard abstraction for time. These extensions help you:
    /// - Register TimeProvider.System for production
    /// - Register FakeTimeProvider for testing
    /// - Bridge between TimeProvider and legacy IClock code
    /// </para>
    /// </remarks>
    public static class TimeProviderServiceExtensions
    {
        /// <summary>
        /// Adds TimeProvider.System as singleton and bridges to IClock via TimeProviderAdapter.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the recommended way to register time abstractions in production.
        /// Both TimeProvider and IClock will be available via DI.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // In Program.cs
        /// builder.Services.AddTimeProvider();
        /// 
        /// // Use in services
        /// public class MyService
        /// {
        ///     private readonly TimeProvider _timeProvider;
        ///     private readonly IClock _clock;
        ///     
        ///     public MyService(TimeProvider timeProvider, IClock clock)
        ///     {
        ///         _timeProvider = timeProvider;
        ///         _clock = clock;
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddTimeProvider(this IServiceCollection services)
        {
            return services.AddTimeProvider(TimeProvider.System);
        }

        /// <summary>
        /// Adds a custom TimeProvider and bridges to IClock via TimeProviderAdapter.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this overload to register a custom TimeProvider (e.g., FakeTimeProvider for tests).
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // In test setup
        /// var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
        /// services.AddTimeProvider(fakeTime);
        /// 
        /// // Later in test
        /// fakeTime.Advance(TimeSpan.FromHours(1));
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <param name="timeProvider">The TimeProvider instance to register.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddTimeProvider(this IServiceCollection services, TimeProvider timeProvider)
        {
            if (timeProvider == null) throw new ArgumentNullException(nameof(timeProvider));

            // Register TimeProvider as singleton
            services.TryAddSingleton(timeProvider);

            // Register IClock adapter that wraps TimeProvider
            services.TryAddSingleton<IClock>(sp =>
            {
                var tp = sp.GetRequiredService<TimeProvider>();
                return new TimeProviderAdapter(tp);
            });

            return services;
        }

        /// <summary>
        /// Adds TimeProvider with a specific timezone for local time conversions.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="timeProvider">The TimeProvider instance.</param>
        /// <param name="localTimeZone">The timezone for local time conversions.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddTimeProvider(
            this IServiceCollection services,
            TimeProvider timeProvider,
            TimeZoneInfo localTimeZone)
        {
            if (timeProvider == null) throw new ArgumentNullException(nameof(timeProvider));
            if (localTimeZone == null) throw new ArgumentNullException(nameof(localTimeZone));

            services.TryAddSingleton(timeProvider);
            services.TryAddSingleton<IClock>(new TimeProviderAdapter(timeProvider, localTimeZone));

            return services;
        }

        /// <summary>
        /// Adds an existing IClock and bridges to TimeProvider via ClockAdapter.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this for backward compatibility when you have existing IClock implementations
        /// but need to support code that expects TimeProvider.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Register existing TestClock as both IClock and TimeProvider
        /// var testClock = new TestClock(DateTime.UtcNow);
        /// services.AddClock(testClock);
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <param name="clock">The IClock instance to register.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddClock(this IServiceCollection services, IClock clock)
        {
            if (clock == null) throw new ArgumentNullException(nameof(clock));

            // Register IClock as singleton
            services.TryAddSingleton(clock);

            // Register TimeProvider adapter that wraps IClock
            services.TryAddSingleton<TimeProvider>(sp =>
            {
                var c = sp.GetRequiredService<IClock>();
                return new ClockAdapter(c);
            });

            return services;
        }

        /// <summary>
        /// Adds SystemClock as IClock and bridges to TimeProvider.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is a convenience method equivalent to:
        /// <code>services.AddClock(SystemClock.Instance);</code>
        /// </para>
        /// </remarks>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddSystemClock(this IServiceCollection services)
        {
            return services.AddClock(SystemClock.Instance);
        }

        /// <summary>
        /// Replaces the current TimeProvider registration with a new instance.
        /// Useful for testing scenarios.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="timeProvider">The new TimeProvider to register.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection ReplaceTimeProvider(this IServiceCollection services, TimeProvider timeProvider)
        {
            if (timeProvider == null) throw new ArgumentNullException(nameof(timeProvider));

            // Remove existing registrations
            services.RemoveAll<TimeProvider>();
            services.RemoveAll<IClock>();

            // Add new registrations
            return services.AddTimeProvider(timeProvider);
        }

        /// <summary>
        /// Replaces the current IClock registration with a new instance.
        /// Useful for testing scenarios.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="clock">The new IClock to register.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection ReplaceClock(this IServiceCollection services, IClock clock)
        {
            if (clock == null) throw new ArgumentNullException(nameof(clock));

            // Remove existing registrations
            services.RemoveAll<TimeProvider>();
            services.RemoveAll<IClock>();

            // Add new registrations
            return services.AddClock(clock);
        }
    }
}

