using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Options;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Infrastructure.Clients;

/// <summary>
/// Shared HTTP and error handling for Keycloak Admin REST API services.
/// </summary>
public sealed class KeycloakAdminHttpClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _client;

    public KeycloakAdminHttpClient(
        IHttpClientFactory httpClientFactory,
        IOptions<KeycloakAdminOptions> options)
    {
        _client = httpClientFactory.CreateClient("KeycloakAdmin");
        if (_client.BaseAddress is null)
        {
            string baseUrl = options.Value.AdminBaseUrl;
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                _client.BaseAddress = new Uri($"{baseUrl.TrimEnd('/')}/");
            }
        }
    }

    public Task<IBusinessResult<T>> GetAsync<T>(
        string path,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<T>(HttpMethod.Get, path, null, cancellationToken);
    }

    public Task<IBusinessResult<T>> PostAsync<T>(
        string path,
        object? body,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<T>(HttpMethod.Post, path, body, cancellationToken);
    }

    public Task<IBusinessResult<bool>> PostAsync(
        string path,
        object? body,
        CancellationToken cancellationToken = default)
    {
        return SendBooleanAsync(HttpMethod.Post, path, body, cancellationToken);
    }

    public Task<IBusinessResult<bool>> PutAsync(
        string path,
        object? body,
        CancellationToken cancellationToken = default)
    {
        return SendBooleanAsync(HttpMethod.Put, path, body, cancellationToken);
    }

    public Task<IBusinessResult<bool>> DeleteAsync(
        string path,
        object? body = null,
        CancellationToken cancellationToken = default)
    {
        return SendBooleanAsync(HttpMethod.Delete, path, body, cancellationToken);
    }

    public async Task<IBusinessResult<string>> PostForLocationAsync(
        string path,
        object body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpRequestMessage request = CreateRequest(HttpMethod.Post, path, body);
            using HttpResponseMessage response = await _client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return await ReadErrorAsync<string>(response, cancellationToken);
            }

            string? identifier = response.Headers.Location?.Segments
                .LastOrDefault(segment => !string.IsNullOrWhiteSpace(segment.Trim('/')))
                ?.Trim('/');
            return !string.IsNullOrWhiteSpace(identifier)
                ? BusinessResult.Success(identifier)
                : BusinessResult.Failure<string>(
                    "Keycloak did not return the created resource location.",
                    "KEYCLOAK_MISSING_LOCATION");
        }
        catch (HttpRequestException exception)
        {
            return BusinessResult.Failure<string>(exception);
        }
    }

    private async Task<IBusinessResult<T>> SendAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpRequestMessage request = CreateRequest(method, path, body);
            using HttpResponseMessage response = await _client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return await ReadErrorAsync<T>(response, cancellationToken);
            }

            T? data = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
            return data is not null
                ? BusinessResult.Success(data)
                : BusinessResult.Failure<T>(
                    "Keycloak returned an empty response.",
                    "KEYCLOAK_EMPTY_RESPONSE");
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            return BusinessResult.Failure<T>(exception);
        }
    }

    private async Task<IBusinessResult<bool>> SendBooleanAsync(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpRequestMessage request = CreateRequest(method, path, body);
            using HttpResponseMessage response = await _client.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode
                ? BusinessResult.Success(true)
                : await ReadErrorAsync<bool>(response, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            return BusinessResult.Failure<bool>(exception);
        }
    }

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string path,
        object? body)
    {
        HttpRequestMessage request = new(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        return request;
    }

    private static async Task<IBusinessResult<T>> ReadErrorAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        string body = await response.Content.ReadAsStringAsync(cancellationToken);
        KeycloakErrorResponse? error = null;
        try
        {
            error = JsonSerializer.Deserialize<KeycloakErrorResponse>(body, JsonOptions);
        }
        catch (JsonException)
        {
            // Preserve the raw response below.
        }

        string message = !string.IsNullOrWhiteSpace(error?.Message)
            ? error.Message
            : !string.IsNullOrWhiteSpace(body)
                ? body
                : $"Keycloak Admin API request failed with HTTP {(int)response.StatusCode}.";
        return BusinessResult.Failure<T>(
            message,
            error?.Error ?? $"KEYCLOAK_HTTP_{(int)response.StatusCode}");
    }
}
