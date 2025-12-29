//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.Infrastructure.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Mvp24Hours.Core.Extensions.Options;

/// <summary>
/// Extension methods for configuring and validating options using the Options pattern.
/// </summary>
/// <remarks>
/// <para>
/// Provides a unified API for registering options with:
/// - Data Annotations validation
/// - Custom validation via <see cref="IOptionsValidator{TOptions}"/>
/// - Fluent validation
/// - Startup validation (fail-fast)
/// </para>
/// <para>
/// <strong>IOptions&lt;T&gt; vs IOptionsMonitor&lt;T&gt; vs IOptionsSnapshot&lt;T&gt;</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <term>IOptions&lt;T&gt;</term>
/// <description>Singleton, configuration read once at startup. Best for static configuration.</description>
/// </item>
/// <item>
/// <term>IOptionsMonitor&lt;T&gt;</term>
/// <description>Singleton, supports change notifications. Best for runtime configuration changes.</description>
/// </item>
/// <item>
/// <term>IOptionsSnapshot&lt;T&gt;</term>
/// <description>Scoped, configuration re-evaluated per request. Best for per-request configuration.</description>
/// </item>
/// </list>
/// </remarks>
public static class OptionsValidationExtensions
{
    #region AddOptions with Validation

    /// <summary>
    /// Adds options with Data Annotations validation and optional startup validation.
    /// </summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configurationSection">The configuration section to bind.</param>
    /// <param name="validateOnStart">Whether to validate on startup (fail-fast).</param>
    /// <returns>The <see cref="OptionsBuilder{TOptions}"/> for further configuration.</returns>
    /// <example>
    /// <code>
    /// services.AddOptionsWithValidation&lt;MyOptions&gt;(
    ///     configuration.GetSection("MyOptions"),
    ///     validateOnStart: true);
    /// </code>
    /// </example>
    public static OptionsBuilder<TOptions> AddOptionsWithValidation<TOptions>(
        this IServiceCollection services,
        IConfigurationSection configurationSection,
        bool validateOnStart = true)
        where TOptions : class
    {
        var builder = services.AddOptions<TOptions>()
            .Bind(configurationSection)
            .ValidateDataAnnotations();

        if (validateOnStart)
        {
            builder.ValidateOnStart();
        }

        return builder;
    }

    /// <summary>
    /// Adds options with custom validator and optional startup validation.
    /// </summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <typeparam name="TValidator">The validator type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configurationSection">The configuration section to bind.</param>
    /// <param name="validateOnStart">Whether to validate on startup (fail-fast).</param>
    /// <returns>The <see cref="OptionsBuilder{TOptions}"/> for further configuration.</returns>
    /// <example>
    /// <code>
    /// services.AddOptionsWithValidation&lt;MyOptions, MyOptionsValidator&gt;(
    ///     configuration.GetSection("MyOptions"),
    ///     validateOnStart: true);
    /// </code>
    /// </example>
    public static OptionsBuilder<TOptions> AddOptionsWithValidation<TOptions, TValidator>(
        this IServiceCollection services,
        IConfigurationSection configurationSection,
        bool validateOnStart = true)
        where TOptions : class
        where TValidator : class, IOptionsValidator<TOptions>
    {
        // Register the validator
        services.TryAddSingleton<TValidator>();
        services.TryAddSingleton<IValidateOptions<TOptions>>(sp =>
        {
            var validator = sp.GetRequiredService<TValidator>();
            return new OptionsValidatorAdapter<TOptions, TValidator>(validator);
        });

        var builder = services.AddOptions<TOptions>()
            .Bind(configurationSection)
            .ValidateDataAnnotations();

        if (validateOnStart)
        {
            builder.ValidateOnStart();
        }

        return builder;
    }

