//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Serialization.Json;
using Mvp24Hours.Core.Serialization.SourceGeneration;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Mvp24Hours.Core.Extensions.SourceGeneration;

/// <summary>
/// Extension methods for configuring source-generated serialization and logging.
/// </summary>
/// <remarks>
/// <para>
/// Source generators provide:
/// <list type="bullet">
/// <item><description>AOT (Ahead-of-Time) compilation compatibility</description></item>
/// <item><description>Better performance (no runtime reflection)</description></item>
/// <item><description>Smaller application size (trimming friendly)</description></item>
/// </list>
/// </para>
/// </remarks>
public static class SourceGenerationServiceExtensions
{
    /// <summary>
    /// Adds the Mvp24Hours source-generated JSON serialization options to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursJsonSourceGeneration();
    /// 
    /// // Then inject and use:
    /// public class MyService
    /// {
    ///     private readonly JsonSerializerOptions _options;
    ///     
    ///     public MyService(JsonSerializerOptions options)
    ///     {
    ///         _options = options;
    ///     }
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursJsonSourceGeneration(this IServiceCollection services)
    {
        var options = Mvp24HoursJsonSerializerContext.CreateOptions();

        // Add EntityId converter factory for strongly-typed IDs
        options.Converters.Add(new EntityIdJsonConverterFactory());

        services.AddSingleton(options);
        services.AddSingleton(Mvp24HoursJsonSerializerContext.Default);
        services.AddSingleton<IJsonTypeInfoResolver>(Mvp24HoursJsonSerializerContext.Default);

        return services;
    }

    /// <summary>
    /// Adds the Mvp24Hours source-generated JSON serialization with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure additional options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursJsonSourceGeneration(options =>
    /// {
    ///     options.WriteIndented = true;
    ///     options.Converters.Add(new MyCustomConverter());
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursJsonSourceGeneration(
        this IServiceCollection services,
        Action<JsonSerializerOptions> configure)
    {
        var options = Mvp24HoursJsonSerializerContext.CreateOptions();
        options.Converters.Add(new EntityIdJsonConverterFactory());

        configure(options);

        services.AddSingleton(options);
        services.AddSingleton(Mvp24HoursJsonSerializerContext.Default);
        services.AddSingleton<IJsonTypeInfoResolver>(Mvp24HoursJsonSerializerContext.Default);

        return services;
    }

    /// <summary>
    /// Adds additional JsonSerializerContext to the type info resolver chain.
    /// </summary>
    /// <typeparam name="TContext">The JsonSerializerContext type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="context">The context instance.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // Create your own context for domain types
    /// [JsonSerializable(typeof(Order))]
    /// [JsonSerializable(typeof(Customer))]
    /// public partial class DomainJsonContext : JsonSerializerContext { }
    /// 
    /// // Register it
    /// services.AddMvp24HoursJsonSourceGeneration()
    ///         .AddJsonSerializerContext(DomainJsonContext.Default);
    /// </code>
    /// </example>
    public static IServiceCollection AddJsonSerializerContext<TContext>(
        this IServiceCollection services,
        TContext context)
        where TContext : JsonSerializerContext
    {
        // Get the existing options and add the new context to the chain
        services.AddSingleton<IJsonTypeInfoResolver>(context);

        // Update the options with the new resolver
        services.PostConfigure<JsonSerializerOptions>(options =>
        {
            if (!options.TypeInfoResolverChain.Contains(context))
            {
                options.TypeInfoResolverChain.Add(context);
            }
        });

        return services;
    }

    /// <summary>
    /// Configures MVC to use source-generated JSON serialization.
    /// </summary>
    /// <param name="builder">The MVC builder.</param>
    /// <returns>The MVC builder for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddControllers()
    ///         .AddMvp24HoursJsonSourceGeneration();
    /// </code>
    /// </example>
    public static IMvcBuilder AddMvp24HoursJsonSourceGeneration(this IMvcBuilder builder)
    {
        builder.AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            options.JsonSerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString;
            options.JsonSerializerOptions.AllowTrailingCommas = true;
            options.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;

            // Add source-generated context
            options.JsonSerializerOptions.TypeInfoResolverChain.Add(Mvp24HoursJsonSerializerContext.Default);

            // Add EntityId converter factory
            options.JsonSerializerOptions.Converters.Add(new EntityIdJsonConverterFactory());
        });

        return builder;
    }

    /// <summary>
    /// Configures MVC to use source-generated JSON serialization with custom contexts.
    /// </summary>
    /// <param name="builder">The MVC builder.</param>
    /// <param name="additionalContexts">Additional JsonSerializerContext instances to include.</param>
    /// <returns>The MVC builder for chaining.</returns>
    public static IMvcBuilder AddMvp24HoursJsonSourceGeneration(
        this IMvcBuilder builder,
        params JsonSerializerContext[] additionalContexts)
    {
        builder.AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            options.JsonSerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString;
            options.JsonSerializerOptions.AllowTrailingCommas = true;
            options.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;

            // Add source-generated contexts
            options.JsonSerializerOptions.TypeInfoResolverChain.Add(Mvp24HoursJsonSerializerContext.Default);

            foreach (var context in additionalContexts)
            {
                options.JsonSerializerOptions.TypeInfoResolverChain.Add(context);
            }

            // Add EntityId converter factory
            options.JsonSerializerOptions.Converters.Add(new EntityIdJsonConverterFactory());
        });

        return builder;
    }
}

