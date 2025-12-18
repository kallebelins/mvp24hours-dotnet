//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
namespace Mvp24Hours.Infrastructure.Http.Contract
{
    /// <summary>
    /// Alias for IHttpClientSerializer to provide a more specific name for content serialization.
    /// This interface defines the contract for HTTP content serialization (JSON, XML, MessagePack, etc.).
    /// </summary>
    /// <remarks>
    /// Use this interface when you need to explicitly reference content serialization.
    /// For backward compatibility, IHttpClientSerializer is still available.
    /// </remarks>
    public interface IHttpContentSerializer : IHttpClientSerializer
    {
        // This interface extends IHttpClientSerializer to provide semantic clarity.
        // All serializers implement both interfaces.
    }
}

