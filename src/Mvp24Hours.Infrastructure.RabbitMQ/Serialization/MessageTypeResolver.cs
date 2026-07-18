//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Serialization;

/// <summary>
/// Default implementation of message type resolver.
/// </summary>
/// <remarks>
/// Creates a new message type resolver.
/// </remarks>
/// <param name="messageTypeHeader">The header name to use for message type. Default is "x-message-type".</param>
public class MessageTypeResolver(string messageTypeHeader = "x-message-type") : IMessageTypeResolver
{
    private readonly ConcurrentDictionary<string, Type> _typeMap = new();
    private readonly string _messageTypeHeader = messageTypeHeader;

    /// <inheritdoc />
    public Type? ResolveType(IDictionary<string, object>? headers)
    {
        if (headers == null)
        {
            return null;
        }

        if (!headers.TryGetValue(_messageTypeHeader, out object? typeNameObj))
        {
            return null;
        }

        string? typeName = typeNameObj switch
        {
            string s => s,
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            _ => typeNameObj?.ToString()
        };

        return ResolveType(typeName);
    }

    /// <inheritdoc />
    public Type? ResolveType(string? typeName)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            return null;
        }

        // Try registered types first
        if (_typeMap.TryGetValue(typeName, out Type? registeredType))
        {
            return registeredType;
        }

        // Try to resolve by assembly qualified name
        try
        {
            var type = Type.GetType(typeName);
            if (type != null)
            {
                _typeMap.TryAdd(typeName, type);
                return type;
            }
        }
        catch
        {
            // Ignore type resolution errors
        }

        // Try to find type by name across loaded assemblies
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                Type? type = assembly.GetType(typeName);
                if (type != null)
                {
                    _typeMap.TryAdd(typeName, type);
                    return type;
                }
            }
            catch
            {
                // Ignore assembly scanning errors
            }
        }

        return null;
    }

    /// <inheritdoc />
    public string GetTypeName(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.FullName ?? type.Name;
    }

    /// <inheritdoc />
    public void RegisterType(string typeName, Type type)
    {
        ArgumentNullException.ThrowIfNull(typeName);
        ArgumentNullException.ThrowIfNull(type);
        _typeMap[typeName] = type;
    }

    /// <inheritdoc />
    public void RegisterType<T>()
    {
        Type type = typeof(T);
        RegisterType(GetTypeName(type), type);
    }
}

