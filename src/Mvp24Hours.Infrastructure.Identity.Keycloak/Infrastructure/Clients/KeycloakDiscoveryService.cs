using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Options;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authentication;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Infrastructure.Clients;

/// <summary>
/// Retrieves and caches Keycloak OpenID Connect metadata.
/// </summary>
public sealed class KeycloakDiscoveryService(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    IOptions<KeycloakOptions> options) : IKeycloakDiscoveryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly KeycloakOptions _options = options.Value;

    public async Task<OpenIdConnectConfigurationDocument> GetConfigurationAsync(
        CancellationToken cancellationToken = default)
    {
        string metadataAddress = GetMetadataAddress();
        string cacheKey = $"Keycloak:Discovery:{metadataAddress}";
        if (cache.TryGetValue(cacheKey, out OpenIdConnectConfigurationDocument? cached)
            && cached is not null)
        {
            return cached;
        }

        ValidateMetadataAddress(metadataAddress);
        HttpClient client = httpClientFactory.CreateClient("KeycloakDiscovery");
        using HttpResponseMessage response = await client.GetAsync(metadataAddress, cancellationToken);
        response.EnsureSuccessStatusCode();

        OpenIdConnectConfigurationDocument? configuration =
            await response.Content.ReadFromJsonAsync<OpenIdConnectConfigurationDocument>(
                JsonOptions,
                cancellationToken) ?? throw new InvalidOperationException("Keycloak returned an empty discovery document.");
        cache.Set(cacheKey, configuration, _options.DiscoveryCacheTtl);
        return configuration;
    }

    public async Task<string> GetTokenEndpointAsync(CancellationToken cancellationToken = default)
    {
        return RequireEndpoint(
            (await GetConfigurationAsync(cancellationToken)).TokenEndpoint,
            "token_endpoint");
    }

    public async Task<string> GetIntrospectionEndpointAsync(
        CancellationToken cancellationToken = default)
    {
        return RequireEndpoint(
            (await GetConfigurationAsync(cancellationToken)).IntrospectionEndpoint,
            "introspection_endpoint");
    }

    public async Task<string> GetRevocationEndpointAsync(
        CancellationToken cancellationToken = default)
    {
        return RequireEndpoint(
            (await GetConfigurationAsync(cancellationToken)).RevocationEndpoint,
            "revocation_endpoint");
    }

    public async Task<string> GetJwksUriAsync(CancellationToken cancellationToken = default)
    {
        return RequireEndpoint(
            (await GetConfigurationAsync(cancellationToken)).JwksUri,
            "jwks_uri");
    }

    private string GetMetadataAddress()
    {
        return !string.IsNullOrWhiteSpace(_options.MetadataAddress)
            ? _options.MetadataAddress
            : $"{_options.Authority.TrimEnd('/')}/.well-known/openid-configuration";
    }

    private void ValidateMetadataAddress(string address)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out Uri? uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("Keycloak metadata address must be an absolute HTTP or HTTPS URL.");
        }

        if (_options.RequireHttpsMetadata && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                "Keycloak metadata address must use HTTPS when RequireHttpsMetadata is enabled.");
        }
    }

    private static string RequireEndpoint(string? endpoint, string name)
    {
        return !string.IsNullOrWhiteSpace(endpoint)
            ? endpoint
            : throw new InvalidOperationException(
                $"The Keycloak discovery document does not contain '{name}'.");
    }
}
