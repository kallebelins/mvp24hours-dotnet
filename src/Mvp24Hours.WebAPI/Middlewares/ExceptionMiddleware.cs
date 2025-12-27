//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.DTOs;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using Mvp24Hours.WebAPI.Configuration;
using System;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Middlewares
{
    public class ExceptionMiddleware(RequestDelegate next, IOptions<ExceptionOptions> options, ILogger<ExceptionMiddleware> logger)
    {
        private readonly ExceptionOptions options = options?.Value ?? throw new ArgumentNullException(nameof(options), "[ExceptionMiddleware] Options is required. Check: services.AddMvp24HoursWebExceptions().");
        private readonly ILogger<ExceptionMiddleware> _logger = logger;

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                if (!httpContext.Response.HasStarted)
                {
                    _logger.LogDebug("Exception middleware processing request. Path: {Path}", httpContext.Request.Path);
                    await next(httpContext);
                    _logger.LogDebug("Exception middleware completed request. Path: {Path}", httpContext.Request.Path);
                }
            }
            catch (Exception ex)
            {
                if (!httpContext.Response.HasStarted)
                {
                    _logger.LogError(ex, "Exception occurred in middleware. Path: {Path}", httpContext.Request.Path);
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
