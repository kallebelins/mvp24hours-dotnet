//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.WebAPI.Filters.Swagger
{
    /// <summary>
    /// Operation filter that adds deprecation warnings to deprecated API endpoints.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This filter checks for the [ApiVersion] attribute with Deprecated = true
    /// and adds deprecation information to the OpenAPI operation.
    /// </para>
    /// </remarks>
    public class DeprecationOperationFilter : IOperationFilter
    {
        /// <summary>
        /// Applies deprecation information to the operation if the endpoint is deprecated.
        /// </summary>
        /// <param name="operation">The OpenAPI operation.</param>
        /// <param name="context">The operation filter context.</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Check for deprecated API version on controller
            var controllerApiVersion = context.MethodInfo.DeclaringType?
                .GetCustomAttributes(typeof(ApiVersionAttribute), false)
                .Cast<ApiVersionAttribute>()
                .FirstOrDefault();

            if (controllerApiVersion?.Deprecated == true)
            {
                operation.Deprecated = true;
                AddDeprecationWarning(operation, controllerApiVersion);
            }

            // Check for deprecated API version on action
            var actionApiVersion = context.MethodInfo
                .GetCustomAttributes(typeof(ApiVersionAttribute), false)
                .Cast<ApiVersionAttribute>()
                .FirstOrDefault();

            if (actionApiVersion?.Deprecated == true)
            {
                operation.Deprecated = true;
                AddDeprecationWarning(operation, actionApiVersion);
            }

            // Check for [Obsolete] attribute
            var obsoleteAttribute = context.MethodInfo
                .GetCustomAttributes(typeof(ObsoleteAttribute), false)
                .Cast<ObsoleteAttribute>()
                .FirstOrDefault();

            if (obsoleteAttribute != null)
            {
                operation.Deprecated = true;
                if (!string.IsNullOrWhiteSpace(obsoleteAttribute.Message))
                {
                    AddDeprecationWarning(operation, obsoleteAttribute.Message);
                }
            }
        }

        private static void AddDeprecationWarning(OpenApiOperation operation, ApiVersionAttribute apiVersion)
        {
            var message = "This API version is deprecated.";
            
            if (apiVersion.Deprecated)
            {
                message = $"API version {apiVersion.Versions.FirstOrDefault()} is deprecated.";
            }

            AddDeprecationWarning(operation, message);
        }

        private static void AddDeprecationWarning(OpenApiOperation operation, string message)
        {
            if (operation.Extensions == null)
            {
                operation.Extensions = new Dictionary<string, Microsoft.OpenApi.Interfaces.IOpenApiExtension>();
            }

            // Add deprecation warning to operation description
            if (string.IsNullOrWhiteSpace(operation.Description))
            {
                operation.Description = $"⚠️ **DEPRECATED**: {message}";
            }
            else
            {
                operation.Description = $"⚠️ **DEPRECATED**: {message}\n\n{operation.Description}";
            }

            // Add x-deprecation-warning extension
            operation.Extensions["x-deprecation-warning"] = new Microsoft.OpenApi.Any.OpenApiString(message);
        }
    }
}

