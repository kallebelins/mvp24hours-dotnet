//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Http.Contract
{
    /// <summary>
    /// Defines the contract for HTTP content serialization.
    /// </summary>
    public interface IHttpClientSerializer
    {
        /// <summary>
        /// Gets the media type produced by this serializer (e.g., "application/json").
        /// </summary>
        string MediaType { get; }

        /// <summary>
        /// Serializes the specified object to HTTP content.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <returns>The serialized HTTP content.</returns>
        HttpContent Serialize(object? value);

        /// <summary>
        /// Deserializes the HTTP content to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="content">The HTTP content to deserialize.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The deserialized object.</returns>
        Task<T?> DeserializeAsync<T>(HttpContent content, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Deserializes the string content to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="content">The string content to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        T? Deserialize<T>(string content) where T : class;
    }
}

