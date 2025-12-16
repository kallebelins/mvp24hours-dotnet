//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract
{
    /// <summary>
    /// Interface for resolving message types from headers or type names.
    /// </summary>
    public interface IMessageTypeResolver
    {
        /// <summary>
        /// Resolves a message type from headers.
        /// </summary>
        /// <param name="headers">The message headers.</param>
        /// <returns>The resolved type, or null if not found.</returns>
        Type? ResolveType(IDictionary<string, object>? headers);

        /// <summary>
        /// Resolves a message type from a type name.
        /// </summary>
        /// <param name="typeName">The type name.</param>
        /// <returns>The resolved type, or null if not found.</returns>
        Type? ResolveType(string? typeName);

        /// <summary>
        /// Gets the type name for a message type.
        /// </summary>
        /// <param name="type">The message type.</param>
        /// <returns>The type name to use in headers.</returns>
        string GetTypeName(Type type);

        /// <summary>
        /// Registers a type mapping.
        /// </summary>
        /// <param name="typeName">The type name.</param>
        /// <param name="type">The corresponding type.</param>
        void RegisterType(string typeName, Type type);

        /// <summary>
        /// Registers a type using its full name as the type name.
        /// </summary>
        /// <typeparam name="T">The type to register.</typeparam>
        void RegisterType<T>();
    }
}

