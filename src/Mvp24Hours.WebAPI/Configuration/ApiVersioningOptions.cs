//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.WebAPI.Configuration
{
    /// <summary>
    /// Configuration options for API Versioning.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class configures how API versioning is handled in the application.
    /// Supports multiple versioning strategies: URL path, HTTP header, and query string.
    /// </para>
    /// </remarks>
    public class ApiVersioningOptions
    {
        /// <summary>
        /// Gets or sets the default API version when none is specified.
        /// </summary>
        /// <remarks>
        /// Defaults to "1.0" if not specified.
        /// </remarks>
        public ApiVersion DefaultApiVersion { get; set; } = new ApiVersion(1, 0);

        /// <summary>
        /// Gets or sets whether to assume a default version when a version isn't specified.
        /// </summary>
        /// <remarks>
        /// When true, requests without a version will use the DefaultApiVersion.
        /// </remarks>
        public bool AssumeDefaultVersionWhenUnspecified { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to report API versions in response headers.
        /// </summary>
        /// <remarks>
        /// When enabled, adds "api-supported-versions" and "api-deprecated-versions" headers.
        /// </remarks>
        public bool ReportApiVersions { get; set; } = true;

        /// <summary>
        /// Gets or sets the versioning strategy.
        /// </summary>
        /// <remarks>
        /// Can be a combination of:
        /// - ApiVersionReader.UrlSegment (e.g., /api/v1/users)
        /// - ApiVersionReader.Header (e.g., X-API-Version: 1.0)
        /// - ApiVersionReader.QueryString (e.g., ?api-version=1.0)
        /// </remarks>
        public IApiVersionReader? ApiVersionReader { get; set; }

        /// <summary>
        /// Gets or sets the list of supported API versions.
        /// </summary>
        public List<ApiVersion> SupportedApiVersions { get; set; } = new List<ApiVersion>();

        /// <summary>
        /// Gets or sets the list of deprecated API versions.
        /// </summary>
        public List<ApiVersion> DeprecatedApiVersions { get; set; } = new List<ApiVersion>();

        /// <summary>
        /// Gets or sets the versioning strategy mode.
        /// </summary>
        public ApiVersioningStrategy Strategy { get; set; } = ApiVersioningStrategy.UrlPath | ApiVersioningStrategy.Header | ApiVersioningStrategy.QueryString;

        /// <summary>
        /// Gets or sets the header name for header-based versioning.
        /// </summary>
        /// <remarks>
        /// Defaults to "X-API-Version" if not specified.
        /// </remarks>
        public string HeaderName { get; set; } = "X-API-Version";

        /// <summary>
        /// Gets or sets the query string parameter name for query-based versioning.
        /// </summary>
        /// <remarks>
        /// Defaults to "api-version" if not specified.
        /// </remarks>
        public string QueryStringParameterName { get; set; } = "api-version";

        /// <summary>
        /// Gets or sets the URL segment pattern for URL-based versioning.
        /// </summary>
        /// <remarks>
        /// Defaults to "v{version}" (e.g., /api/v1/users).
        /// </remarks>
        public string UrlSegmentPattern { get; set; } = "v{version}";
    }

    /// <summary>
    /// Enumeration of API versioning strategies.
    /// </summary>
    [Flags]
    public enum ApiVersioningStrategy
    {
        /// <summary>
        /// No versioning strategy.
        /// </summary>
        None = 0,

        /// <summary>
        /// Version specified in URL path (e.g., /api/v1/users).
        /// </summary>
        UrlPath = 1,

        /// <summary>
        /// Version specified in HTTP header (e.g., X-API-Version: 1.0).
        /// </summary>
        Header = 2,

        /// <summary>
        /// Version specified in query string (e.g., ?api-version=1.0).
        /// </summary>
        QueryString = 4,

        /// <summary>
        /// All versioning strategies enabled.
        /// </summary>
        All = UrlPath | Header | QueryString
    }
}

