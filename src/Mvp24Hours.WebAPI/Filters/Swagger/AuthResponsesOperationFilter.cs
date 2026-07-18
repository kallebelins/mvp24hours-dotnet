//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Mvp24Hours.Extensions;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Mvp24Hours.WebAPI.Filters.Swagger;

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

        object[] declaringAttrs = context.MethodInfo.DeclaringType?.GetCustomAttributes(true) ?? [];
        object[] methodAttrs = context.MethodInfo.GetCustomAttributes(true);

        bool hasAuthAttributes = declaringAttrs
            .Union(methodAttrs)
            .Where(x => AuthTypes.Contains(x.GetType()) && !x.GetType().Equals(typeof(AllowAnonymousAttribute)))
            .AnySafe();

        bool hasAllowAnonymousAttributes = declaringAttrs
            .Union(methodAttrs)
            .Where(x => x.GetType().Equals(typeof(AllowAnonymousAttribute)))
            .AnySafe();

        if (hasAuthAttributes && !hasAllowAnonymousAttributes)
        {
            var securityRequirement = new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
            };
            operation.Security = [securityRequirement];
            operation.Responses ??= [];
            operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
        }
    }
}
