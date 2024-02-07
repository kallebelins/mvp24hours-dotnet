//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Http;

namespace Mvp24Hours.Infrastructure.Extensions
{
    /// <summary>
    /// Contains functions to register and obtain context of the application that is running
    /// </summary>
    public static class HttpContextExtensions
    {
        /// <summary>
        /// Gets IP of the user who originated the current request
        /// </summary>
        public static string GetUserIP(this IHttpContextAccessor accessor)
        {
            return accessor?.HttpContext?.GetUserIP();
        }

        /// <summary>
        /// Gets IP of the user who originated the current request
        /// </summary>
        public static string GetUserIP(this HttpContext context)
        {
            if (context != null)
            {
                string ip = context.Connection?.RemoteIpAddress?.ToString() ?? context.Connection?.LocalIpAddress?.ToString() ?? "127.0.0.1";

                if (ip.Contains(':'))
                {
                    ip = ip.Split(':')[0].Trim();
                }

                if (ip.Contains(','))
                {
                    ip = ip.Split(',')[0].Trim();
                }

                return ip;
            }
            return "0.0.0.0";
        }

        /// <summary>
        /// Get web address dynamically from current service
        /// </summary>
        public static string GetBaseUrl(this IHttpContextAccessor accessor)
        {
            return accessor?.HttpContext?.GetBaseUrl();
        }

        /// <summary>
        /// Get web address dynamically from current service
        /// </summary>
        public static string GetBaseUrl(this HttpContext context)
        {
            if (context != null)
            {
                return $"{context.Request.Scheme}://{context.Request.Host.Value}{context.Request.PathBase.Value}";
            }
            return null;
        }
    }
}
