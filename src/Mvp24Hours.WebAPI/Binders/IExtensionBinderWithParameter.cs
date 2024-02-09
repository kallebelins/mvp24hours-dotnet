//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Binders
{
    /// <summary>
    /// 
    /// </summary>
    public interface IExtensionBinderWithParameter<T>
        where T : class, new()
    {
        /// <summary>
        /// 
        /// </summary>
        static abstract ValueTask<T> BindAsync(HttpContext context, ParameterInfo parameter);
    }
}