    /// <summary>
    /// Adds options with fluent validation and optional startup validation.
    /// </summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configurationSection">The configuration section to bind.</param>
    /// <param name="validate">The validation function.</param>
    /// <param name="failureMessage">The failure message if validation fails.</param>
    /// <param name="validateOnStart">Whether to validate on startup (fail-fast).</param>
    /// <returns>The <see cref="OptionsBuilder{TOptions}"/> for further configuration.</returns>
    /// <example>
    /// <code>
    /// services.AddOptionsWithValidation&lt;MyOptions&gt;(
    ///     configuration.GetSection("MyOptions"),
    ///     options => !string.IsNullOrEmpty(options.ConnectionString),
    ///     "ConnectionString is required");
    /// </code>
    /// </example>
    public static OptionsBuilder<TOptions> AddOptionsWithValidation<TOptions>(
        this IServiceCollection services,
        IConfigurationSection configurationSection,
        Func<TOptions, bool> validate,
        string failureMessage,
        bool validateOnStart = true)
        where TOptions : class
    {
        var builder = services.AddOptions<TOptions>()
            .Bind(configurationSection)
            .ValidateDataAnnotations()
            .Validate(validate, failureMessage);

        if (validateOnStart)
        {
            builder.ValidateOnStart();
        }

        return builder;
    }

    /// <summary>
    /// Adds options with multiple validation functions and optional startup validation.
    /// </summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configurationSection">The configuration section to bind.</param>
    /// <param name="validations">The validation rules (predicate, failure message).</param>
    /// <param name="validateOnStart">Whether to validate on startup (fail-fast).</param>
    /// <returns>The <see cref="OptionsBuilder{TOptions}"/> for further configuration.</returns>
    public static OptionsBuilder<TOptions> AddOptionsWithValidation<TOptions>(
        this IServiceCollection services,
        IConfigurationSection configurationSection,
        IEnumerable<(Func<TOptions, bool> Validate, string FailureMessage)> validations,
        bool validateOnStart = true)
        where TOptions : class
    {
        var builder = services.AddOptions<TOptions>()
            .Bind(configurationSection)
            .ValidateDataAnnotations();

        foreach (var (validate, failureMessage) in validations)
        {
            builder.Validate(validate, failureMessage);
        }

        if (validateOnStart)
        {
            builder.ValidateOnStart();
        }

        return builder;
    }

    #endregion

    #region AddOptions for IOptionsMonitor (Runtime Changes)

    /// <summary>
    /// Adds options configured for runtime changes using IOptionsMonitor.
    /// </summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configurationSection">The configuration section to bind.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this when you need to react to configuration changes at runtime.
    /// The configuration will be reloaded automatically when the underlying
    /// configuration source changes (e.g., appsettings.json file changes).
    /// </para>
    /// <para>
    /// To handle change notifications, use <see cref="IOptionsMonitor{TOptions}.OnChange"/> in your service:
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddOptionsForMonitor&lt;FeatureFlagsOptions&gt;(
    ///     configuration.GetSection("FeatureFlags"));
    /// 
    /// // In your service:
    /// public class MyService
    /// {
    ///     private readonly IOptionsMonitor&lt;FeatureFlagsOptions&gt; _options;
    ///     private readonly IDisposable _changeListener;
    ///     
    ///     public MyService(IOptionsMonitor&lt;FeatureFlagsOptions&gt; options)
    ///     {
    ///         _options = options;
    ///         _changeListener = _options.OnChange(opts => HandleConfigChange(opts));
    ///     }
    ///     
    ///     public bool IsFeatureEnabled() => _options.CurrentValue.EnableFeature1;
    ///     
    ///     private void HandleConfigChange(FeatureFlagsOptions opts)
    ///     {
    ///         Console.WriteLine($"Features changed! Feature1={opts.EnableFeature1}");
    ///     }
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddOptionsForMonitor<TOptions>(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
        where TOptions : class
    {
        services.AddOptions<TOptions>()
            .Bind(configurationSection)
            .ValidateDataAnnotations();

        // Register change token source for configuration reloading
        services.AddSingleton<IOptionsChangeTokenSource<TOptions>>(sp =>
            new ConfigurationChangeTokenSource<TOptions>(
                Microsoft.Extensions.Options.Options.DefaultName,
                configurationSection));

        return services;
    }

    #endregion

    #region AddOptions for IOptionsSnapshot (Per-Request)

    /// <summary>
    /// Adds options configured for per-request evaluation using IOptionsSnapshot.
    /// </summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configurationSection">The configuration section to bind.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this when you need configuration to be re-evaluated per HTTP request
    /// or per scope. Each scope gets a fresh snapshot of the configuration.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddOptionsForSnapshot&lt;TenantOptions&gt;(
    ///     configuration.GetSection("Tenant"));
    /// 
    /// // In your service:
    /// public class MyService
    /// {
    ///     private readonly IOptionsSnapshot&lt;TenantOptions&gt; _options;
    ///     
    ///     public MyService(IOptionsSnapshot&lt;TenantOptions&gt; options)
    ///     {
    ///         _options = options;
    ///     }
    ///     
    ///     public string GetTenantId() => _options.Value.TenantId;
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddOptionsForSnapshot<TOptions>(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
        where TOptions : class
    {
        services.AddOptions<TOptions>()
            .Bind(configurationSection)
            .ValidateDataAnnotations();

        return services;
    }

    #endregion

    #region Validator Registration

    /// <summary>
    /// Registers a custom options validator.
    /// </summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <typeparam name="TValidator">The validator type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOptionsValidator<TOptions, TValidator>(
        this IServiceCollection services)
        where TOptions : class
        where TValidator : class, IOptionsValidator<TOptions>
    {
        services.TryAddSingleton<TValidator>();
        services.TryAddSingleton<IValidateOptions<TOptions>>(sp =>
        {
            var validator = sp.GetRequiredService<TValidator>();
            return new OptionsValidatorAdapter<TOptions, TValidator>(validator);
        });
        return services;
    }

    /// <summary>
    /// Registers options validators from an assembly by scanning for types
    /// implementing <see cref="IOptionsValidator{TOptions}"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOptionsValidatorsFromAssembly(
        this IServiceCollection services,
        Assembly assembly)
    {
        var validatorTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType &&
                         i.GetGenericTypeDefinition() == typeof(IOptionsValidator<>)));

