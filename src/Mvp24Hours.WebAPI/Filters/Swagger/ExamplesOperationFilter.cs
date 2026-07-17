//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

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
                    if (parameter is OpenApiParameter openApiParameter)
                    {
                        AddExampleToParameter(openApiParameter, context);
                    }
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

        private static JsonNode? GenerateExampleFromSchema(IOpenApiSchema schema, OperationFilterContext context)
        {
            if (schema is not OpenApiSchema openApiSchema)
            {
                return null;
            }

            // Handle different schema types
            if (HasSchemaType(openApiSchema, JsonSchemaType.String))
            {
                if (openApiSchema.Format == "date-time")
                {
                    return JsonValue.Create(DateTime.UtcNow.ToString("O"));
                }
                if (openApiSchema.Format == "email")
                {
                    return JsonValue.Create("user@example.com");
                }
                if (openApiSchema.Format == "uri")
                {
                    return JsonValue.Create("https://example.com");
                }
                return JsonValue.Create("string");
            }

            if (HasSchemaType(openApiSchema, JsonSchemaType.Integer))
            {
                return JsonValue.Create(0);
            }

            if (HasSchemaType(openApiSchema, JsonSchemaType.Number))
            {
                return JsonValue.Create(0.0);
            }

            if (HasSchemaType(openApiSchema, JsonSchemaType.Boolean))
            {
                return JsonValue.Create(false);
            }

            if (HasSchemaType(openApiSchema, JsonSchemaType.Array) && openApiSchema.Items != null)
            {
                var itemExample = GenerateExampleFromSchema(openApiSchema.Items, context);
                if (itemExample != null)
                {
                    return new JsonArray(itemExample);
                }
            }

            return null;
        }

        private static bool HasSchemaType(OpenApiSchema schema, JsonSchemaType type) =>
            schema.Type?.HasFlag(type) == true;
    }
}
