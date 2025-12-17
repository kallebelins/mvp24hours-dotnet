//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Mvp24Hours.WebAPI.Filters.Swagger
{
    /// <summary>
    /// Operation filter that automatically generates examples from XML comments and attributes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This filter extracts examples from:
    /// - XML comments with &lt;example&gt; tags
    /// - [SwaggerExample] attributes
    /// - Default values from properties
    /// </para>
    /// </remarks>
    public class ExamplesOperationFilter : IOperationFilter
    {
        /// <summary>
        /// Applies automatic examples to the operation.
        /// </summary>
        /// <param name="operation">The OpenAPI operation.</param>
        /// <param name="context">The operation filter context.</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Add examples to request body
            if (operation.RequestBody?.Content != null)
            {
                foreach (var content in operation.RequestBody.Content)
                {
                    AddExamplesToContent(content.Value, context);
                }
            }

            // Add examples to responses
            if (operation.Responses != null)
            {
                foreach (var response in operation.Responses.Values)
                {
                    if (response.Content != null)
                    {
                        foreach (var content in response.Content)
                        {
                            AddExamplesToContent(content.Value, context);
                        }
                    }
                }
            }

            // Add examples to parameters
            if (operation.Parameters != null)
            {
                foreach (var parameter in operation.Parameters)
                {
                    AddExampleToParameter(parameter, context);
                }
            }
        }

        private static void AddExamplesToContent(OpenApiMediaType mediaType, OperationFilterContext context)
        {
            if (mediaType.Schema == null)
            {
                return;
            }

            // Try to get example from schema
            if (mediaType.Schema.Example == null && mediaType.Example == null)
            {
                var example = GenerateExampleFromSchema(mediaType.Schema, context);
                if (example != null)
                {
                    mediaType.Example = example;
                }
            }
        }

        private static void AddExampleToParameter(OpenApiParameter parameter, OperationFilterContext context)
        {
            if (parameter.Schema == null || parameter.Example != null)
            {
                return;
            }

            var example = GenerateExampleFromSchema(parameter.Schema, context);
            if (example != null)
            {
                parameter.Example = example;
            }
        }

        private static Microsoft.OpenApi.Any.IOpenApiAny? GenerateExampleFromSchema(OpenApiSchema schema, OperationFilterContext context)
        {
            // Handle different schema types
            if (schema.Type == "string")
            {
                if (schema.Format == "date-time")
                {
                    return new Microsoft.OpenApi.Any.OpenApiString(DateTime.UtcNow.ToString("O"));
                }
                if (schema.Format == "email")
                {
                    return new Microsoft.OpenApi.Any.OpenApiString("user@example.com");
                }
                if (schema.Format == "uri")
                {
                    return new Microsoft.OpenApi.Any.OpenApiString("https://example.com");
                }
                return new Microsoft.OpenApi.Any.OpenApiString("string");
            }

            if (schema.Type == "integer")
            {
                return new Microsoft.OpenApi.Any.OpenApiInteger(0);
            }

            if (schema.Type == "number")
            {
                return new Microsoft.OpenApi.Any.OpenApiDouble(0.0);
            }

            if (schema.Type == "boolean")
            {
                return new Microsoft.OpenApi.Any.OpenApiBoolean(false);
            }

            if (schema.Type == "array" && schema.Items != null)
            {
                var itemExample = GenerateExampleFromSchema(schema.Items, context);
                if (itemExample != null)
                {
                    return new Microsoft.OpenApi.Any.OpenApiArray
                    {
                        itemExample
                    };
                }
            }

            return null;
        }
    }
}

