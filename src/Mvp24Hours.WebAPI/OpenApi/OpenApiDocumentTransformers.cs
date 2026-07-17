//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Mvp24Hours.WebAPI.OpenApi
{
    /// <summary>
    /// Document transformer that adds custom headers to all operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This transformer adds custom header parameters (e.g., Correlation-Id, Tenant-Id)
    /// to all operations in the OpenAPI document.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// openApiOptions.AddDocumentTransformer(new CustomHeadersTransformer(
    ///     ("X-Correlation-Id", "Correlation ID for request tracing", false),
    ///     ("X-Tenant-Id", "Tenant identifier", true)
    /// ));
    /// </code>
    /// </example>
    public class CustomHeadersTransformer : IOpenApiDocumentTransformer
    {
        private readonly List<(string Name, string Description, bool Required)> _headers;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomHeadersTransformer"/> class.
        /// </summary>
        /// <param name="headers">The headers to add (name, description, required).</param>
        public CustomHeadersTransformer(params (string Name, string Description, bool Required)[] headers)
        {
            _headers = headers.ToList();
        }

        /// <inheritdoc />
        public Task TransformAsync(
            OpenApiDocument document,
            OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken)
        {
            if (document.Paths == null) return Task.CompletedTask;
            foreach (IOpenApiPathItem path in document.Paths.Values)
            {
                if (path.Operations == null) continue;
                foreach (OpenApiOperation operation in path.Operations.Values)
                {
                    operation.Parameters ??= [];

                    foreach ((string? name, string? description, bool required) in _headers)
                    {
                        // Skip if already exists
                        if (operation.Parameters.Any(p => p.Name == name && p.In == ParameterLocation.Header))
                            continue;

                        operation.Parameters.Add(new OpenApiParameter
                        {
                            Name = name,
                            In = ParameterLocation.Header,
                            Description = description,
                            Required = required,
                            Schema = new OpenApiSchema { Type = JsonSchemaType.String }
                        });
                    }
                }
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Document transformer that adds response codes to all operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This transformer adds common response codes (e.g., 401, 403, 500) to all operations.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// openApiOptions.AddDocumentTransformer(new CommonResponsesTransformer(
    ///     add401: true,
    ///     add403: true,
    ///     add500: true
    /// ));
    /// </code>
    /// </example>
    public class CommonResponsesTransformer : IOpenApiDocumentTransformer
    {
        private readonly bool _add401;
        private readonly bool _add403;
        private readonly bool _add500;
        private readonly bool _add503;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommonResponsesTransformer"/> class.
        /// </summary>
        /// <param name="add401">Whether to add 401 Unauthorized response.</param>
        /// <param name="add403">Whether to add 403 Forbidden response.</param>
        /// <param name="add500">Whether to add 500 Internal Server Error response.</param>
        /// <param name="add503">Whether to add 503 Service Unavailable response.</param>
        public CommonResponsesTransformer(
            bool add401 = true,
            bool add403 = true,
            bool add500 = true,
            bool add503 = false)
        {
            _add401 = add401;
            _add403 = add403;
            _add500 = add500;
            _add503 = add503;
        }

        /// <inheritdoc />
        public Task TransformAsync(
            OpenApiDocument document,
            OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken)
        {
            if (document.Paths == null) return Task.CompletedTask;
            foreach (IOpenApiPathItem path in document.Paths.Values)
            {
                if (path.Operations == null) continue;
                foreach (OpenApiOperation operation in path.Operations.Values)
                {
                    operation.Responses ??= [];

                    if (_add401 && !operation.Responses.ContainsKey("401"))
                    {
                        operation.Responses["401"] = new OpenApiResponse
                        {
                            Description = "Unauthorized - Authentication required"
                        };
                    }

                    if (_add403 && !operation.Responses.ContainsKey("403"))
                    {
                        operation.Responses["403"] = new OpenApiResponse
                        {
                            Description = "Forbidden - Insufficient permissions"
                        };
                    }

                    if (_add500 && !operation.Responses.ContainsKey("500"))
                    {
                        operation.Responses["500"] = new OpenApiResponse
                        {
                            Description = "Internal Server Error"
                        };
                    }

                    if (_add503 && !operation.Responses.ContainsKey("503"))
                    {
                        operation.Responses["503"] = new OpenApiResponse
                        {
                            Description = "Service Unavailable"
                        };
                    }
                }
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Document transformer that adds deprecation notices to deprecated operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This transformer enhances deprecated operations with additional metadata
    /// such as sunset date and replacement information.
    /// </para>
    /// </remarks>
    public class DeprecationTransformer : IOpenApiDocumentTransformer
    {
        private readonly string _defaultMessage;
        private readonly DateTime? _sunsetDate;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeprecationTransformer"/> class.
        /// </summary>
        /// <param name="defaultMessage">The default deprecation message.</param>
        /// <param name="sunsetDate">The optional sunset date.</param>
        public DeprecationTransformer(
            string defaultMessage = "This operation is deprecated and will be removed in a future version.",
            DateTime? sunsetDate = null)
        {
            _defaultMessage = defaultMessage;
            _sunsetDate = sunsetDate;
        }

        /// <inheritdoc />
        public Task TransformAsync(
            OpenApiDocument document,
            OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken)
        {
            if (document.Paths == null) return Task.CompletedTask;
            foreach (IOpenApiPathItem path in document.Paths.Values)
            {
                if (path.Operations == null) continue;
                foreach (OpenApiOperation operation in path.Operations.Values)
                {
                    if (operation.Deprecated)
                    {
                        var sunsetInfo = _sunsetDate.HasValue
                            ? $" Sunset date: {_sunsetDate.Value:yyyy-MM-dd}."
                            : "";

                        operation.Description = $"⚠️ **DEPRECATED**: {_defaultMessage}{sunsetInfo}\n\n{operation.Description}";
                    }
                }
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Document transformer that filters operations by tag.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This transformer can be used to include or exclude operations based on their tags.
    /// </para>
    /// </remarks>
    public class TagFilterTransformer : IOpenApiDocumentTransformer
    {
        private readonly HashSet<string> _includeTags;
        private readonly HashSet<string> _excludeTags;

        /// <summary>
        /// Initializes a new instance of the <see cref="TagFilterTransformer"/> class.
        /// </summary>
        /// <param name="includeTags">Tags to include (null or empty means include all).</param>
        /// <param name="excludeTags">Tags to exclude.</param>
        public TagFilterTransformer(
            IEnumerable<string>? includeTags = null,
            IEnumerable<string>? excludeTags = null)
        {
            _includeTags = includeTags != null ? new HashSet<string>(includeTags, StringComparer.OrdinalIgnoreCase) : [];
            _excludeTags = excludeTags != null ? new HashSet<string>(excludeTags, StringComparer.OrdinalIgnoreCase) : [];
        }

        /// <inheritdoc />
        public Task TransformAsync(
            OpenApiDocument document,
            OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken)
        {
            var pathsToRemove = new List<string>();

            if (document.Paths == null) return Task.CompletedTask;

            foreach ((string? pathKey, IOpenApiPathItem? path) in document.Paths)
            {
                var operationsToRemove = new List<HttpMethod>();

                if (path.Operations == null) continue;
                foreach ((HttpMethod? operationType, OpenApiOperation? operation) in path.Operations)
                {
                    HashSet<string> operationTags = operation.Tags?.Select(t => t.Name).Where(n => n != null).Cast<string>().ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

                    // Check if should exclude
                    if (_excludeTags.Any() && operationTags.Overlaps(_excludeTags))
                    {
                        operationsToRemove.Add(operationType);
                        continue;
                    }

                    // Check if should include
                    if (_includeTags.Any() && !operationTags.Overlaps(_includeTags))
                    {
                        operationsToRemove.Add(operationType);
                    }
                }

                foreach (HttpMethod op in operationsToRemove)
                {
                    path.Operations.Remove(op);
                }

                if (!path.Operations.Any())
                {
                    pathsToRemove.Add(pathKey);
                }
            }

            foreach (var pathKey in pathsToRemove)
            {
                document.Paths.Remove(pathKey);
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Document transformer that adds ProblemDetails schema references.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This transformer adds RFC 7807 ProblemDetails schema references to error responses.
    /// </para>
    /// </remarks>
    public class ProblemDetailsTransformer : IOpenApiDocumentTransformer
    {
        /// <inheritdoc />
        public Task TransformAsync(
            OpenApiDocument document,
            OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken)
        {
            document.Components ??= new OpenApiComponents();
            document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();

            // Add ProblemDetails schema if not exists
            if (!document.Components.Schemas.ContainsKey("ProblemDetails"))
            {
                document.Components.Schemas["ProblemDetails"] = new OpenApiSchema
                {
                    Type = JsonSchemaType.Object,
                    Properties = new Dictionary<string, IOpenApiSchema>
                    {
                        ["type"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = "A URI reference that identifies the problem type." },
                        ["title"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = "A short, human-readable summary of the problem type." },
                        ["status"] = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32", Description = "The HTTP status code." },
                        ["detail"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = "A human-readable explanation specific to this occurrence of the problem." },
                        ["instance"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = "A URI reference that identifies the specific occurrence of the problem." },
                        ["traceId"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = "The trace identifier for the request." }
                    },
                    AdditionalPropertiesAllowed = true
                };
            }

            // Add ValidationProblemDetails schema if not exists
            if (!document.Components.Schemas.ContainsKey("ValidationProblemDetails"))
            {
                document.Components.Schemas["ValidationProblemDetails"] = new OpenApiSchema
                {
                    AllOf = [new OpenApiSchemaReference("ProblemDetails")],
                    Properties = new Dictionary<string, IOpenApiSchema>
                    {
                        ["errors"] = new OpenApiSchema
                        {
                            Type = JsonSchemaType.Object,
                            AdditionalProperties = new OpenApiSchema
                            {
                                Type = JsonSchemaType.Array,
                                Items = new OpenApiSchema { Type = JsonSchemaType.String }
                            },
                            Description = "The validation errors."
                        }
                    }
                };
            }

            // Update 4xx and 5xx responses to reference ProblemDetails
            if (document.Paths == null) return Task.CompletedTask;
            foreach (IOpenApiPathItem path in document.Paths.Values)
            {
                if (path.Operations == null) continue;
                foreach (OpenApiOperation operation in path.Operations.Values)
                {
                    foreach ((string? statusCode, IOpenApiResponse? response) in operation.Responses ?? [])
                    {
                        if (int.TryParse(statusCode, out var code) && code >= 400)
                        {
                            var schemaName = statusCode == "400" || statusCode == "422"
                                ? "ValidationProblemDetails"
                                : "ProblemDetails";

                            if (response is OpenApiResponse openApiResponse)
                            {
                                openApiResponse.Content ??= new Dictionary<string, OpenApiMediaType>();

                                if (!openApiResponse.Content.ContainsKey("application/problem+json"))
                                {
                                    openApiResponse.Content["application/problem+json"] = new OpenApiMediaType
                                    {
                                        Schema = new OpenApiSchemaReference(schemaName)
                                    };
                                }
                            }
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Operation transformer that adds rate limit headers to responses.
    /// </summary>
    public class RateLimitHeadersTransformer : IOpenApiDocumentTransformer
    {
        /// <inheritdoc />
        public Task TransformAsync(
            OpenApiDocument document,
            OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken)
        {
            if (document.Paths == null) return Task.CompletedTask;
            foreach (IOpenApiPathItem path in document.Paths.Values)
            {
                if (path.Operations == null) continue;
                foreach (OpenApiOperation operation in path.Operations.Values)
                {
                    foreach (IOpenApiResponse response in operation.Responses?.Values ?? Enumerable.Empty<IOpenApiResponse>())
                    {
                        if (response is not OpenApiResponse openApiResponse)
                        {
                            continue;
                        }

                        openApiResponse.Headers ??= new Dictionary<string, IOpenApiHeader>();
                        IDictionary<string, IOpenApiHeader> headers = openApiResponse.Headers;

                        if (!headers.ContainsKey("X-RateLimit-Limit"))
                        {
                            headers["X-RateLimit-Limit"] = new OpenApiHeader
                            {
                                Description = "The maximum number of requests allowed in the current window.",
                                Schema = new OpenApiSchema { Type = JsonSchemaType.Integer }
                            };
                        }

                        if (!headers.ContainsKey("X-RateLimit-Remaining"))
                        {
                            headers["X-RateLimit-Remaining"] = new OpenApiHeader
                            {
                                Description = "The number of requests remaining in the current window.",
                                Schema = new OpenApiSchema { Type = JsonSchemaType.Integer }
                            };
                        }

                        if (!headers.ContainsKey("X-RateLimit-Reset"))
                        {
                            headers["X-RateLimit-Reset"] = new OpenApiHeader
                            {
                                Description = "The time at which the current rate limit window resets (Unix timestamp).",
                                Schema = new OpenApiSchema { Type = JsonSchemaType.Integer }
                            };
                        }
                    }

                    // Add 429 response
                    operation.Responses ??= [];
                    if (!operation.Responses.ContainsKey("429"))
                    {
                        operation.Responses["429"] = new OpenApiResponse
                        {
                            Description = "Too Many Requests - Rate limit exceeded",
                            Headers = new Dictionary<string, IOpenApiHeader>
                            {
                                ["Retry-After"] = new OpenApiHeader
                                {
                                    Description = "The number of seconds to wait before retrying.",
                                    Schema = new OpenApiSchema { Type = JsonSchemaType.Integer }
                                }
                            }
                        };
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}

