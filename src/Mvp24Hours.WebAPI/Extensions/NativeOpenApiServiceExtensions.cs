//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.OpenApi;
using System;
using System.Linq;

namespace Mvp24Hours.WebAPI.Extensions
{
    /// <summary>
    /// Extension methods for configuring native OpenAPI support (.NET 9+).
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extensions provide integration with Microsoft.AspNetCore.OpenApi, the native OpenAPI
    /// support introduced in .NET 9. This is a lightweight alternative to Swashbuckle that is
    /// fully AOT-compatible and provides core OpenAPI functionality.
    /// </para>
    /// <para>
    /// <b>Benefits over Swashbuckle:</b>
    /// <list type="bullet">
    /// <item>Native .NET integration</item>
    /// <item>AOT-compatible</item>
    /// <item>Smaller footprint</item>
    /// <item>Better performance</item>
    /// <item>First-party support from Microsoft</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Note:</b> Swagger UI and ReDoc are still provided via their respective packages
    /// for visualization. The native OpenAPI only generates the document.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic configuration
    /// builder.Services.AddMvp24HoursNativeOpenApi(options =>
    /// {
    ///     options.Title = "My API";
    ///     options.Version = "1.0.0";
    ///     options.Description = "My API Description";
    /// });
    /// 
    /// // In the app pipeline
    /// app.UseMvp24HoursNativeOpenApi();
    /// </code>
    /// </example>
    public static class NativeOpenApiServiceExtensions
    {
        /// <summary>
        /// Adds native OpenAPI (.NET 9+) services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Configuration action for OpenAPI options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method configures the native OpenAPI document generation introduced in .NET 9.
        /// It uses the Microsoft.AspNetCore.OpenApi package instead of Swashbuckle.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursNativeOpenApi(options =>
        /// {
        ///     options.Title = "My API";
        ///     options.Version = "1.0.0";
        ///     options.Description = "My API Description";
        ///     options.AuthenticationScheme = OpenApiAuthenticationScheme.Bearer;
        ///     options.Contact = new OpenApiContactInfo
        ///     {
        ///         Name = "API Support",
        ///         Email = "support@example.com",
        ///         Url = "https://example.com/support"
        ///     };
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursNativeOpenApi(
            this IServiceCollection services,
            Action<NativeOpenApiOptions>? configureOptions = null)
        {
            var options = new NativeOpenApiOptions();
            configureOptions?.Invoke(options);

            // Store options for later use
            services.AddSingleton(options);
            services.Configure<NativeOpenApiOptions>(opt =>
            {
                opt.DocumentName = options.DocumentName;
                opt.Title = options.Title;
                opt.Version = options.Version;
                opt.Description = options.Description;
                opt.Contact = options.Contact;
                opt.License = options.License;
                opt.TermsOfServiceUrl = options.TermsOfServiceUrl;
                opt.EnableSwaggerUI = options.EnableSwaggerUI;
                opt.SwaggerUIRoutePrefix = options.SwaggerUIRoutePrefix;
                opt.EnableReDoc = options.EnableReDoc;
                opt.ReDocRoutePrefix = options.ReDocRoutePrefix;
                opt.DocumentRoutePattern = options.DocumentRoutePattern;
                opt.IncludeServerInfo = options.IncludeServerInfo;
                opt.Servers = options.Servers;
                opt.AuthenticationScheme = options.AuthenticationScheme;
                opt.BearerSecurityScheme = options.BearerSecurityScheme;
                opt.ApiKeySecurityScheme = options.ApiKeySecurityScheme;
                opt.AdditionalVersions = options.AdditionalVersions;
                opt.XmlCommentsFilePath = options.XmlCommentsFilePath;
                opt.Tags = options.Tags;
                opt.ShowOperationIds = options.ShowOperationIds;
                opt.GenerateExamplesFromXmlComments = options.GenerateExamplesFromXmlComments;
                opt.ExternalDocsUrl = options.ExternalDocsUrl;
                opt.ExternalDocsDescription = options.ExternalDocsDescription;
            });

            // Configure the main OpenAPI document
            services.AddOpenApi(options.DocumentName, openApiOptions =>
            {
                ConfigureOpenApiDocument(openApiOptions, options);
            });

            // Configure additional version documents
            foreach (var version in options.AdditionalVersions)
            {
                var versionOptions = new NativeOpenApiOptions
                {
                    DocumentName = version.DocumentName,
                    Title = version.Title ?? options.Title,
                    Version = version.Version,
                    Description = version.Description ?? options.Description,
                    Contact = options.Contact,
                    License = options.License,
                    TermsOfServiceUrl = options.TermsOfServiceUrl,
                    AuthenticationScheme = options.AuthenticationScheme,
                    BearerSecurityScheme = options.BearerSecurityScheme,
                    ApiKeySecurityScheme = options.ApiKeySecurityScheme,
                    Tags = options.Tags
                };

                services.AddOpenApi(version.DocumentName, openApiOptions =>
                {
                    ConfigureOpenApiDocument(openApiOptions, versionOptions);

                    // Add deprecation warning if applicable
                    if (version.IsDeprecated)
                    {
                        openApiOptions.AddDocumentTransformer((document, context, ct) =>
                        {
                            var deprecationNote = version.DeprecationMessage ?? "This API version is deprecated.";
                            document.Info.Description = $"⚠️ **DEPRECATED**: {deprecationNote}\n\n{document.Info.Description}";
                            return System.Threading.Tasks.Task.CompletedTask;
                        });
                    }
                });
            }

            // Add Swagger UI if enabled
            if (options.EnableSwaggerUI)
            {
                services.AddEndpointsApiExplorer();
            }

            return services;
        }

