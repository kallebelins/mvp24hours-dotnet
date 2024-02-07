//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.DTOs;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using Mvp24Hours.WebAPI.Configuration;
using System;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Middlewares
{
    public class ExceptionMiddleware(RequestDelegate next, IOptions<ExceptionOptions> options)
    {
        private readonly ExceptionOptions options = options?.Value ?? throw new ArgumentNullException(nameof(options), "[ExceptionMiddleware] Options is required. Check: services.AddMvp24HoursWebExceptions().");

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                if (!httpContext.Response.HasStarted)
                {
                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "my-exception-middleware-start", $"http-request-path:{httpContext.Request.Path}");
                    await next(httpContext);
                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "my-exception-middleware-end", $"http-request-path:{httpContext.Request.Path}");
                }
            }
            catch (Exception ex)
            {
                if (!httpContext.Response.HasStarted)
                {
                    TelemetryHelper.Execute(TelemetryLevels.Error, "my-exception-middleware-failure", ex);
                    await HandleExceptionAsync(httpContext, ex);
                }
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = options.StatusCodeHandle(exception);

            string message;

            if (options.TraceMiddleware)
            {
                message = $"Message: {(exception.InnerException ?? exception).Message} / Trace: {exception.StackTrace}";
            }
            else
            {
                message = $"Message: {(exception.InnerException ?? exception).Message}";
            }

            var boResult = message
                .ToMessageResult("internalservererror", MessageType.Error)
                .ToBusiness<VoidResult>();

            var messageResult = JsonHelper.Serialize(boResult);
            return context.Response.WriteAsync(messageResult);
        }
    }
}
