//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.WebAPI.Models;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.WebAPI.Configuration
{
    /// <summary>
    /// Configuration options for Swagger/OpenAPI documentation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class configures Swagger/OpenAPI documentation generation including:
    /// - Multiple API versions support
    /// - OpenAPI 3.1 specification
    /// - ReDoc integration
    /// - Automatic examples
    /// - Deprecation warnings
    /// </para>
    /// </remarks>
    public class SwaggerOptions
    {
        /// <summary>
        /// Gets or sets the API title.
        /// </summary>
        public string Title { get; set; } = "API";

        /// <summary>
        /// Gets or sets the API description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the API contact information.
        /// </summary>
        public SwaggerContact? Contact { get; set; }

        /// <summary>
        /// Gets or sets the API license information.
        /// </summary>
        public SwaggerLicense? License { get; set; }

        /// <summary>
        /// Gets or sets the OpenAPI specification version.
        /// </summary>
        /// <remarks>
        /// Defaults to "3.1.0" (OpenAPI 3.1).
        /// </remarks>
        public string OpenApiVersion { get; set; } = "3.1.0";

        /// <summary>
        /// Gets or sets the path to XML comments file.
        /// </summary>
        public string? XmlCommentsFileName { get; set; }

        /// <summary>
        /// Gets or sets whether to enable automatic examples from XML comments.
        /// </summary>
        public bool EnableExamples { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to enable ReDoc UI.
        /// </summary>
        public bool EnableReDoc { get; set; } = false;

        /// <summary>
        /// Gets or sets the ReDoc route prefix.
        /// </summary>
        /// <remarks>
        /// Defaults to "redoc" if not specified.
        /// </remarks>
        public string ReDocRoutePrefix { get; set; } = "redoc";

        /// <summary>
        /// Gets or sets whether to show deprecation warnings in Swagger UI.
        /// </summary>
        public bool ShowDeprecationWarnings { get; set; } = true;

        /// <summary>
        /// Gets or sets the Swagger UI route prefix.
        /// </summary>
        /// <remarks>
        /// Defaults to "swagger" if not specified.
        /// </remarks>
        public string SwaggerRoutePrefix { get; set; } = "swagger";

        /// <summary>
        /// Gets or sets the list of API version configurations.
        /// </summary>
        public List<SwaggerVersionInfo> Versions { get; set; } = new List<SwaggerVersionInfo>();

        /// <summary>
        /// Gets or sets the authorization scheme configuration.
        /// </summary>
        public SwaggerAuthorizationScheme AuthorizationScheme { get; set; } = SwaggerAuthorizationScheme.None;

        /// <summary>
        /// Gets or sets the list of authorization types for operation filters.
        /// </summary>
        public List<Type>? AuthorizationTypes { get; set; }
    }

    /// <summary>
    /// Information about a specific API version for Swagger documentation.
    /// </summary>
    public class SwaggerVersionInfo
    {
        /// <summary>
        /// Gets or sets the version string (e.g., "v1", "1.0").
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the version title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the version description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets whether this version is deprecated.
        /// </summary>
        public bool IsDeprecated { get; set; } = false;

        /// <summary>
        /// Gets or sets the deprecation message.
        /// </summary>
        public string? DeprecationMessage { get; set; }

        /// <summary>
        /// Gets or sets the deprecation date.
        /// </summary>
        public DateTime? DeprecationDate { get; set; }

        /// <summary>
        /// Gets or sets the removal date for deprecated versions.
        /// </summary>
        public DateTime? RemovalDate { get; set; }
    }

    /// <summary>
    /// Contact information for the API.
    /// </summary>
    public class SwaggerContact
    {
        /// <summary>
        /// Gets or sets the contact name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the contact email.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Gets or sets the contact URL.
        /// </summary>
        public string? Url { get; set; }
    }

    /// <summary>
    /// License information for the API.
    /// </summary>
    public class SwaggerLicense
    {
        /// <summary>
        /// Gets or sets the license name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the license URL.
        /// </summary>
        public string? Url { get; set; }
    }
}

