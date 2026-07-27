using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization;
using KeycloakTokenClient =
    Mvp24Hours.Infrastructure.Identity.Keycloak.Infrastructure.Clients.TokenClient;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Application.Logic;

public class KeycloakService(HttpClient client, KeycloakTokenClient tokenClient)
{
    private readonly HttpClient _client = client;
    private readonly KeycloakTokenClient _tokenClient = tokenClient;

    public async Task CreateResource(Resource resource)
    {
        string? token = await _tokenClient.GetClientCredentialsToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Keycloak did not return an access token.");
        }

        KeycloakTokenClient.SetBearerToken(_client, token);
        await _client.HttpPostAsync(string.Empty, resource.ToSerialize());
    }
}
