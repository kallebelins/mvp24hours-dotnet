//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCaching;
using System;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Middlewares
{
    /// <summary>
    /// Caching
    /// </summary>
    /// <remarks>
    /// DevelopedBy: AnthonyGiretti
    /// <see cref="https://github.com/AnthonyGiretti/commonfeatures-webapi-aspnetcore/blob/master/WebApiDemo/Middlewares/CachingMiddlewarecs.cs"/>
    /// </remarks>
    public class CachingMiddleware(RequestDelegate next, string[] varyByQueryKeys)
    {
        public async Task Invoke(HttpContext context)
        {
            // Sample of global cache for any request that returns 200
            context.Response.GetTypedHeaders().CacheControl =
                new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
                {
                    MaxAge = TimeSpan.FromSeconds(60),
                    Public = true
                };
            var responseCachingFeature = context.Features.Get<IResponseCachingFeature>();
            if (responseCachingFeature != null)
            {
                responseCachingFeature.VaryByQueryKeys = varyByQueryKeys;
            }
            if (!context.Response.HasStarted)
            {
                await next(context);
            }
        }
    }
}
