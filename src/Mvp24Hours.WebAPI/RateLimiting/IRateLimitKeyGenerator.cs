//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Http;
using Mvp24Hours.WebAPI.Configuration;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.RateLimiting
{
    /// <summary>
    /// Interface for generating rate limit keys based on request context.
    /// </summary>
    public interface IRateLimitKeyGenerator
    {
        /// <summary>
        /// Generates a rate limit key for the given HTTP context and policy.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="policy">The rate limit policy.</param>
        /// <returns>The generated rate limit key.</returns>
        Task<string> GenerateKeyAsync(HttpContext context, RateLimitPolicy policy);

        /// <summary>
        /// Generates a rate limit key synchronously for the given HTTP context and policy.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="policy">The rate limit policy.</param>
        /// <returns>The generated rate limit key.</returns>
        string GenerateKey(HttpContext context, RateLimitPolicy policy);
    }
}

