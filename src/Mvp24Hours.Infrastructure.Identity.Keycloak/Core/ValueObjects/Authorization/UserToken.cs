using System.Text.Json;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization;

/// <summary>
/// Parsed representation of a Keycloak access token claims set.
/// </summary>
public sealed record UserToken
{
    public Guid? Id { get; init; }

    public string? Name { get; init; }

    public string? PreferredUserName { get; init; }

    public string? Email { get; init; }

    public bool? EmailVerified { get; init; }

    public string? Scope { get; init; }

    public Guid? SessionId { get; init; }

    public Guid? SessionState { get; init; }

    public string? AuthorizedParty { get; init; }

    public IReadOnlyList<string>? AllowedOrigins { get; init; }

    public IReadOnlyList<string>? RealmRoles { get; init; }

    /// <summary>
    /// Flattened roles from a single resource (legacy/convenience). Prefer <see cref="ClientRoles"/>.
    /// </summary>
    public IReadOnlyList<string>? ResourceRoles { get; init; }

    /// <summary>
    /// Client roles keyed by client id (from <c>resource_access</c>).
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? ClientRoles { get; init; }

    public IReadOnlyList<string>? Groups { get; init; }

    public IReadOnlyDictionary<string, IReadOnlyList<string>>? Attributes { get; init; }

    public DateTimeOffset? IssuedAt { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }

    public bool HasRealmRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role) || RealmRoles is null)
        {
            return false;
        }

        return RealmRoles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasClientRole(string clientId, string role)
    {
        if (string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(role)
            || ClientRoles is null
            || !ClientRoles.TryGetValue(clientId, out IReadOnlyList<string>? roles)
            || roles is null)
        {
            return false;
        }

        return roles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasGroup(string group)
    {
        if (string.IsNullOrWhiteSpace(group) || Groups is null)
        {
            return false;
        }

        return Groups.Any(g => string.Equals(g, group, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Builds a <see cref="UserToken"/> from a JWT payload JSON object.
    /// </summary>
    public static UserToken? FromJwtPayloadJson(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return null;
        }

        using var document = JsonDocument.Parse(payloadJson);
        return FromJsonElement(document.RootElement);
    }

    /// <summary>
    /// Builds a <see cref="UserToken"/> from a JWT payload <see cref="JsonElement"/>.
    /// </summary>
    public static UserToken FromJsonElement(JsonElement payload)
    {
        Dictionary<string, IReadOnlyList<string>>? clientRoles = ParseClientRoles(payload);
        var flattenedResourceRoles = clientRoles?
            .SelectMany(pair => pair.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new UserToken
        {
            Id = GetGuid(payload, "sub"),
            Name = GetString(payload, "name"),
            PreferredUserName = GetString(payload, "preferred_username"),
            Email = GetString(payload, "email"),
            EmailVerified = GetBoolean(payload, "email_verified"),
            Scope = GetString(payload, "scope"),
            SessionId = GetGuid(payload, "sid"),
            SessionState = GetGuid(payload, "session_state"),
            AuthorizedParty = GetString(payload, "azp"),
            AllowedOrigins = GetStringArray(payload, "allowed-origins"),
            RealmRoles = GetNestedStringArray(payload, "realm_access", "roles"),
            ResourceRoles = flattenedResourceRoles,
            ClientRoles = clientRoles,
            Groups = GetStringArray(payload, "groups") ?? GetStringArray(payload, "group"),
            Attributes = ParseAttributes(payload),
            IssuedAt = GetUnixTimestamp(payload, "iat"),
            ExpiresAt = GetUnixTimestamp(payload, "exp")
        };
    }

    private static Dictionary<string, IReadOnlyList<string>>? ParseClientRoles(JsonElement payload)
    {
        if (!TryGetProperty(payload, "resource_access", out JsonElement resourceAccess)
            || resourceAccess.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        Dictionary<string, IReadOnlyList<string>> result = new(StringComparer.OrdinalIgnoreCase);

        foreach (JsonProperty client in resourceAccess.EnumerateObject())
        {
            if (client.Value.ValueKind != JsonValueKind.Object
                || !client.Value.TryGetProperty("roles", out JsonElement rolesElement)
                || rolesElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            List<string> roles =
            [
                .. rolesElement.EnumerateArray()
                    .Select(item => item.GetString())
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Select(item => item!)
            ];

            if (roles.Count > 0)
            {
                result[client.Name] = roles;
            }
        }

        return result.Count > 0 ? result : null;
    }

    private static Dictionary<string, IReadOnlyList<string>>? ParseAttributes(JsonElement payload)
    {
        if (!TryGetProperty(payload, "attributes", out JsonElement attributes)
            || attributes.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        Dictionary<string, IReadOnlyList<string>> result = new(StringComparer.OrdinalIgnoreCase);

        foreach (JsonProperty attribute in attributes.EnumerateObject())
        {
            if (attribute.Value.ValueKind == JsonValueKind.Array)
            {
                List<string> values =
                [
                    .. attribute.Value.EnumerateArray()
                        .Select(item => item.GetString())
                        .Where(item => !string.IsNullOrWhiteSpace(item))
                        .Select(item => item!)
                ];

                if (values.Count > 0)
                {
                    result[attribute.Name] = values;
                }
            }
            else if (attribute.Value.ValueKind == JsonValueKind.String)
            {
                string? value = attribute.Value.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    result[attribute.Name] = [value];
                }
            }
        }

        return result.Count > 0 ? result : null;
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out value))
        {
            return true;
        }

        value = default;
        return false;
    }

    private static string? GetString(JsonElement payload, string propertyName)
    {
        return TryGetProperty(payload, propertyName, out JsonElement value)
            && value.ValueKind != JsonValueKind.Null
            && value.ValueKind != JsonValueKind.Undefined
            ? value.ToString()
            : null;
    }

    private static Guid? GetGuid(JsonElement payload, string propertyName)
    {
        return Guid.TryParse(GetString(payload, propertyName), out Guid value) ? value : null;
    }

    private static bool? GetBoolean(JsonElement payload, string propertyName)
    {
        if (!TryGetProperty(payload, propertyName, out JsonElement value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(value.GetString(), out bool parsed) ? parsed : null,
            _ => null
        };
    }

    private static DateTimeOffset? GetUnixTimestamp(JsonElement payload, string propertyName)
    {
        if (!TryGetProperty(payload, propertyName, out JsonElement value))
        {
            return null;
        }

        long seconds = value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt64(out long number) => number,
            JsonValueKind.String when long.TryParse(value.GetString(), out long parsed) => parsed,
            _ => 0
        };

        return seconds > 0
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;
    }

    private static List<string>? GetStringArray(JsonElement payload, string propertyName)
    {
        if (!TryGetProperty(payload, propertyName, out JsonElement value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        List<string> items =
        [
            .. value.EnumerateArray()
                .Select(item => item.GetString())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item!)
        ];

        return items.Count > 0 ? items : null;
    }

    private static List<string>? GetNestedStringArray(
        JsonElement payload,
        string propertyName,
        params string[] path)
    {
        if (!TryGetProperty(payload, propertyName, out JsonElement current))
        {
            return null;
        }

        foreach (string segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object
                || !current.TryGetProperty(segment, out current))
            {
                return null;
            }
        }

        if (current.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        List<string> items =
        [
            .. current.EnumerateArray()
                .Select(item => item.GetString())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item!)
        ];

        return items.Count > 0 ? items : null;
    }
}
