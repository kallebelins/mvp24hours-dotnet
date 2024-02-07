//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Mvp24Hours.Extensions;
using Mvp24Hours.WebAPI.Configuration;
using System.Net;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Middlewares
{
    /// <summary>
    /// See: https://stackoverflow.com/a/45844400
    /// </summary>
    public class CorsMiddleware(RequestDelegate next, IOptions<CorsOptions> options)
    {
        private readonly RequestDelegate _next = next;
        private readonly CorsOptions options = options?.Value ?? throw new System.ArgumentNullException(nameof(options), "[CorsMiddleware] Options is required. Check: services.AddMvp24HoursWebCors().");

        public async Task Invoke(HttpContext context)
        {
            if (options.Credentials.HasValue())
            {
                context.Response.Headers.Append("Access-Control-Allow-Credentials", options.Credentials);
            }

            string originCors, headersCors, methodsCors;

            if (options.AllowAll)
            {
                originCors = "*";
                headersCors = "*";
                methodsCors = "*";
            }
            else
            {
                originCors = options.Origin;
                headersCors = options.Headers;
                methodsCors = options.Methods;
            }

            if (originCors.HasValue())
            {
                context.Response.Headers.Append("Access-Control-Allow-Origin", originCors);
            }
            // Added "Accept-Encoding" to this list
            if (headersCors.HasValue())
            {
                context.Response.Headers.Append("Access-Control-Allow-Headers", headersCors);
            }

            if (methodsCors.HasValue())
            {
                context.Response.Headers.Append("Access-Control-Allow-Methods", methodsCors);
            }

            // New Code Starts here
            if (options.AllowRequestOptions && context.Request.Method == "OPTIONS")
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                await context.Response.WriteAsync(string.Empty);
            }
            // New Code Ends here

            await _next(context);
        }
    }
}
