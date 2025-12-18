//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Application.Contract.Pagination;
using Mvp24Hours.Application.Logic.Pagination;
using System;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for registering pagination services in the dependency injection container.
    /// </summary>
    public static class PaginationServiceCollectionExtensions
    {
        /// <summary>
        /// Adds pagination services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursPagination();
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursPagination(this IServiceCollection services)
        {
            return services.AddMvp24HoursPagination(options => { });
        }

        /// <summary>
        /// Adds pagination services to the service collection with configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">The configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursPagination(options =>
        /// {
        ///     options.DefaultPageSize = 25;
        ///     options.MaxPageSize = 200;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursPagination(
            this IServiceCollection services,
            Action<PaginationOptions> configure)
        {
            var options = new PaginationOptions();
            configure?.Invoke(options);

            services.TryAddSingleton(options);

            return services;
        }
    }

    /// <summary>
    /// Options for configuring pagination behavior.
    /// </summary>
    public class PaginationOptions
    {
        /// <summary>
        /// Gets or sets the default page size when not specified.
        /// Default: 20
        /// </summary>
        public int DefaultPageSize { get; set; } = PaginationHelper.DefaultPageSize;

        /// <summary>
        /// Gets or sets the maximum allowed page size.
        /// Default: 100
        /// </summary>
        public int MaxPageSize { get; set; } = PaginationHelper.MaxPageSize;

        /// <summary>
        /// Gets or sets the minimum allowed page size.
        /// Default: 1
        /// </summary>
        public int MinPageSize { get; set; } = PaginationHelper.MinPageSize;

        /// <summary>
        /// Gets or sets whether to validate pagination parameters.
        /// Default: true
        /// </summary>
        public bool ValidateParameters { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to normalize out-of-range page numbers.
        /// When true, pages beyond total pages return the last page.
        /// Default: false
        /// </summary>
        public bool NormalizePageNumbers { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to include pagination metadata in response headers.
        /// Default: true
        /// </summary>
        public bool IncludeHeaderMetadata { get; set; } = true;

        /// <summary>
        /// Gets or sets the header name for total count.
        /// Default: X-Total-Count
        /// </summary>
        public string TotalCountHeaderName { get; set; } = "X-Total-Count";

        /// <summary>
        /// Gets or sets the header name for pagination links.
        /// Default: Link
        /// </summary>
        public string LinkHeaderName { get; set; } = "Link";
    }
}

