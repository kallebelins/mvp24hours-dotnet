//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Core.Contract.Domain.Specifications;
using Mvp24Hours.Core.Domain.Specifications;
using System;

namespace Mvp24Hours.Application.Extensions
{
    /// <summary>
    /// Extension methods for registering Specification Pattern services in the DI container.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Specification Pattern allows encapsulating query logic in reusable, composable objects.
    /// This extension provides methods to register specification evaluators that can be injected
    /// into services and repositories.
    /// </para>
    /// <para>
    /// <strong>Available Evaluators:</strong>
    /// <list type="bullet">
    /// <item><see cref="InMemorySpecificationEvaluator{T}"/> - Basic evaluator for in-memory collections</item>
    /// <item>SpecificationEvaluator&lt;T&gt; (EFCore) - Full-featured evaluator with Include support</item>
    /// <item>MongoDbSpecificationEvaluator&lt;T&gt; - MongoDB-specific evaluator</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class SpecificationServiceCollectionExtensions
    {
        #region [ In-Memory Evaluator Registration ]

        /// <summary>
        /// Registers the in-memory specification evaluator for all entity types.
        /// Use this for testing or in-memory data sources.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime (default: Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// The in-memory evaluator supports:
        /// <list type="bullet">
        /// <item>Where clause filtering via specification criteria</item>
        /// <item>OrderBy/ThenBy for sorting</item>
        /// <item>Skip/Take for pagination</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Note:</strong> The in-memory evaluator does NOT support Include statements
        /// as those are ORM-specific. Use EFCore or MongoDB evaluators for eager loading.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Register in Startup.cs or Program.cs
        /// services.AddMvp24HoursInMemorySpecificationEvaluator();
        /// 
        /// // Use in service
        /// public class ProductService
        /// {
        ///     private readonly ISpecificationEvaluator&lt;Product&gt; _evaluator;
        ///     
        ///     public ProductService(ISpecificationEvaluator&lt;Product&gt; evaluator)
        ///     {
        ///         _evaluator = evaluator;
        ///     }
        ///     
        ///     public IList&lt;Product&gt; GetActiveProducts(IQueryable&lt;Product&gt; products)
        ///     {
        ///         var spec = new ActiveProductSpecification();
        ///         return _evaluator.GetQuery(products, spec).ToList();
        ///     }
        /// }
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursInMemorySpecificationEvaluator(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            // Register the generic evaluator
            services.TryAdd(new ServiceDescriptor(
                typeof(ISpecificationEvaluator<>),
                typeof(InMemorySpecificationEvaluator<>),
                lifetime));

            // Register the non-generic evaluator
            services.TryAdd(new ServiceDescriptor(
                typeof(ISpecificationEvaluator),
                typeof(InMemorySpecificationEvaluator),
                lifetime));

            return services;
        }

        /// <summary>
        /// Registers the in-memory specification evaluator as a singleton (using default instances).
        /// This is more efficient when state is not needed.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursInMemorySpecificationEvaluatorSingleton();
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursInMemorySpecificationEvaluatorSingleton(
            this IServiceCollection services)
        {
            // Register the non-generic evaluator as singleton using default instance
            services.TryAddSingleton<ISpecificationEvaluator>(InMemorySpecificationEvaluator.Default);

            return services;
        }

        #endregion

        #region [ Custom Evaluator Registration ]

        /// <summary>
        /// Registers a custom specification evaluator implementation.
        /// </summary>
        /// <typeparam name="TImplementation">The evaluator implementation type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime (default: Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// Use this method to register data-access specific evaluators:
        /// <list type="bullet">
        /// <item>EFCore: SpecificationEvaluator&lt;T&gt; with Include support</item>
        /// <item>MongoDB: MongoDbSpecificationEvaluator&lt;T&gt; with aggregation support</item>
        /// <item>Custom: Your own evaluator implementation</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Register EFCore evaluator (requires Mvp24Hours.Infrastructure.Data.EFCore)
        /// services.AddMvp24HoursSpecificationEvaluator&lt;SpecificationEvaluator&lt;&gt;&gt;();
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursSpecificationEvaluator<TImplementation>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TImplementation : class
        {
            var implementationType = typeof(TImplementation);

            // Check if it's a generic type definition
            if (implementationType.IsGenericTypeDefinition)
            {
                // Register as open generic
                services.TryAdd(new ServiceDescriptor(
                    typeof(ISpecificationEvaluator<>),
                    implementationType,
                    lifetime));
            }
            else
            {
                // Register as closed generic - extract the entity type
                var interfaces = implementationType.GetInterfaces();
                foreach (var @interface in interfaces)
                {
                    if (@interface.IsGenericType &&
                        @interface.GetGenericTypeDefinition() == typeof(ISpecificationEvaluator<>))
                    {
                        services.TryAdd(new ServiceDescriptor(@interface, implementationType, lifetime));
                        break;
                    }
                }
            }

            return services;
        }

        /// <summary>
        /// Registers a custom specification evaluator implementation with a factory.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="factory">The factory function to create the evaluator.</param>
        /// <param name="lifetime">The service lifetime (default: Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursSpecificationEvaluator&lt;Customer&gt;(
        ///     sp => new CustomSpecificationEvaluator&lt;Customer&gt;(sp.GetRequiredService&lt;ILogger&gt;())
        /// );
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursSpecificationEvaluator<TEntity>(
            this IServiceCollection services,
            Func<IServiceProvider, ISpecificationEvaluator<TEntity>> factory,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TEntity : class
        {
            services.TryAdd(new ServiceDescriptor(
                typeof(ISpecificationEvaluator<TEntity>),
                sp => factory(sp),
                lifetime));

            return services;
        }

        /// <summary>
        /// Registers a singleton specification evaluator instance.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="instance">The evaluator instance to register.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursSpecificationEvaluatorInstance&lt;Customer&gt;(
        ///     SpecificationEvaluator&lt;Customer&gt;.Default
        /// );
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursSpecificationEvaluatorInstance<TEntity>(
            this IServiceCollection services,
            ISpecificationEvaluator<TEntity> instance)
            where TEntity : class
        {
            ArgumentNullException.ThrowIfNull(instance);

            services.TryAddSingleton(instance);

            return services;
        }

        #endregion

        #region [ Combined Registration ]

        /// <summary>
        /// Registers all specification pattern services including evaluator and combinators.
        /// This is a convenience method for complete specification pattern setup.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="useInMemoryEvaluator">
        /// If true, registers the in-memory evaluator.
        /// Set to false if you're registering a database-specific evaluator separately.
        /// </param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers:
        /// <list type="bullet">
        /// <item>ISpecificationEvaluator&lt;T&gt; - For applying specifications to queries</item>
        /// <item>ISpecificationEvaluator (non-generic) - For generic query operations</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Note:</strong> The <see cref="Specifications.SpecificationCombinators"/> class provides
        /// static methods for combining specifications and doesn't require DI registration.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // For in-memory/testing scenarios
        /// services.AddMvp24HoursSpecificationPattern();
        /// 
        /// // For database scenarios (register evaluator separately)
        /// services.AddMvp24HoursSpecificationPattern(useInMemoryEvaluator: false);
        /// services.AddMvp24HoursSpecificationEvaluator&lt;SpecificationEvaluator&lt;&gt;&gt;();
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursSpecificationPattern(
            this IServiceCollection services,
            bool useInMemoryEvaluator = true)
        {
            if (useInMemoryEvaluator)
            {
                services.AddMvp24HoursInMemorySpecificationEvaluator();
            }

            return services;
        }

        #endregion
    }
}

