//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract
{
    /// <summary>
    /// Interface for message serialization and deserialization.
    /// </summary>
    public interface IMessageSerializer
    {
        /// <summary>
        /// Gets the content type this serializer produces (e.g., "application/json").
        /// </summary>
        string ContentType { get; }

        /// <summary>
        /// Serializes an object to a byte array.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="value">The object to serialize.</param>
        /// <returns>The serialized byte array.</returns>
        byte[] Serialize<T>(T value);

        /// <summary>
        /// Serializes an object to a byte array.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="type">The type of the object.</param>
        /// <returns>The serialized byte array.</returns>
        byte[] Serialize(object value, Type type);

        /// <summary>
        /// Deserializes a byte array to an object.
        /// </summary>
        /// <typeparam name="T">The expected type.</typeparam>
        /// <param name="data">The byte array to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        T? Deserialize<T>(byte[] data);

        /// <summary>
        /// Deserializes a byte array to an object.
        /// </summary>
        /// <param name="data">The byte array to deserialize.</param>
        /// <param name="type">The expected type.</param>
        /// <returns>The deserialized object.</returns>
        object? Deserialize(byte[] data, Type type);
    }
}

