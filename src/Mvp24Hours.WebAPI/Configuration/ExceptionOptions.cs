//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System;
using System.Net;

namespace Mvp24Hours.WebAPI.Configuration
{
    public class ExceptionOptions
    {
        public ExceptionOptions()
        {
            StatusCodeHandle = (Exception exception) =>
            {
                return exception != null ? (int)HttpStatusCode.InternalServerError : throw new ArgumentNullException(nameof(exception), "Invalid status code.");
            };
        }

        public bool TraceMiddleware { get; set; }

        public Func<Exception, int> StatusCodeHandle { get; set; }
    }
}
