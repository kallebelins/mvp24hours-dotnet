//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Mvp24Hours.Extensions;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.WebAPI.Filters.Swagger
{
    /// <summary>
    /// Remove lock icon from service
    /// </summary>
    /// <remarks>
    /// Add to swagger service registry => c.OperationFilter&lt;AuthResponsesOperationFilter&lt;AuthorizeAttribute&gt;&gt;();
    /// </remarks>
    /// <remarks>
    /// 
    /// </remarks>
    public class AuthResponsesOperationFilter(IEnumerable<Type> authTypes) : IOperationFilter
    {
        public IEnumerable<Type> AuthTypes { get; private set; } = authTypes;

        /// <summary>
        /// 
        /// </summary>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (!AuthTypes.AnySafe())
            {
                return;
            }

            var hasAuthAttributes = context.MethodInfo.DeclaringType.GetCustomAttributes(true)
                .Union(context.MethodInfo.GetCustomAttributes(true))
                .Where(x => AuthTypes.Contains(x.GetType()) && !x.GetType().Equals(typeof(AllowAnonymousAttribute)))
                .AnySafe();

            var hasAllowAnonymousAttributes = context.MethodInfo.DeclaringType.GetCustomAttributes(true)
                .Union(context.MethodInfo.GetCustomAttributes(true))
                .Where(x => x.GetType().Equals(typeof(AllowAnonymousAttribute)))
                .AnySafe();

            if (hasAuthAttributes && !hasAllowAnonymousAttributes)
            {
                var securityRequirement = new OpenApiSecurityRequirement()
                {
                    {
                        // Put here you own security scheme, this one is an example
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header,
                        },
                        new List<string>()
                    }
                };
                operation.Security = new List<OpenApiSecurityRequirement> { securityRequirement };
                operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
            }
        }
    }
}
