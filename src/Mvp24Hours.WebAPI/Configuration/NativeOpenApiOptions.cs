//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.WebAPI.Configuration
{
    /// <summary>
    /// Configuration options for native OpenAPI (.NET 9+) documentation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class configures the native OpenAPI support introduced in .NET 9 via Microsoft.AspNetCore.OpenApi.
    /// It provides a simplified alternative to Swashbuckle for basic OpenAPI documentation.
    /// </para>
    /// <para>
    /// <b>Key Features:</b>
    /// <list type="bullet">
    /// <item>Native .NET 9 OpenAPI support</item>
    /// <item>AOT-compatible configuration</item>
    /// <item>Document transformers for customization</item>
    /// <item>Multiple document support for API versioning</item>
    /// <item>Integration with Swagger UI/ReDoc for visualization</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursNativeOpenApi(options =>
    /// {
    ///     options.DocumentName = "v1";
    ///     options.Title = "My API";
    ///     options.Version = "1.0.0";
    ///     options.Description = "My API Description";
    ///     options.EnableSwaggerUI = true;
    ///     options.EnableReDoc = true;
    /// });
    /// </code>
    /// </example>
    public class NativeOpenApiOptions
    {
        /// <summary>
        /// Gets or sets the document name (used in the URL path).
        /// </summary>
        /// <remarks>
        /// This is used to identify the OpenAPI document. Default is "v1".
        /// </remarks>
        public string DocumentName { get; set; } = "v1";

        /// <summary>
        /// Gets or sets the API title.
        /// </summary>
        public string Title { get; set; } = "API";

        /// <summary>
        /// Gets or sets the API version.
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Gets or sets the API description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the contact information.
        /// </summary>
        public OpenApiContactInfo? Contact { get; set; }

        /// <summary>
        /// Gets or sets the license information.
        /// </summary>
        public OpenApiLicenseInfo? License { get; set; }

        /// <summary>
        /// Gets or sets the terms of service URL.
        /// </summary>
        public string? TermsOfServiceUrl { get; set; }

        /// <summary>
        /// Gets or sets whether to enable Swagger UI for visualization.
        /// </summary>
        /// <remarks>
        /// When enabled, Swagger UI will be available at the configured <see cref="SwaggerUIRoutePrefix"/>.
        /// </remarks>
        public bool EnableSwaggerUI { get; set; } = true;

        /// <summary>
        /// Gets or sets the Swagger UI route prefix.
        /// </summary>
        /// <remarks>
        /// Defaults to "swagger". The Swagger UI will be available at /{prefix}/index.html.
        /// </remarks>
        public string SwaggerUIRoutePrefix { get; set; } = "swagger";

        /// <summary>
        /// Gets or sets whether to enable ReDoc UI for visualization.
        /// </summary>
        public bool EnableReDoc { get; set; } = false;

        /// <summary>
        /// Gets or sets the ReDoc route prefix.
        /// </summary>
        /// <remarks>
        /// Defaults to "redoc". ReDoc will be available at /{prefix}/index.html.
        /// </remarks>
        public string ReDocRoutePrefix { get; set; } = "redoc";

        /// <summary>
        /// Gets or sets the OpenAPI document route pattern.
        /// </summary>
        /// <remarks>
        /// Defaults to "openapi/{documentName}.json". The document will be available at this route.
        /// </remarks>
        public string DocumentRoutePattern { get; set; } = "openapi/{documentName}.json";

        /// <summary>
        /// Gets or sets whether to include server information in the document.
        /// </summary>
        public bool IncludeServerInfo { get; set; } = true;

        /// <summary>
        /// Gets or sets the server URLs to include in the document.
        /// </summary>
        /// <remarks>
        /// If empty and <see cref="IncludeServerInfo"/> is true, the current request URL will be used.
        /// </remarks>
        public List<OpenApiServerInfo> Servers { get; set; } = new();

        /// <summary>
        /// Gets or sets the authentication scheme configuration.
        /// </summary>
        public OpenApiAuthenticationScheme AuthenticationScheme { get; set; } = OpenApiAuthenticationScheme.None;

        /// <summary>
        /// Gets or sets the security scheme details for Bearer authentication.
        /// </summary>
        public OpenApiBearerSecurityScheme? BearerSecurityScheme { get; set; }

        /// <summary>
        /// Gets or sets the security scheme details for API Key authentication.
        /// </summary>
        public OpenApiApiKeySecurityScheme? ApiKeySecurityScheme { get; set; }

        /// <summary>
        /// Gets or sets additional document configurations for API versioning.
        /// </summary>
        /// <remarks>
        /// Each entry creates a separate OpenAPI document for that version.
        /// </remarks>
        public List<OpenApiVersionConfig> AdditionalVersions { get; set; } = new();

        /// <summary>
        /// Gets or sets the path to XML comments file for documentation enrichment.
        /// </summary>
        public string? XmlCommentsFilePath { get; set; }

        /// <summary>
        /// Gets or sets custom tags for grouping operations.
        /// </summary>
        public List<OpenApiTagInfo> Tags { get; set; } = new();

        /// <summary>
        /// Gets or sets whether to show operation IDs in the document.
        /// </summary>
        public bool ShowOperationIds { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to generate examples from XML comments.
        /// </summary>
        public bool GenerateExamplesFromXmlComments { get; set; } = false;

        /// <summary>
        /// Gets or sets the external docs URL.
        /// </summary>
        public string? ExternalDocsUrl { get; set; }

        /// <summary>
        /// Gets or sets the external docs description.
        /// </summary>
        public string? ExternalDocsDescription { get; set; }
    }

    /// <summary>
    /// Contact information for the API.
    /// </summary>
    public class OpenApiContactInfo
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
    public class OpenApiLicenseInfo
    {
        /// <summary>
        /// Gets or sets the license name.
        /// </summary>
        public string Name { get; set; } = "MIT";

        /// <summary>
        /// Gets or sets the license URL.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the SPDX license identifier (e.g., "MIT", "Apache-2.0").
        /// </summary>
        public string? Identifier { get; set; }
    }

    /// <summary>
    /// Server information for the OpenAPI document.
    /// </summary>
    public class OpenApiServerInfo
    {
        /// <summary>
        /// Gets or sets the server URL.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the server description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets server variables for templated URLs.
        /// </summary>
        public Dictionary<string, OpenApiServerVariable> Variables { get; set; } = new();
    }

    /// <summary>
    /// Server variable for templated server URLs.
    /// </summary>
    public class OpenApiServerVariable
    {
        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        public string Default { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the enumeration of possible values.
        /// </summary>
        public List<string> Enum { get; set; } = new();
    }

    /// <summary>
    /// Authentication scheme for OpenAPI security definitions.
    /// </summary>
    public enum OpenApiAuthenticationScheme
    {
        /// <summary>
        /// No authentication.
        /// </summary>
        None = 0,

        /// <summary>
        /// Bearer (JWT) authentication.
        /// </summary>
        Bearer = 1,

        /// <summary>
        /// Basic authentication.
        /// </summary>
        Basic = 2,

        /// <summary>
        /// API Key authentication.
        /// </summary>
        ApiKey = 3,

        /// <summary>
        /// OAuth2 authentication.
        /// </summary>
        OAuth2 = 4
    }

    /// <summary>
    /// Bearer (JWT) security scheme configuration.
    /// </summary>
    public class OpenApiBearerSecurityScheme
    {
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { get; set; } = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.";

        /// <summary>
        /// Gets or sets the scheme name.
        /// </summary>
        public string Scheme { get; set; } = "Bearer";

        /// <summary>
        /// Gets or sets the bearer format (e.g., "JWT").
        /// </summary>
        public string BearerFormat { get; set; } = "JWT";
    }

    /// <summary>
    /// API Key security scheme configuration.
    /// </summary>
    public class OpenApiApiKeySecurityScheme
    {
        /// <summary>
        /// Gets or sets the header or query parameter name.
        /// </summary>
        public string Name { get; set; } = "X-API-Key";

        /// <summary>
        /// Gets or sets where the API key is located (header, query, cookie).
        /// </summary>
        public ApiKeyLocation Location { get; set; } = ApiKeyLocation.Header;

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { get; set; } = "API Key authentication. Enter your API key in the text input below.";
    }

    /// <summary>
    /// Location of the API key.
    /// </summary>
    public enum ApiKeyLocation
    {
        /// <summary>
        /// API key in header.
        /// </summary>
        Header,

        /// <summary>
        /// API key in query string.
        /// </summary>
        Query,

        /// <summary>
        /// API key in cookie.
        /// </summary>
        Cookie
    }

    /// <summary>
    /// Configuration for an additional API version document.
    /// </summary>
    public class OpenApiVersionConfig
    {
        /// <summary>
        /// Gets or sets the document name (version identifier).
        /// </summary>
        public string DocumentName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the version string.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the description.
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
    }

    /// <summary>
    /// Tag information for grouping API operations.
    /// </summary>
    public class OpenApiTagInfo
    {
        /// <summary>
        /// Gets or sets the tag name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the tag description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the external documentation URL for this tag.
        /// </summary>
        public string? ExternalDocsUrl { get; set; }
    }
}

