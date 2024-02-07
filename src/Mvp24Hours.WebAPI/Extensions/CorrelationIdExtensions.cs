//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Middlewares;
using System;

namespace Mvp24Hours.WebAPI.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class CorrelationIdExtensions
    {
        public static IApplicationBuilder UseMvp24HoursCorrelationId(this IApplicationBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);
            return app.UseMiddleware<CorrelationIdMiddleware>();
        }

        public static IApplicationBuilder UseMvp24HoursCorrelationId(this IApplicationBuilder app, string header)
        {
            ArgumentNullException.ThrowIfNull(app);
            return app.UseMvp24HoursCorrelationId(new CorrelationIdOptions
            {
                Header = header
            });
        }

        public static IApplicationBuilder UseMvp24HoursCorrelationId(this IApplicationBuilder app, CorrelationIdOptions options)
        {
            ArgumentNullException.ThrowIfNull(app);
            ArgumentNullException.ThrowIfNull(options);
            return app.UseMiddleware<CorrelationIdMiddleware>(Options.Create(options));
        }
    }
}
