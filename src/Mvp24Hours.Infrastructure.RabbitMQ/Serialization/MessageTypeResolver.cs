//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Serialization
{
    /// <summary>
    /// Default implementation of message type resolver.
    /// </summary>
    public class MessageTypeResolver : IMessageTypeResolver
    {
        private readonly ConcurrentDictionary<string, Type> _typeMap = new();
        private readonly string _messageTypeHeader;

        /// <summary>
        /// Creates a new message type resolver.
        /// </summary>
        /// <param name="messageTypeHeader">The header name to use for message type. Default is "x-message-type".</param>
        public MessageTypeResolver(string messageTypeHeader = "x-message-type")
        {
            _messageTypeHeader = messageTypeHeader;
        }

        /// <inheritdoc />
        public Type? ResolveType(IDictionary<string, object>? headers)
        {
            if (headers == null)
                return null;

            if (!headers.TryGetValue(_messageTypeHeader, out var typeNameObj))
                return null;

            var typeName = typeNameObj switch
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
                return null;

            // Try registered types first
            if (_typeMap.TryGetValue(typeName, out var registeredType))
                return registeredType;

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
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(typeName);
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
            var type = typeof(T);
            RegisterType(GetTypeName(type), type);
        }
    }
}

