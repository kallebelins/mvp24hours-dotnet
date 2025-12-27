//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Infrastructure.Caching
{
    /// <summary>
    /// Interface for serializing and deserializing cache values.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface abstracts the serialization mechanism used to store values in the cache.
    /// Different implementations can support JSON, MessagePack, Protobuf, or other formats.
    /// </para>
    /// <para>
    /// <strong>Supported Formats:</strong>
    /// <list type="bullet">
    /// <item>JSON (default, human-readable)</item>
    /// <item>MessagePack (compact, binary)</item>
    /// <item>Custom formats via implementation</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register serializer
    /// services.AddSingleton&lt;ICacheSerializer&gt;(new JsonCacheSerializer());
    /// 
    /// // Use in cache provider
    /// public class MyCacheProvider : ICacheProvider
    /// {
    ///     private readonly ICacheSerializer _serializer;
    ///     
    ///     public async Task SetAsync&lt;T&gt;(string key, T value, ...)
    ///     {
    ///         var bytes = await _serializer.SerializeAsync(value);
    ///         // Store bytes in cache
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface ICacheSerializer
    {
        /// <summary>
        /// Serializes an object to a byte array.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="value">The object to serialize.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The serialized byte array.</returns>
        Task<byte[]> SerializeAsync<T>(T value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deserializes a byte array to an object.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize.</typeparam>
        /// <param name="bytes">The byte array to deserialize.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The deserialized object, or null if deserialization fails.</returns>
        Task<T?> DeserializeAsync<T>(byte[] bytes, CancellationToken cancellationToken = default);

        /// <summary>
        /// Serializes an object to a string.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="value">The object to serialize.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The serialized string.</returns>
        Task<string> SerializeToStringAsync<T>(T value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deserializes a string to an object.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize.</typeparam>
        /// <param name="value">The string to deserialize.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The deserialized object, or null if deserialization fails.</returns>
        Task<T?> DeserializeFromStringAsync<T>(string value, CancellationToken cancellationToken = default);
    }
}

