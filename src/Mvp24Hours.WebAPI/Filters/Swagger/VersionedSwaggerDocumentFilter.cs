//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Mvp24Hours.WebAPI.Configuration;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Linq;

namespace Mvp24Hours.WebAPI.Filters.Swagger
{
    /// <summary>
    /// Document filter that configures Swagger documents for each API version.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This filter is responsible for creating separate Swagger documents for each API version
    /// discovered via IApiVersionDescriptionProvider.
    /// </para>
    /// </remarks>
    public class VersionedSwaggerDocumentFilter : IDocumentFilter
    {
        private readonly SwaggerOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="VersionedSwaggerDocumentFilter"/> class.
        /// </summary>
        /// <param name="options">The Swagger options.</param>
        public VersionedSwaggerDocumentFilter(SwaggerOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Applies the filter to configure API version documents.
        /// </summary>
        /// <param name="swaggerDoc">The OpenAPI document.</param>
        /// <param name="context">The document filter context.</param>
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            // This method is called for each document, but we need to configure documents
            // based on API versions. The actual configuration happens in AddSwaggerGen.
            // This filter can be used for additional document-level modifications.
        }
    }

    /// <summary>
    /// Operation filter that filters operations by API version.
    /// </summary>
    public class VersionedSwaggerOperationFilter : IOperationFilter
    {
        /// <summary>
        /// Applies the filter to include/exclude operations based on API version.
        /// </summary>
        /// <param name="operation">The OpenAPI operation.</param>
        /// <param name="context">The operation filter context.</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Additional operation-level filtering can be done here if needed
        }
    }
}

