//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Linq;

namespace Mvp24Hours.WebAPI.Configuration
{
    /// <summary>
    /// Configures Swagger generation options for API versioning.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class configures Swagger documents for each API version discovered via IApiVersionDescriptionProvider.
    /// </para>
    /// </remarks>
    public class ConfigureSwaggerGenOptions : IConfigureOptions<SwaggerGenOptions>
    {
        private readonly IApiVersionDescriptionProvider _provider;
        private readonly SwaggerOptions _swaggerOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigureSwaggerGenOptions"/> class.
        /// </summary>
        /// <param name="provider">The API version description provider.</param>
        /// <param name="swaggerOptions">The Swagger options.</param>
        public ConfigureSwaggerGenOptions(
            IApiVersionDescriptionProvider provider,
            SwaggerOptions swaggerOptions)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _swaggerOptions = swaggerOptions ?? throw new ArgumentNullException(nameof(swaggerOptions));
        }

        /// <summary>
        /// Configures Swagger generation options.
        /// </summary>
        /// <param name="options">The Swagger generation options.</param>
        public void Configure(SwaggerGenOptions options)
        {
            // Configure each API version
            foreach (var description in _provider.ApiVersionDescriptions)
            {
                var versionInfo = _swaggerOptions.Versions.FirstOrDefault(v => 
                    v.Version == description.ApiVersion.ToString() || 
                    v.Version == description.GroupName)
                    ?? new SwaggerVersionInfo
                    {
                        Version = description.ApiVersion.ToString(),
                        Title = $"{_swaggerOptions.Title} {description.ApiVersion}",
                        Description = _swaggerOptions.Description
                    };

                var openApiInfo = new OpenApiInfo
                {
                    Title = versionInfo.Title,
                    Version = versionInfo.Version,
                    Description = versionInfo.Description ?? _swaggerOptions.Description,
                    Contact = _swaggerOptions.Contact != null ? new OpenApiContact
                    {
                        Name = _swaggerOptions.Contact.Name,
                        Email = _swaggerOptions.Contact.Email,
                        Url = !string.IsNullOrEmpty(_swaggerOptions.Contact.Url) ? new Uri(_swaggerOptions.Contact.Url) : null
                    } : null,
                    License = _swaggerOptions.License != null ? new OpenApiLicense
                    {
                        Name = _swaggerOptions.License.Name,
                        Url = !string.IsNullOrEmpty(_swaggerOptions.License.Url) ? new Uri(_swaggerOptions.License.Url) : null
                    } : null
                };
                
                options.SwaggerGeneratorOptions.SwaggerDocs.Add(description.GroupName, openApiInfo);
            }
        }
    }
}

