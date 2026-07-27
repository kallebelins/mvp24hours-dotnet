using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Options;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authentication;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Infrastructure.Clients;

/// <summary>
/// Adds a Keycloak service-account bearer token to Admin API requests.
/// </summary>
public sealed class KeycloakAdminBearerDelegatingHandler(
    KeycloakTokenClient tokenClient,
    IMemoryCache cache,
    IOptions<KeycloakAdminOptions> options)
    : DelegatingHandler
{
    private readonly KeycloakAdminOptions _options = options.Value;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync(cancellationToken));
        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        string cacheKey = $"Keycloak:AdminToken:{_options.Realm}:{_options.ClientId}";
        if (cache.TryGetValue(cacheKey, out string? cached)
            && !string.IsNullOrWhiteSpace(cached))
        {
            return cached;
        }

        Dictionary<string, string> form = new()
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _options.ClientId
        };
        if (!string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            form["client_secret"] = _options.ClientSecret;
        }

        using HttpResponseMessage response =
            await tokenClient.RequestTokenAsync(form, cancellationToken);
        response.EnsureSuccessStatusCode();
        AccessTokenResponse token = await response.Content.ReadFromJsonAsync<AccessTokenResponse>(
            cancellationToken)
            ?? throw new HttpRequestException(
                "Keycloak returned an empty Admin API token response.");
        if (string.IsNullOrWhiteSpace(token.AccessToken))
        {
            throw new HttpRequestException(
                "Keycloak did not return an Admin API access token.");
        }

        cache.Set(
            cacheKey,
            token.AccessToken,
            TimeSpan.FromSeconds(Math.Max(1, token.ExpiresIn - 30)));
        return token.AccessToken;
    }
}
