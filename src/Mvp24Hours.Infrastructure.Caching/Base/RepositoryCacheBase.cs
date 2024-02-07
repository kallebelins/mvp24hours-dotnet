//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Distributed;

namespace Mvp24Hours.Infrastructure.Caching.Base
{
    /// <summary>
    ///  <see cref="Core.Contract.Data.IRepositoryCacheAsync{T}"/>
    /// </summary>
    public class RepositoryCacheBase(IDistributedCache cache)
    {
        private readonly IDistributedCache _cache = cache ?? throw new System.ArgumentNullException(nameof(cache), "An instance of IDistributedCache is required for this key and value repository.");

        protected virtual IDistributedCache Cache => _cache;
    }
}
