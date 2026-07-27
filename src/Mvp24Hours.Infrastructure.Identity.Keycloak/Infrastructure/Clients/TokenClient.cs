using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authentication;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Infrastructure.Clients;

/// <summary>
/// Minimal Keycloak token client using raw HTTP (no IdentityModel).
/// Expanded in task 4.5 via <c>IKeycloakTokenService</c>.
/// </summary>
public class TokenClient(HttpClient client, ClientCredentialsTokenRequest tokenRequest)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _client = client;
    private readonly ClientCredentialsTokenRequest _tokenRequest = tokenRequest;

    public async Task<string?> GetClientCredentialsToken(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_tokenRequest.Address))
        {
            throw new InvalidOperationException("ClientCredentialsTokenRequest.Address is required.");
        }

        if (string.IsNullOrWhiteSpace(_tokenRequest.ClientId))
        {
            throw new InvalidOperationException("ClientCredentialsTokenRequest.ClientId is required.");
        }

        Dictionary<string, string> form = new()
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _tokenRequest.ClientId,
            ["client_secret"] = _tokenRequest.ClientSecret ?? string.Empty
        };

        if (!string.IsNullOrWhiteSpace(_tokenRequest.Scope))
        {
            form["scope"] = _tokenRequest.Scope;
        }

        using FormUrlEncodedContent content = new(form);
        using HttpResponseMessage response = await _client.PostAsync(
            _tokenRequest.Address,
            content,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Keycloak token request failed with {(int)response.StatusCode}: {body}");
        }

        AccessTokenResponse? tokenResponse = await response.Content.ReadFromJsonAsync<AccessTokenResponse>(
            JsonOptions,
            cancellationToken);

        return tokenResponse?.AccessToken;
    }

    public static void SetBearerToken(HttpClient httpClient, string accessToken)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
    }
}
