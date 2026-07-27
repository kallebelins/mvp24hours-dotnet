using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Options;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authentication;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Infrastructure.Clients;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Application.Logic;

/// <summary>
/// Implements Keycloak OAuth token operations without third-party OIDC clients.
/// </summary>
public sealed class KeycloakTokenService(
    KeycloakTokenClient client,
    IMemoryCache cache,
    IOptions<KeycloakOptions> options) : IKeycloakTokenService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly KeycloakOptions _options = options.Value;

    public async Task<IBusinessResult<AccessTokenResponse>> GetClientCredentialsTokenAsync(
        CancellationToken cancellationToken = default)
    {
        string cacheKey = $"Keycloak:ClientCredentials:{_options.Authority}:{_options.ClientId}";
        if (cache.TryGetValue(cacheKey, out AccessTokenResponse? cached)
            && cached is not null)
        {
            return BusinessResult.Success(cached);
        }

        Dictionary<string, string> form = CreateClientForm();
        form["grant_type"] = "client_credentials";
        IBusinessResult<AccessTokenResponse> result =
            await RequestTokenAsync(form, cancellationToken);
        if (!result.HasErrors && result.Data is { AccessToken.Length: > 0 } token)
        {
            cache.Set(
                cacheKey,
                token,
                TimeSpan.FromSeconds(Math.Max(1, token.ExpiresIn - 30)));
        }

        return result;
    }

    public Task<IBusinessResult<AccessTokenResponse>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Task.FromResult(
                BusinessResult.Failure<AccessTokenResponse>(
                    "Refresh token is required.",
                    "KEYCLOAK_VALIDATION"));
        }

        Dictionary<string, string> form = CreateClientForm();
        form["grant_type"] = "refresh_token";
        form["refresh_token"] = refreshToken;
        return RequestTokenAsync(form, cancellationToken);
    }

    public async Task<IBusinessResult<TokenIntrospectionResponse>> IntrospectTokenAsync(
        string token,
        string? tokenTypeHint = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return BusinessResult.Failure<TokenIntrospectionResponse>(
                "Token is required.",
                "KEYCLOAK_VALIDATION");
        }

        Dictionary<string, string> form = CreateClientForm();
        form["token"] = token;
        AddOptional(form, "token_type_hint", tokenTypeHint);

        try
        {
            using HttpResponseMessage response = await client.IntrospectAsync(form, cancellationToken);
            return await ReadResponseAsync<TokenIntrospectionResponse>(response, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            return BusinessResult.Failure<TokenIntrospectionResponse>(exception);
        }
    }

    public async Task<IBusinessResult<bool>> RevokeTokenAsync(
        string token,
        string? tokenTypeHint = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return BusinessResult.Failure<bool>("Token is required.", "KEYCLOAK_VALIDATION");
        }

        Dictionary<string, string> form = CreateClientForm();
        form["token"] = token;
        AddOptional(form, "token_type_hint", tokenTypeHint);

        try
        {
            using HttpResponseMessage response = await client.RevokeAsync(form, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return BusinessResult.Success(true);
            }

            return await ReadErrorAsync<bool>(response, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            return BusinessResult.Failure<bool>(exception);
        }
    }

    public Task<IBusinessResult<AccessTokenResponse>> GetPasswordTokenAsync(
        string username,
        string password,
        string? scope = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return Task.FromResult(
                BusinessResult.Failure<AccessTokenResponse>(
                    "Username and password are required.",
                    "KEYCLOAK_VALIDATION"));
        }

        Dictionary<string, string> form = CreateClientForm();
        form["grant_type"] = "password";
        form["username"] = username;
        form["password"] = password;
        AddOptional(form, "scope", scope);
        return RequestTokenAsync(form, cancellationToken);
    }

    private async Task<IBusinessResult<AccessTokenResponse>> RequestTokenAsync(
        IReadOnlyDictionary<string, string> form,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await client.RequestTokenAsync(form, cancellationToken);
            return await ReadResponseAsync<AccessTokenResponse>(response, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            return BusinessResult.Failure<AccessTokenResponse>(exception);
        }
    }

    private Dictionary<string, string> CreateClientForm()
    {
        Dictionary<string, string> form = new()
        {
            ["client_id"] = _options.ClientId
        };
        AddOptional(form, "client_secret", _options.ClientSecret);
        return form;
    }

    private static async Task<IBusinessResult<T>> ReadResponseAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
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
                : $"Keycloak request failed with HTTP {(int)response.StatusCode}.";
        return BusinessResult.Failure<T>(
            message,
            error?.Error ?? $"KEYCLOAK_HTTP_{(int)response.StatusCode}");
    }

    private static void AddOptional(
        IDictionary<string, string> form,
        string key,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            form[key] = value;
        }
    }
}
