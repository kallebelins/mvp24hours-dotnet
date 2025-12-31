//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
            foreach (var path in document.Paths.Values)
            {
                foreach (var operation in path.Operations.Values)
                {
                    operation.Parameters ??= new List<OpenApiParameter>();

                    foreach (var (name, description, required) in _headers)
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
                            Schema = new OpenApiSchema { Type = "string" }
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
            foreach (var path in document.Paths.Values)
            {
                foreach (var operation in path.Operations.Values)
                {
                    operation.Responses ??= new OpenApiResponses();

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
            foreach (var path in document.Paths.Values)
            {
                foreach (var operation in path.Operations.Values)
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
            _includeTags = includeTags != null ? new HashSet<string>(includeTags, StringComparer.OrdinalIgnoreCase) : new();
            _excludeTags = excludeTags != null ? new HashSet<string>(excludeTags, StringComparer.OrdinalIgnoreCase) : new();
        }

        /// <inheritdoc />
        public Task TransformAsync(
            OpenApiDocument document,
            OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken)
        {
            var pathsToRemove = new List<string>();

            foreach (var (pathKey, path) in document.Paths)
            {
                var operationsToRemove = new List<OperationType>();

                foreach (var (operationType, operation) in path.Operations)
                {
                    var operationTags = operation.Tags?.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();

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

                foreach (var op in operationsToRemove)
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
            document.Components.Schemas ??= new Dictionary<string, OpenApiSchema>();

            // Add ProblemDetails schema if not exists
            if (!document.Components.Schemas.ContainsKey("ProblemDetails"))
            {
                document.Components.Schemas["ProblemDetails"] = new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>
                    {
                        ["type"] = new OpenApiSchema { Type = "string", Description = "A URI reference that identifies the problem type." },
                        ["title"] = new OpenApiSchema { Type = "string", Description = "A short, human-readable summary of the problem type." },
                        ["status"] = new OpenApiSchema { Type = "integer", Format = "int32", Description = "The HTTP status code." },
                        ["detail"] = new OpenApiSchema { Type = "string", Description = "A human-readable explanation specific to this occurrence of the problem." },
                        ["instance"] = new OpenApiSchema { Type = "string", Description = "A URI reference that identifies the specific occurrence of the problem." },
                        ["traceId"] = new OpenApiSchema { Type = "string", Description = "The trace identifier for the request." }
                    },
                    AdditionalPropertiesAllowed = true
                };
            }

            // Add ValidationProblemDetails schema if not exists
            if (!document.Components.Schemas.ContainsKey("ValidationProblemDetails"))
            {
                document.Components.Schemas["ValidationProblemDetails"] = new OpenApiSchema
                {
                    AllOf = new List<OpenApiSchema>
                    {
                        new OpenApiSchema
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.Schema,
                                Id = "ProblemDetails"
                            }
                        }
                    },
                    Properties = new Dictionary<string, OpenApiSchema>
                    {
                        ["errors"] = new OpenApiSchema
                        {
                            Type = "object",
                            AdditionalProperties = new OpenApiSchema
                            {
                                Type = "array",
                                Items = new OpenApiSchema { Type = "string" }
                            },
                            Description = "The validation errors."
                        }
                    }
                };
            }

            // Update 4xx and 5xx responses to reference ProblemDetails
            foreach (var path in document.Paths.Values)
            {
                foreach (var operation in path.Operations.Values)
                {
                    foreach (var (statusCode, response) in operation.Responses)
                    {
                        if (int.TryParse(statusCode, out var code) && code >= 400)
                        {
                            var schemaName = statusCode == "400" || statusCode == "422"
                                ? "ValidationProblemDetails"
                                : "ProblemDetails";

                            response.Content ??= new Dictionary<string, OpenApiMediaType>();

                            if (!response.Content.ContainsKey("application/problem+json"))
                            {
                                response.Content["application/problem+json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Reference = new OpenApiReference
                                        {
                                            Type = ReferenceType.Schema,
                                            Id = schemaName
                                        }
                                    }
                                };
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
            foreach (var path in document.Paths.Values)
            {
                foreach (var operation in path.Operations.Values)
                {
                    foreach (var response in operation.Responses.Values)
                    {
                        response.Headers ??= new Dictionary<string, OpenApiHeader>();

                        if (!response.Headers.ContainsKey("X-RateLimit-Limit"))
                        {
                            response.Headers["X-RateLimit-Limit"] = new OpenApiHeader
                            {
                                Description = "The maximum number of requests allowed in the current window.",
                                Schema = new OpenApiSchema { Type = "integer" }
                            };
                        }

                        if (!response.Headers.ContainsKey("X-RateLimit-Remaining"))
                        {
                            response.Headers["X-RateLimit-Remaining"] = new OpenApiHeader
                            {
                                Description = "The number of requests remaining in the current window.",
                                Schema = new OpenApiSchema { Type = "integer" }
                            };
                        }

                        if (!response.Headers.ContainsKey("X-RateLimit-Reset"))
                        {
                            response.Headers["X-RateLimit-Reset"] = new OpenApiHeader
                            {
                                Description = "The time at which the current rate limit window resets (Unix timestamp).",
                                Schema = new OpenApiSchema { Type = "integer" }
                            };
                        }
                    }

                    // Add 429 response
                    if (!operation.Responses.ContainsKey("429"))
                    {
                        operation.Responses["429"] = new OpenApiResponse
                        {
                            Description = "Too Many Requests - Rate limit exceeded",
                            Headers = new Dictionary<string, OpenApiHeader>
                            {
                                ["Retry-After"] = new OpenApiHeader
                                {
                                    Description = "The number of seconds to wait before retrying.",
                                    Schema = new OpenApiSchema { Type = "integer" }
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

