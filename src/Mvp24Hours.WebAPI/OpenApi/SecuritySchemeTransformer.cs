//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Mvp24Hours.WebAPI.Configuration;

namespace Mvp24Hours.WebAPI.OpenApi;

/// <summary>
/// Document transformer that adds security schemes to the OpenAPI document.
/// </summary>
public class SecuritySchemeTransformer(NativeOpenApiOptions options) : IOpenApiDocumentTransformer
{
    private readonly NativeOpenApiOptions _options = options;

    /// <inheritdoc />
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Security ??= [];

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

    private static void AddSecurityRequirement(OpenApiDocument document, string schemeId, IList<string>? scopes = null)
    {
        document.Security!.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(schemeId, document)] = scopes is null ? [] : [.. scopes]
        });
    }

    private void AddBearerSecurityScheme(OpenApiDocument document)
    {
        OpenApiBearerSecurityScheme scheme = _options.BearerSecurityScheme ?? new OpenApiBearerSecurityScheme();

        document.Components!.SecuritySchemes!["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = scheme.Scheme.ToLowerInvariant(),
            BearerFormat = scheme.BearerFormat,
            Description = scheme.Description,
            In = ParameterLocation.Header
        };

        AddSecurityRequirement(document, "Bearer");
    }

    private void AddBasicSecurityScheme(OpenApiDocument document)
    {
        document.Components!.SecuritySchemes!["Basic"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "basic",
            Description = "Basic authentication. Enter your username and password.",
            In = ParameterLocation.Header
        };

        AddSecurityRequirement(document, "Basic");
    }

    private void AddApiKeySecurityScheme(OpenApiDocument document)
    {
        OpenApiApiKeySecurityScheme scheme = _options.ApiKeySecurityScheme ?? new OpenApiApiKeySecurityScheme();

        document.Components!.SecuritySchemes!["ApiKey"] = new OpenApiSecurityScheme
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

        AddSecurityRequirement(document, "ApiKey");
    }

    private void AddOAuth2SecurityScheme(OpenApiDocument document)
    {
        document.Components!.SecuritySchemes!["OAuth2"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Description = "OAuth2 authentication.",
            Flows = new OpenApiOAuthFlows
            {
                Implicit = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new System.Uri("https://example.com/oauth/authorize"),
                    Scopes = new Dictionary<string, string>
                    {
                        { "read", "Read access" },
                        { "write", "Write access" }
                    }
                }
            }
        };

        AddSecurityRequirement(document, "OAuth2", ["read", "write"]);
    }
}
