using CustomerAPI.Core.Ports;
using CustomerAPI.Core.ValueObjects;
using CustomerAPI.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerAPI.Infrastructure.Adapters.Http
{
    /// <summary>
    /// Outbound HTTP adapter implementing <see cref="IExternalProfilePort"/>.
    /// Uses a named <see cref="IHttpClientFactory"/> client registered with
    /// <c>AddStandardResilienceHandler</c> (retry + circuit-breaker) at the composition root.
    /// </summary>
    public class TypicodeProfileAdapter(
        IHttpClientFactory httpClientFactory,
        IOptions<TypicodeOptions> options,
        ILogger<TypicodeProfileAdapter> logger) : IExternalProfilePort
    {
        public const string HttpClientName = "Typicode";

        public async Task<IList<ExternalProfile>> GetProfilesAsync(CancellationToken cancellationToken = default)
        {
            logger.LogDebug("Fetching all external profiles from {Url}", options.Value.UsersUrl);

            var client = httpClientFactory.CreateClient(HttpClientName);

            var raw = await client.GetFromJsonAsync<IList<TypicodeUserDto>>(options.Value.UsersUrl, cancellationToken);

            if (raw == null)
                return [];

            return raw.Select(Map).ToList();
        }

        public async Task<ExternalProfile?> GetProfileByIdAsync(int externalId, CancellationToken cancellationToken = default)
        {
            logger.LogDebug("Fetching external profile id={Id} from {Url}", externalId, options.Value.UsersUrl);

            var client = httpClientFactory.CreateClient(HttpClientName);

            var url = $"{options.Value.UsersUrl.TrimEnd('/')}/{externalId}";

            var raw = await client.GetFromJsonAsync<TypicodeUserDto>(url, cancellationToken);

            return raw == null ? null : Map(raw);
        }

        private static ExternalProfile Map(TypicodeUserDto dto) => new()
        {
            Id = dto.Id,
            Name = dto.Name ?? string.Empty,
            Username = dto.Username ?? string.Empty,
            Email = dto.Email ?? string.Empty,
            Phone = dto.Phone ?? string.Empty,
            Website = dto.Website ?? string.Empty
        };

        private sealed class TypicodeUserDto
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public string? Username { get; set; }
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public string? Website { get; set; }
        }
    }
}
