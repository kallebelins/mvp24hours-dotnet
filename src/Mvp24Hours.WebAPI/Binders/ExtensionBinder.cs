//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Http;
using Mvp24Hours.Extensions;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Binders
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class ExtensionBinder<T>
        where T : class, new()
    {
        /// <summary>
        /// 
        /// </summary>
        public static ValueTask<T> BindAsync(HttpContext context)
        {
            T result = context.Request.GetFromQueryString<T>() ?? new T();
            return ValueTask.FromResult(result);
        }
    }
}
