//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.ContentNegotiation
{
    /// <summary>
    /// Interface for content formatters that serialize objects to specific media types.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implement this interface to create custom content formatters for specific media types.
    /// </para>
    /// </remarks>
    public interface IContentFormatter
    {
        /// <summary>
        /// Gets the list of media types this formatter supports.
        /// </summary>
        IReadOnlyList<string> SupportedMediaTypes { get; }

        /// <summary>
        /// Gets the primary media type for this formatter.
        /// </summary>
        string PrimaryMediaType { get; }

        /// <summary>
        /// Determines whether this formatter can serialize the specified type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the formatter can serialize the type; otherwise, false.</returns>
        bool CanWrite(System.Type type);

        /// <summary>
        /// Serializes the specified object to a string.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <returns>The serialized string representation.</returns>
        string Serialize(object? value);

        /// <summary>
        /// Serializes the specified object to a stream asynchronously.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="value">The object to serialize.</param>
        /// <param name="encoding">The encoding to use.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SerializeAsync(Stream stream, object? value, Encoding encoding, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the content type header value for this formatter.
        /// </summary>
        /// <param name="charset">The charset to include, or null to omit.</param>
        /// <returns>The content type string.</returns>
        string GetContentType(string? charset = null);
    }

    /// <summary>
    /// Interface for content formatters that handle ProblemDetails serialization.
    /// </summary>
    public interface IProblemDetailsFormatter : IContentFormatter
    {
        /// <summary>
        /// Serializes ProblemDetails to a string.
        /// </summary>
        /// <param name="problemDetails">The ProblemDetails to serialize.</param>
        /// <returns>The serialized string representation.</returns>
        string SerializeProblemDetails(ProblemDetails problemDetails);

        /// <summary>
        /// Serializes ProblemDetails to a stream asynchronously.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="problemDetails">The ProblemDetails to serialize.</param>
        /// <param name="encoding">The encoding to use.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SerializeProblemDetailsAsync(Stream stream, ProblemDetails problemDetails, Encoding encoding, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the problem details content type (RFC 7807 compliant).
        /// </summary>
        /// <param name="charset">The charset to include, or null to omit.</param>
        /// <returns>The problem details content type string.</returns>
        string GetProblemDetailsContentType(string? charset = null);
    }

    /// <summary>
    /// Interface for content formatter registry.
    /// </summary>
    public interface IContentFormatterRegistry
    {
        /// <summary>
        /// Gets all registered formatters.
        /// </summary>
        IReadOnlyList<IContentFormatter> Formatters { get; }

        /// <summary>
        /// Gets the default formatter.
        /// </summary>
        IContentFormatter DefaultFormatter { get; }

        /// <summary>
        /// Gets a formatter for the specified media type.
        /// </summary>
        /// <param name="mediaType">The media type.</param>
        /// <returns>The formatter, or null if not found.</returns>
        IContentFormatter? GetFormatter(string mediaType);

        /// <summary>
        /// Gets a ProblemDetails formatter for the specified media type.
        /// </summary>
        /// <param name="mediaType">The media type.</param>
        /// <returns>The ProblemDetails formatter, or null if not found.</returns>
        IProblemDetailsFormatter? GetProblemDetailsFormatter(string mediaType);

        /// <summary>
        /// Determines if the specified media type is supported.
        /// </summary>
        /// <param name="mediaType">The media type to check.</param>
        /// <returns>True if supported; otherwise, false.</returns>
        bool IsSupported(string mediaType);

        /// <summary>
        /// Registers a custom formatter.
        /// </summary>
        /// <param name="formatter">The formatter to register.</param>
        void RegisterFormatter(IContentFormatter formatter);
    }
}

