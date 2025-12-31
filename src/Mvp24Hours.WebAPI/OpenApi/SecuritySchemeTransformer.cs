//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using Mvp24Hours.WebAPI.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.OpenApi
{
    /// <summary>
    /// Document transformer that adds security schemes to the OpenAPI document.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This transformer adds authentication/authorization security schemes based on the
    /// configured <see cref="NativeOpenApiOptions.AuthenticationScheme"/>.
    /// </para>
    /// <para>
    /// Supported schemes:
    /// <list type="bullet">
    /// <item>Bearer (JWT)</item>
    /// <item>Basic</item>
    /// <item>API Key</item>
    /// <item>OAuth2</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class SecuritySchemeTransformer : IOpenApiDocumentTransformer
    {
        private readonly NativeOpenApiOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecuritySchemeTransformer"/> class.
        /// </summary>
        /// <param name="options">The OpenAPI options.</param>
        public SecuritySchemeTransformer(NativeOpenApiOptions options)
        {
            _options = options;
        }

        /// <inheritdoc />
        public Task TransformAsync(
            OpenApiDocument document,
            OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken)
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new System.Collections.Generic.Dictionary<string, OpenApiSecurityScheme>();

            switch (_options.AuthenticationScheme)
            {
                case OpenApiAuthenticationScheme.Bearer:
                    AddBearerSecurityScheme(document);
                    break;

                case OpenApiAuthenticationScheme.Basic:
                    AddBasicSecurityScheme(document);
                    break;

                case OpenApiAuthenticationScheme.ApiKey:
                    AddApiKeySecurityScheme(document);
                    break;

                case OpenApiAuthenticationScheme.OAuth2:
                    AddOAuth2SecurityScheme(document);
                    break;
            }

            return Task.CompletedTask;
        }

        private void AddBearerSecurityScheme(OpenApiDocument document)
        {
            var scheme = _options.BearerSecurityScheme ?? new OpenApiBearerSecurityScheme();

            var securityScheme = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = scheme.Scheme.ToLowerInvariant(),
                BearerFormat = scheme.BearerFormat,
                Description = scheme.Description,
                In = ParameterLocation.Header
            };

            document.Components.SecuritySchemes["Bearer"] = securityScheme;

            // Add security requirement
            document.SecurityRequirements.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    System.Array.Empty<string>()
                }
            });
        }

        private void AddBasicSecurityScheme(OpenApiDocument document)
        {
            var securityScheme = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "basic",
                Description = "Basic authentication. Enter your username and password.",
                In = ParameterLocation.Header
            };

            document.Components.SecuritySchemes["Basic"] = securityScheme;

            // Add security requirement
            document.SecurityRequirements.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Basic"
                        }
                    },
                    System.Array.Empty<string>()
                }
            });
        }

        private void AddApiKeySecurityScheme(OpenApiDocument document)
        {
            var scheme = _options.ApiKeySecurityScheme ?? new OpenApiApiKeySecurityScheme();

            var securityScheme = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                Name = scheme.Name,
                Description = scheme.Description,
                In = scheme.Location switch
                {
                    ApiKeyLocation.Header => ParameterLocation.Header,
                    ApiKeyLocation.Query => ParameterLocation.Query,
                    ApiKeyLocation.Cookie => ParameterLocation.Cookie,
                    _ => ParameterLocation.Header
                }
            };

            document.Components.SecuritySchemes["ApiKey"] = securityScheme;

            // Add security requirement
            document.SecurityRequirements.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "ApiKey"
                        }
                    },
                    System.Array.Empty<string>()
                }
            });
        }

        private void AddOAuth2SecurityScheme(OpenApiDocument document)
        {
            var securityScheme = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Description = "OAuth2 authentication.",
                Flows = new OpenApiOAuthFlows
                {
                    Implicit = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new System.Uri("https://example.com/oauth/authorize"),
                        Scopes = new System.Collections.Generic.Dictionary<string, string>
                        {
                            { "read", "Read access" },
                            { "write", "Write access" }
                        }
                    }
                }
            };

            document.Components.SecuritySchemes["OAuth2"] = securityScheme;

            // Add security requirement
            document.SecurityRequirements.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "OAuth2"
                        }
                    },
                    new[] { "read", "write" }
                }
            });
        }
    }
}