        /// <summary>
        /// Adds native OpenAPI with multiple API versions support.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Configuration action for OpenAPI options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursNativeOpenApiWithVersions(options =>
        /// {
        ///     options.Title = "My API";
        ///     options.AdditionalVersions.Add(new OpenApiVersionConfig
        ///     {
        ///         DocumentName = "v1",
        ///         Version = "1.0.0",
        ///         Title = "My API v1"
        ///     });
        ///     options.AdditionalVersions.Add(new OpenApiVersionConfig
        ///     {
        ///         DocumentName = "v2",
        ///         Version = "2.0.0",
        ///         Title = "My API v2"
        ///     });
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursNativeOpenApiWithVersions(
            this IServiceCollection services,
            Action<NativeOpenApiOptions> configureOptions)
        {
            return services.AddMvp24HoursNativeOpenApi(configureOptions);
        }

        /// <summary>
        /// Adds a minimal native OpenAPI configuration with sensible defaults.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="title">The API title.</param>
        /// <param name="version">The API version (default: "1.0.0").</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursNativeOpenApiMinimal("My API", "1.0.0");
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursNativeOpenApiMinimal(
            this IServiceCollection services,
            string title,
            string version = "1.0.0")
        {
            return services.AddMvp24HoursNativeOpenApi(options =>
            {
                options.Title = title;
                options.Version = version;
                options.EnableSwaggerUI = true;
            });
        }

        private static void ConfigureOpenApiDocument(OpenApiOptions openApiOptions, NativeOpenApiOptions options)
        {
            // Add document transformer for basic info
            openApiOptions.AddDocumentTransformer((document, context, ct) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = options.Title,
                    Version = options.Version,
                    Description = options.Description,
                    TermsOfService = !string.IsNullOrEmpty(options.TermsOfServiceUrl)
                        ? new Uri(options.TermsOfServiceUrl)
                        : null,
                    Contact = options.Contact != null ? new OpenApiContact
                    {
                        Name = options.Contact.Name,
                        Email = options.Contact.Email,
                        Url = !string.IsNullOrEmpty(options.Contact.Url)
                            ? new Uri(options.Contact.Url)
                            : null
                    } : null,
                    License = options.License != null ? new OpenApiLicense
                    {
                        Name = options.License.Name,
                        Url = !string.IsNullOrEmpty(options.License.Url)
                            ? new Uri(options.License.Url)
                            : null
                        // Note: Identifier property requires OpenAPI 3.1 (Microsoft.OpenApi 1.6.13+)
                        // It is supported in OpenApiLicenseInfo but not used here for compatibility
                    } : null
                };

                return System.Threading.Tasks.Task.CompletedTask;
            });

            // Add server information transformer
            if (options.IncludeServerInfo && options.Servers.Any())
            {
                openApiOptions.AddDocumentTransformer((document, context, ct) =>
                {
                    document.Servers = options.Servers.Select(s => new OpenApiServer
                    {
                        Url = s.Url,
                        Description = s.Description,
                        Variables = s.Variables.ToDictionary(
                            v => v.Key,
                            v => new Microsoft.OpenApi.Models.OpenApiServerVariable
                            {
                                Default = v.Value.Default,
                                Description = v.Value.Description,
                                Enum = v.Value.Enum
                            })
                    }).ToList();

                    return System.Threading.Tasks.Task.CompletedTask;
                });
            }

            // Add security scheme transformer
            if (options.AuthenticationScheme != OpenApiAuthenticationScheme.None)
            {
                openApiOptions.AddDocumentTransformer(new SecuritySchemeTransformer(options));
            }

            // Add tags transformer
            if (options.Tags.Any())
            {
                openApiOptions.AddDocumentTransformer((document, context, ct) =>
                {
                    document.Tags = options.Tags.Select(t => new OpenApiTag
                    {
                        Name = t.Name,
                        Description = t.Description,
                        ExternalDocs = !string.IsNullOrEmpty(t.ExternalDocsUrl)
                            ? new OpenApiExternalDocs { Url = new Uri(t.ExternalDocsUrl) }
                            : null
                    }).ToList();

                    return System.Threading.Tasks.Task.CompletedTask;
                });
            }

            // Add external docs transformer
            if (!string.IsNullOrEmpty(options.ExternalDocsUrl))
            {
                openApiOptions.AddDocumentTransformer((document, context, ct) =>
                {
                    document.ExternalDocs = new OpenApiExternalDocs
                    {
                        Url = new Uri(options.ExternalDocsUrl),
                        Description = options.ExternalDocsDescription
                    };

                    return System.Threading.Tasks.Task.CompletedTask;
                });
            }
        }
    }
}

