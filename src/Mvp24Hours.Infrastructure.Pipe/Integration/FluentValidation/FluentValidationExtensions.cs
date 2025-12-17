//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Integration.FluentValidation;
using Mvp24Hours.Infrastructure.Pipe.Typed;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for integrating FluentValidation with Mvp24Hours pipelines.
    /// </summary>
    public static class FluentValidationExtensions
    {
        /// <summary>
        /// Registers FluentValidation operations for use in pipelines.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPipelineFluentValidation(
            this IServiceCollection services,
            Action<FluentValidationOptions>? configure = null)
        {
            var options = new FluentValidationOptions();
            configure?.Invoke(options);
            services.TryAddSingleton(options);
            
            return services;
        }

        /// <summary>
        /// Registers a FluentValidation operation for a specific type.
        /// </summary>
        /// <typeparam name="T">The type to validate.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddFluentValidationOperation<T>(
            this IServiceCollection services,
            Action<FluentValidationOptions>? configure = null)
        {
            services.AddTransient<ITypedOperationAsync<T, T>>(sp =>
            {
                var validators = sp.GetServices<IValidator<T>>();
                var logger = sp.GetService<ILogger<FluentValidationOperation<T>>>();
                var globalOptions = sp.GetService<FluentValidationOptions>() ?? new FluentValidationOptions();
                
                var options = new FluentValidationOptions
                {
                    IsRequired = globalOptions.IsRequired,
                    ThrowValidationException = globalOptions.ThrowValidationException,
                    ThrowOnValidatorException = globalOptions.ThrowOnValidatorException,
                    FailOnMissingData = globalOptions.FailOnMissingData,
                    LockPipelineOnFailure = globalOptions.LockPipelineOnFailure,
                    IncludeNonErrorMessages = globalOptions.IncludeNonErrorMessages,
                    RuleSet = globalOptions.RuleSet
                };
                
                configure?.Invoke(options);
                
                return new FluentValidationOperation<T>(validators, logger, options);
            });

            return services;
        }

        /// <summary>
        /// Registers a FluentValidation pipeline operation for IPipelineMessage.
        /// </summary>
        /// <typeparam name="T">The type of data to validate from the message.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="tokenAlias">The token alias to retrieve data. If null, uses typeof(T).Name.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddFluentValidationPipelineOperation<T>(
            this IServiceCollection services,
            string? tokenAlias = null,
            Action<FluentValidationOptions>? configure = null)
        {
            services.AddTransient<FluentValidationPipelineOperation<T>>(sp =>
            {
                var validators = sp.GetServices<IValidator<T>>();
                var logger = sp.GetService<ILogger<FluentValidationPipelineOperation<T>>>();
                var globalOptions = sp.GetService<FluentValidationOptions>() ?? new FluentValidationOptions();
                
                var options = new FluentValidationOptions
                {
                    IsRequired = globalOptions.IsRequired,
                    ThrowValidationException = globalOptions.ThrowValidationException,
                    ThrowOnValidatorException = globalOptions.ThrowOnValidatorException,
                    FailOnMissingData = globalOptions.FailOnMissingData,
                    LockPipelineOnFailure = globalOptions.LockPipelineOnFailure,
                    IncludeNonErrorMessages = globalOptions.IncludeNonErrorMessages,
                    RuleSet = globalOptions.RuleSet
                };
                
                configure?.Invoke(options);
                
                return new FluentValidationPipelineOperation<T>(validators, tokenAlias, logger, options);
            });

            return services;
        }

        /// <summary>
        /// Adds a FluentValidation operation to a typed pipeline.
        /// </summary>
        /// <typeparam name="T">The type to validate.</typeparam>
        /// <param name="pipeline">The pipeline to add the operation to.</param>
        /// <param name="validators">The validators to use.</param>
        /// <param name="options">Optional validation options.</param>
        /// <returns>The pipeline for chaining.</returns>
        public static TypedPipelineAsync<T, T> AddValidation<T>(
            this TypedPipelineAsync<T, T> pipeline,
            IEnumerable<IValidator<T>> validators,
            FluentValidationOptions? options = null)
        {
            var operation = new FluentValidationOperation<T>(validators, null, options);
            pipeline.Add<T, T>(operation);
            return pipeline;
        }

        /// <summary>
        /// Adds an inline validation function to a typed pipeline using FluentValidation rules.
        /// </summary>
        /// <typeparam name="T">The type to validate.</typeparam>
        /// <param name="pipeline">The pipeline to add the validation to.</param>
        /// <param name="validatorFactory">Factory function that creates a validator.</param>
        /// <param name="options">Optional validation options.</param>
        /// <returns>The pipeline for chaining.</returns>
        public static TypedPipelineAsync<T, T> AddInlineValidation<T>(
            this TypedPipelineAsync<T, T> pipeline,
            Func<InlineValidator<T>> validatorFactory,
            FluentValidationOptions? options = null)
        {
            var validator = validatorFactory();
            var operation = new FluentValidationOperation<T>(new[] { validator }, null, options);
            pipeline.Add<T, T>(operation);
            return pipeline;
        }
    }

    /// <summary>
    /// Helper class for creating inline validators without defining a separate class.
    /// </summary>
    /// <typeparam name="T">The type to validate.</typeparam>
    public class InlineValidator<T> : AbstractValidator<T>
    {
        /// <summary>
        /// Creates a new InlineValidator with an optional configuration action.
        /// </summary>
        /// <param name="configure">Optional action to configure validation rules.</param>
        public InlineValidator(Action<InlineValidator<T>>? configure = null)
        {
            configure?.Invoke(this);
        }
    }
}

