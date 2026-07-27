using System.Net.Http.Headers;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authentication;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Infrastructure.Clients;

/// <summary>
/// Adds a Keycloak service-account bearer token to Admin API requests.
/// </summary>
public sealed class KeycloakAdminBearerDelegatingHandler(IKeycloakTokenService tokenService)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        IBusinessResult<AccessTokenResponse> tokenResult = await tokenService.GetClientCredentialsTokenAsync(cancellationToken);
        if (tokenResult.HasErrors || string.IsNullOrWhiteSpace(tokenResult.Data?.AccessToken))
        {
            string message = tokenResult.Messages?.FirstOrDefault()?.Message
                ?? "Unable to obtain a Keycloak Admin API access token.";
            throw new HttpRequestException(message);
        }

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenResult.Data.AccessToken);
        return await base.SendAsync(request, cancellationToken);
    }
}
