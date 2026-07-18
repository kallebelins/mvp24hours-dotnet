//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Mvp24Hours.WebAPI.Filters.Swagger;

public class CustomSwaggerFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var nonMobileRoutes = swaggerDoc.Paths
            .Where(predicate: x => !x.Key.Contains("public", System.StringComparison.CurrentCultureIgnoreCase))
            .ToList();
        nonMobileRoutes.ForEach(x => swaggerDoc.Paths.Remove(x.Key));
    }
}
