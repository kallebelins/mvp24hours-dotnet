using CustomerAPI.Typicode.Application.Configuration;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Pipe.Operations;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerAPI.Typicode.Application.Pipe.Operations.Customers;

/// <summary>
/// Outbound adapter step: fetches customers via IHttpClientFactory and the resilient named client registered by the host.
/// </summary>
public class GetCustomerClientStep(IHttpClientFactory httpClientFactory, IOptions<TypicodeOptions> options) : OperationBaseAsync
{
    public const string HttpClientName = "Typicode";

    public override async Task ExecuteAsync(IPipelineMessage input)
    {
        string url = options.Value.TypicodeCustomerUrl;

        if (!url.HasValue())
        {
            input.Messages.AddMessage("GetCustomerClientStep", "Typicode service url not found in configuration (appsettings).", Mvp24Hours.Core.Enums.MessageType.Error);
            return;
        }

        var client = httpClientFactory.CreateClient(HttpClientName);
        var cancellationToken = input.HasContent("cancellationToken")
            ? input.GetContent<CancellationToken>("cancellationToken")
            : CancellationToken.None;
        string response = await client.GetStringAsync(url, cancellationToken);

        var def = new[] {
            new {
                id = 0,
                name = string.Empty,
                username = string.Empty,
                email = string.Empty,
                phone = string.Empty,
                website = string.Empty
            }
        };

        var result = response.ToDeserializeAnonymous(def);

        if (result != null)
        {
            input.AddContent("customers", result);
        }
    }
}
