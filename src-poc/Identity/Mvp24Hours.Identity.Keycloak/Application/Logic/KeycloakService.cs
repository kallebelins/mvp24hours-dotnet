using IdentityModel.Client;
using Mvp24Hours.Extensions;
using Mvp24Hours.Identity.Keycloak.Core.ValueObjects.Authorization;
using System.Net.Http;
using System.Threading.Tasks;
using TokenClient = Mvp24Hours.Identity.Keycloak.Infrastructure.Clients.TokenClient;

namespace Mvp24Hours.Identity.Keycloak.Application.Logic
{
    public class KeycloakService
    {
        private readonly HttpClient _client;
        private readonly TokenClient _tokenClient;

        public KeycloakService(HttpClient client, TokenClient tokenClient)
        {
            _client = client;
            _tokenClient = tokenClient;
        }

        public async Task CreateResource(Resource resource)
        {
            var token = await _tokenClient.GetClientCredentialsToken();
            _client.SetBearerToken(token);
            await _client.HttpPostAsync("", resource.ToSerialize());
        }
    }
}