        foreach (var validatorType in validatorTypes)
        {
            var optionsInterface = validatorType.GetInterfaces()
                .First(i => i.IsGenericType &&
                           i.GetGenericTypeDefinition() == typeof(IOptionsValidator<>));

            var optionsType = optionsInterface.GetGenericArguments()[0];

            // Register validator
            services.TryAddSingleton(validatorType);

            // Create and register adapter
            var adapterType = typeof(OptionsValidatorAdapter<,>)
                .MakeGenericType(optionsType, validatorType);
            var validateOptionsType = typeof(IValidateOptions<>)
                .MakeGenericType(optionsType);

            services.TryAddSingleton(validateOptionsType, sp =>
            {
                var validator = sp.GetRequiredService(validatorType);
                return Activator.CreateInstance(adapterType, validator)!;
            });
        }

        return services;
    }

    /// <summary>
    /// Registers options validators from the assembly containing the specified type.
    /// </summary>
    /// <typeparam name="TMarker">A type from the assembly to scan.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOptionsValidatorsFromAssemblyContaining<TMarker>(
        this IServiceCollection services)
    {
        return services.AddOptionsValidatorsFromAssembly(typeof(TMarker).Assembly);
    }

    #endregion

    #region DataAnnotations Validation Helper

    /// <summary>
    /// Validates an options instance using Data Annotations.
    /// </summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <param name="options">The options instance to validate.</param>
    /// <returns>The validation result.</returns>
    public static OptionsValidationResult ValidateWithDataAnnotations<TOptions>(TOptions options)
        where TOptions : class
    {
        if (options == null)
        {
            return OptionsValidationResult.Fail($"{typeof(TOptions).Name} cannot be null.");
        }

        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        if (Validator.TryValidateObject(options, context, results, validateAllProperties: true))
        {
            return OptionsValidationResult.Success();
        }

        var errors = results
            .Where(r => !string.IsNullOrEmpty(r.ErrorMessage))
            .Select(r => r.ErrorMessage!)
            .ToList();

        return OptionsValidationResult.Fail(errors);
    }

    #endregion
}

/// <summary>
/// Adapter that bridges <see cref="IOptionsValidator{TOptions}"/> to
/// <see cref="IValidateOptions{TOptions}"/>.
/// </summary>
/// <typeparam name="TOptions">The options type.</typeparam>
/// <typeparam name="TValidator">The validator type.</typeparam>
internal sealed class OptionsValidatorAdapter<TOptions, TValidator> : IValidateOptions<TOptions>
    where TOptions : class
    where TValidator : IOptionsValidator<TOptions>
{
    private readonly TValidator _validator;

    public OptionsValidatorAdapter(TValidator validator)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public ValidateOptionsResult Validate(string? name, TOptions options)
    {
        if (options == null)
        {
            return ValidateOptionsResult.Fail($"{typeof(TOptions).Name} cannot be null.");
        }

        var result = _validator.Validate(options);

        return result.Succeeded
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(result.Failures);
    }
}

