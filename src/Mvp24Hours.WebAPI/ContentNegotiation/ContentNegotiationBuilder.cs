//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.WebAPI.ContentNegotiation
{
    /// <summary>
    /// Builder for configuring content negotiation with custom formatters.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This builder provides a fluent API for configuring content negotiation,
    /// including registration of custom formatters.
    /// </para>
    /// </remarks>
    public class ContentNegotiationBuilder
    {
        private readonly IServiceCollection _services;
        private readonly List<Type> _customFormatterTypes = new();
        private readonly List<IContentFormatter> _customFormatters = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentNegotiationBuilder"/> class.
        /// </summary>
        /// <param name="services">The service collection.</param>
        internal ContentNegotiationBuilder(IServiceCollection services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <summary>
        /// Adds a custom formatter by type.
        /// </summary>
        /// <typeparam name="TFormatter">The formatter type implementing <see cref="IContentFormatter"/>.</typeparam>
        /// <returns>The builder for chaining.</returns>
        public ContentNegotiationBuilder AddFormatter<TFormatter>()
            where TFormatter : class, IContentFormatter
        {
            _customFormatterTypes.Add(typeof(TFormatter));
            _services.TryAddSingleton<TFormatter>();
            return this;
        }

        /// <summary>
        /// Adds a custom formatter instance.
        /// </summary>
        /// <param name="formatter">The formatter instance.</param>
        /// <returns>The builder for chaining.</returns>
        public ContentNegotiationBuilder AddFormatter(IContentFormatter formatter)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            _customFormatters.Add(formatter);
            return this;
        }

        /// <summary>
        /// Adds a custom formatter with a factory function.
        /// </summary>
        /// <typeparam name="TFormatter">The formatter type implementing <see cref="IContentFormatter"/>.</typeparam>
        /// <param name="factory">The factory function to create the formatter.</param>
        /// <returns>The builder for chaining.</returns>
        public ContentNegotiationBuilder AddFormatter<TFormatter>(Func<IServiceProvider, TFormatter> factory)
            where TFormatter : class, IContentFormatter
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _services.TryAddSingleton(factory);
            _customFormatterTypes.Add(typeof(TFormatter));
            return this;
        }

        /// <summary>
        /// Gets the custom formatter types registered.
        /// </summary>
        internal IReadOnlyList<Type> CustomFormatterTypes => _customFormatterTypes.AsReadOnly();

        /// <summary>
        /// Gets the custom formatter instances registered.
        /// </summary>
        internal IReadOnlyList<IContentFormatter> CustomFormatters => _customFormatters.AsReadOnly();
    }
}

