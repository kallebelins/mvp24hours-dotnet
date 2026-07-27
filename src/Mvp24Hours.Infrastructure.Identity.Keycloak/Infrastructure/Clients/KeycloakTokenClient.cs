using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Infrastructure;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Infrastructure.Clients;

/// <summary>
/// Low-level first-party OAuth client for Keycloak token endpoints.
/// </summary>
public sealed class KeycloakTokenClient(
    IHttpClientFactory httpClientFactory,
    IKeycloakDiscoveryService discoveryService)
{
    public async Task<HttpResponseMessage> RequestTokenAsync(
        IReadOnlyDictionary<string, string> form,
        CancellationToken cancellationToken = default)
    {
        string endpoint = await discoveryService.GetTokenEndpointAsync(cancellationToken);
        return await PostAsync(endpoint, form, cancellationToken);
    }

    public async Task<HttpResponseMessage> IntrospectAsync(
        IReadOnlyDictionary<string, string> form,
        CancellationToken cancellationToken = default)
    {
        string endpoint = await discoveryService.GetIntrospectionEndpointAsync(cancellationToken);
        return await PostAsync(endpoint, form, cancellationToken);
    }

    public async Task<HttpResponseMessage> RevokeAsync(
        IReadOnlyDictionary<string, string> form,
        CancellationToken cancellationToken = default)
    {
        string endpoint = await discoveryService.GetRevocationEndpointAsync(cancellationToken);
        return await PostAsync(endpoint, form, cancellationToken);
    }

    private async Task<HttpResponseMessage> PostAsync(
        string endpoint,
        IReadOnlyDictionary<string, string> form,
        CancellationToken cancellationToken)
    {
        HttpClient client = httpClientFactory.CreateClient("KeycloakToken");
        using FormUrlEncodedContent content = new(form);
        return await client.PostAsync(endpoint, content, cancellationToken);
    }
}
