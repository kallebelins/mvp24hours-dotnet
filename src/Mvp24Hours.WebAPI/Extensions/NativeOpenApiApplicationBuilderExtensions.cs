//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using System.Linq;
using System.Text;

namespace Mvp24Hours.WebAPI.Extensions
{
    /// <summary>
    /// Extension methods for configuring native OpenAPI middleware in the application pipeline.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extensions configure the native OpenAPI document endpoint introduced in .NET 9,
    /// as well as optional Swagger UI and ReDoc visualization.
    /// </para>
    /// <para>
    /// <b>Usage:</b>
    /// Call <see cref="UseMvp24HoursNativeOpenApi"/> after <c>UseRouting()</c> and before <c>UseEndpoints()</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var app = builder.Build();
    /// 
    /// app.UseRouting();
    /// app.UseMvp24HoursNativeOpenApi();
    /// app.UseEndpoints(endpoints => { ... });
    /// </code>
    /// </example>
    public static class NativeOpenApiApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds native OpenAPI middleware to the application pipeline.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method configures:
        /// <list type="bullet">
        /// <item>The OpenAPI document endpoint at the configured route pattern</item>
        /// <item>Swagger UI (if enabled)</item>
        /// <item>ReDoc (if enabled)</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// app.UseMvp24HoursNativeOpenApi();
        /// </code>
        /// </example>
        public static IApplicationBuilder UseMvp24HoursNativeOpenApi(this IApplicationBuilder app)
        {
            var options = app.ApplicationServices.GetService<NativeOpenApiOptions>()
                ?? new NativeOpenApiOptions();

            return app.UseMvp24HoursNativeOpenApi(options);
        }

