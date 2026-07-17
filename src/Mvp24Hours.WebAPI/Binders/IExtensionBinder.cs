//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Mvp24Hours.WebAPI.Binders
{
    /// <summary>
    /// 
    /// </summary>
    public interface IExtensionBinder<T>
        where T : class, new()
    {
        /// <summary>
        /// 
        /// </summary>
        static abstract ValueTask<T> BindAsync(HttpContext context);
    }
}
