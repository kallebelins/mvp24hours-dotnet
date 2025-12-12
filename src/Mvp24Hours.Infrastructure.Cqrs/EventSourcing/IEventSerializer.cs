//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using CoreDomainEvent = Mvp24Hours.Core.Contract.Domain.Entity.IDomainEvent;

namespace Mvp24Hours.Infrastructure.Cqrs.EventSourcing;

/// <summary>
/// Interface for serializing and deserializing events.
/// </summary>
public interface IEventSerializer
{
    /// <summary>
    /// Serializes an event to a string representation.
    /// </summary>
    /// <param name="event">The event to serialize.</param>
    /// <returns>The serialized event string.</returns>
    string Serialize(object @event);

    /// <summary>
    /// Deserializes an event from its string representation.
    /// </summary>
    /// <param name="eventType">The type name of the event.</param>
    /// <param name="data">The serialized event data.</param>
    /// <returns>The deserialized event.</returns>
    CoreDomainEvent Deserialize(string eventType, string data);

    /// <summary>
    /// Deserializes data to a specific type.
    /// </summary>
    /// <param name="type">The target type.</param>
    /// <param name="data">The serialized data.</param>
    /// <returns>The deserialized object.</returns>
    object? Deserialize(Type type, string data);
}

/// <summary>
/// JSON-based event serializer using System.Text.Json.
/// </summary>
public class JsonEventSerializer : IEventSerializer
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance with default options.
    /// </summary>
    public JsonEventSerializer() : this(CreateDefaultOptions()) { }

    /// <summary>
    /// Initializes a new instance with custom options.
    /// </summary>
    /// <param name="options">The JSON serializer options.</param>
    public JsonEventSerializer(JsonSerializerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public string Serialize(object @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        return JsonSerializer.Serialize(@event, @event.GetType(), _options);
    }

    /// <inheritdoc />
    public CoreDomainEvent Deserialize(string eventType, string data)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type cannot be null or empty.", nameof(eventType));

        if (string.IsNullOrWhiteSpace(data))
            throw new ArgumentException("Data cannot be null or empty.", nameof(data));

        var type = Type.GetType(eventType);
        if (type == null)
        {
            throw new InvalidOperationException($"Cannot find type: {eventType}");
        }

        var result = JsonSerializer.Deserialize(data, type, _options);
        if (result == null)
        {
            throw new InvalidOperationException($"Failed to deserialize event of type: {eventType}");
        }

        return (CoreDomainEvent)result;
    }

    /// <inheritdoc />
    public object? Deserialize(Type type, string data)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (string.IsNullOrWhiteSpace(data))
            throw new ArgumentException("Data cannot be null or empty.", nameof(data));

        return JsonSerializer.Deserialize(data, type, _options);
    }

    private static JsonSerializerOptions CreateDefaultOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
    }
}

/// <summary>
/// Type resolver for event deserialization.
/// Allows custom type mapping for versioned events.
/// </summary>
public interface IEventTypeResolver
{
    /// <summary>
    /// Resolves an event type from its name.
    /// </summary>
    /// <param name="typeName">The event type name.</param>
    /// <returns>The resolved type, or null if not found.</returns>
    Type? Resolve(string typeName);

    /// <summary>
    /// Gets the type name for an event type.
    /// </summary>
    /// <param name="type">The event type.</param>
    /// <returns>The type name to store.</returns>
    string GetTypeName(Type type);
}

/// <summary>
/// Default type resolver using assembly qualified names.
/// </summary>
public class DefaultEventTypeResolver : IEventTypeResolver
{
    /// <inheritdoc />
    public Type? Resolve(string typeName)
    {
        return Type.GetType(typeName);
    }

    /// <inheritdoc />
    public string GetTypeName(Type type)
    {
        return type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
    }
}

/// <summary>
/// Type resolver that uses short names and a type registry.
/// Useful for event versioning and refactoring.
/// </summary>
public class RegistryEventTypeResolver : IEventTypeResolver
{
    private readonly Dictionary<string, Type> _typesByName = new();
    private readonly Dictionary<Type, string> _namesByType = new();

    /// <summary>
    /// Registers an event type with a name.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="name">The name to use for serialization.</param>
    public void Register<TEvent>(string name) where TEvent : CoreDomainEvent
    {
        Register(typeof(TEvent), name);
    }

    /// <summary>
    /// Registers an event type with a name.
    /// </summary>
    /// <param name="type">The event type.</param>
    /// <param name="name">The name to use for serialization.</param>
    public void Register(Type type, string name)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));

        _typesByName[name] = type;
        _namesByType[type] = name;
    }

    /// <inheritdoc />
    public Type? Resolve(string typeName)
    {
        if (_typesByName.TryGetValue(typeName, out var type))
        {
            return type;
        }

        // Fall back to assembly qualified name
        return Type.GetType(typeName);
    }

    /// <inheritdoc />
    public string GetTypeName(Type type)
    {
        if (_namesByType.TryGetValue(type, out var name))
        {
            return name;
        }

        // Fall back to assembly qualified name
        return type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
    }
}