        /// <summary>
        /// Adds native OpenAPI middleware to the application pipeline with custom options.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="options">The OpenAPI options.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursNativeOpenApi(
            this IApplicationBuilder app,
            NativeOpenApiOptions options)
        {
            // Map the OpenAPI document endpoint
            app.UseRouting();

            // Add Swagger UI if enabled
            if (options.EnableSwaggerUI)
            {
                app.UseSwaggerUI(swaggerOptions =>
                {
                    swaggerOptions.RoutePrefix = options.SwaggerUIRoutePrefix;

                    // Add the main document
                    var documentPath = options.DocumentRoutePattern.Replace("{documentName}", options.DocumentName);
                    swaggerOptions.SwaggerEndpoint($"/{documentPath}", $"{options.Title} {options.Version}");

                    // Add additional version documents
                    foreach (var version in options.AdditionalVersions)
                    {
                        var versionDocPath = options.DocumentRoutePattern.Replace("{documentName}", version.DocumentName);
                        var versionTitle = version.IsDeprecated
                            ? $"{version.Title ?? options.Title} {version.Version} (Deprecated)"
                            : $"{version.Title ?? options.Title} {version.Version}";

                        swaggerOptions.SwaggerEndpoint($"/{versionDocPath}", versionTitle);
                    }

                    // Configure Swagger UI features
                    swaggerOptions.EnableDeepLinking();
                    swaggerOptions.EnableFilter();
                    swaggerOptions.EnableTryItOutByDefault();
                    swaggerOptions.DisplayRequestDuration();
                    swaggerOptions.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
                    swaggerOptions.DefaultModelsExpandDepth(2);
                    swaggerOptions.EnablePersistAuthorization();
                });
            }

            // ReDoc is handled via standalone HTML endpoint in MapMvp24HoursNativeOpenApi
            // This avoids requiring a separate NuGet package (Swashbuckle.AspNetCore.ReDoc)
            // If you need full ReDoc middleware support, add the Swashbuckle.AspNetCore.ReDoc package
            // and uncomment the code below:
            //
            // if (options.EnableReDoc)
            // {
            //     app.UseReDoc(reDocOptions =>
            //     {
            //         reDocOptions.RoutePrefix = options.ReDocRoutePrefix;
            //         var documentPath = options.DocumentRoutePattern.Replace("{documentName}", options.DocumentName);
            //         reDocOptions.SpecUrl = $"/{documentPath}";
            //         reDocOptions.DocumentTitle = $"{options.Title} - API Documentation";
            //     });
            // }

            return app;
        }

        /// <summary>
        /// Maps the OpenAPI document endpoints for a WebApplication (Minimal APIs).
        /// </summary>
        /// <param name="app">The web application.</param>
        /// <returns>The web application for chaining.</returns>
        /// <remarks>
        /// <para>
        /// Use this method for Minimal APIs (WebApplication) instead of <see cref="UseMvp24HoursNativeOpenApi(IApplicationBuilder)"/>.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var app = builder.Build();
        /// app.MapMvp24HoursNativeOpenApi();
        /// </code>
        /// </example>
        public static WebApplication MapMvp24HoursNativeOpenApi(this WebApplication app)
        {
            var options = app.Services.GetService<NativeOpenApiOptions>()
                ?? new NativeOpenApiOptions();

            return app.MapMvp24HoursNativeOpenApi(options);
        }

        /// <summary>
        /// Maps the OpenAPI document endpoints for a WebApplication with custom options.
        /// </summary>
        /// <param name="app">The web application.</param>
        /// <param name="options">The OpenAPI options.</param>
        /// <returns>The web application for chaining.</returns>
        public static WebApplication MapMvp24HoursNativeOpenApi(
            this WebApplication app,
            NativeOpenApiOptions options)
        {
            // Map the main OpenAPI document
            app.MapOpenApi(options.DocumentRoutePattern.Replace("{documentName}", options.DocumentName));

            // Map additional version documents
            foreach (var version in options.AdditionalVersions)
            {
                app.MapOpenApi(options.DocumentRoutePattern.Replace("{documentName}", version.DocumentName));
            }

            // Add an index endpoint listing all available documents
            var indexRoute = options.DocumentRoutePattern.Split('/').First();
            app.MapGet($"/{indexRoute}", (HttpContext context) =>
            {
                var documents = new System.Collections.Generic.List<object>
                {
                    new
                    {
                        name = options.DocumentName,
                        version = options.Version,
                        url = $"/{options.DocumentRoutePattern.Replace("{documentName}", options.DocumentName)}"
                    }
                };

                foreach (var version in options.AdditionalVersions)
                {
                    documents.Add(new
                    {
                        name = version.DocumentName,
                        version = version.Version,
                        deprecated = version.IsDeprecated,
                        url = $"/{options.DocumentRoutePattern.Replace("{documentName}", version.DocumentName)}"
                    });
                }

                return Results.Ok(new { documents });
            })
            .ExcludeFromDescription()
            .WithTags("OpenAPI");

            // Add Swagger UI redirect
            if (options.EnableSwaggerUI)
            {
                // Swagger UI is still handled by UseSwaggerUI middleware
                // But we can add a convenience redirect
                app.MapGet($"/{options.SwaggerUIRoutePrefix}", (HttpContext context) =>
                {
                    return Results.Redirect($"/{options.SwaggerUIRoutePrefix}/index.html");
                })
                .ExcludeFromDescription();

                // Add embedded Swagger UI page for Minimal APIs without UseSwaggerUI
                app.MapGet($"/{options.SwaggerUIRoutePrefix}/standalone", (HttpContext context) =>
                {
                    var html = GenerateSwaggerUIHtml(options);
                    return Results.Content(html, "text/html");
                })
                .ExcludeFromDescription();
            }

            // Add ReDoc redirect
            if (options.EnableReDoc)
            {
                app.MapGet($"/{options.ReDocRoutePrefix}", (HttpContext context) =>
                {
                    return Results.Redirect($"/{options.ReDocRoutePrefix}/index.html");
                })
                .ExcludeFromDescription();

                // Add embedded ReDoc page for Minimal APIs without UseReDoc
                app.MapGet($"/{options.ReDocRoutePrefix}/standalone", (HttpContext context) =>
                {
                    var html = GenerateReDocHtml(options);
                    return Results.Content(html, "text/html");
                })
                .ExcludeFromDescription();
            }

            return app;
        }

        private static string GenerateSwaggerUIHtml(NativeOpenApiOptions options)
        {
            var documentPath = options.DocumentRoutePattern.Replace("{documentName}", options.DocumentName);

            var urlsJs = new StringBuilder();
            urlsJs.Append('[');
            urlsJs.Append($"{{ url: '/{documentPath}', name: '{options.Title} {options.Version}' }}");

            foreach (var version in options.AdditionalVersions)
            {
                var versionDocPath = options.DocumentRoutePattern.Replace("{documentName}", version.DocumentName);
                var versionTitle = version.IsDeprecated
                    ? $"{version.Title ?? options.Title} {version.Version} (Deprecated)"
                    : $"{version.Title ?? options.Title} {version.Version}";
                urlsJs.Append($", {{ url: '/{versionDocPath}', name: '{versionTitle}' }}");
            }
            urlsJs.Append(']');

            return $@"<!DOCTYPE html>
<html>
<head>
    <title>{options.Title} - Swagger UI</title>
    <meta charset=""utf-8""/>
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <link rel=""stylesheet"" type=""text/css"" href=""https://unpkg.com/swagger-ui-dist@5/swagger-ui.css"">
</head>
<body>
    <div id=""swagger-ui""></div>
    <script src=""https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js""></script>
    <script src=""https://unpkg.com/swagger-ui-dist@5/swagger-ui-standalone-preset.js""></script>
    <script>
        window.onload = function() {{
            const ui = SwaggerUIBundle({{
                urls: {urlsJs},
                dom_id: '#swagger-ui',
                presets: [
                    SwaggerUIBundle.presets.apis,
                    SwaggerUIStandalonePreset
                ],
                layout: ""StandaloneLayout"",
                deepLinking: true,
                showExtensions: true,
                showCommonExtensions: true,
                tryItOutEnabled: true,
                persistAuthorization: true,
                displayRequestDuration: true,
                filter: true
            }});
            window.ui = ui;
        }};
    </script>
</body>
</html>";
        }

        private static string GenerateReDocHtml(NativeOpenApiOptions options)
        {
            var documentPath = options.DocumentRoutePattern.Replace("{documentName}", options.DocumentName);

            return $@"<!DOCTYPE html>
<html>
<head>
    <title>{options.Title} - API Documentation</title>
    <meta charset=""utf-8""/>
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <link href=""https://fonts.googleapis.com/css?family=Montserrat:300,400,700|Roboto:300,400,700"" rel=""stylesheet"">
    <style>
        body {{
            margin: 0;
            padding: 0;
        }}
    </style>
</head>
<body>
    <redoc spec-url='/{documentPath}'></redoc>
    <script src=""https://cdn.redoc.ly/redoc/latest/bundles/redoc.standalone.js""></script>
</body>
</html>";
        }
    }
}

