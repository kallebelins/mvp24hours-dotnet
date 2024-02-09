//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Http;
using Mvp24Hours.Extensions;
using System;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Binders
{
    /// <summary>
    /// 
    /// </summary>
    public class ModelBinder<T> : IExtensionBinder<ModelBinder<T>>
        where T : class, new()
    {
        /// <summary>
        /// 
        /// </summary>
        public T Data { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public Exception Error { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public static ValueTask<ModelBinder<T>> BindAsync(HttpContext context)
        {
            Exception exception = null;
            T data = null;
            try
            {
                data = context.Request.GetFromQueryString<T>() ?? new();
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            ModelBinder<T> model = new()
            {
                Data = data,
                Error = exception
            };
            return ValueTask.FromResult(model);
        }
    }
}
