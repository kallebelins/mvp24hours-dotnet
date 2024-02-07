//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Mvp24Hours.WebAPI.Configuration;
using System;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Middlewares
{
    /// <summary>
    /// 
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly CorrelationIdOptions _options;

        public CorrelationIdMiddleware(RequestDelegate next, IOptions<CorrelationIdOptions> options)
        {
            ArgumentNullException.ThrowIfNull(options);

            _next = next ?? throw new ArgumentNullException(nameof(next));

            _options = options.Value;
        }

        public Task Invoke(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(_options.Header, out StringValues correlationId))
            {
                context.TraceIdentifier = correlationId;
            }

            if (_options.IncludeInResponse)
            {
                // apply the correlation ID to the response header for client side tracking
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers.Append(_options.Header, new[] { context.TraceIdentifier });
                    return Task.CompletedTask;
                });
            }

            return _next(context);
        }
    }
}
