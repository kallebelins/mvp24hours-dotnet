//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Application.Contract.Resilience
{
    /// <summary>
    /// Configuration options for exception to result mapping.
    /// </summary>
    public class ExceptionMappingOptions
    {
        /// <summary>
        /// Gets or sets whether to include exception details in the result.
        /// Should be false in production for security.
        /// Default: false.
        /// </summary>
        public bool IncludeExceptionDetails { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to include stack traces in development mode.
        /// Default: false.
        /// </summary>
        public bool IncludeStackTrace { get; set; } = false;

        /// <summary>
        /// Gets or sets the default error message for unmapped exceptions.
        /// Default: "An unexpected error occurred. Please try again later."
        /// </summary>
        public string DefaultErrorMessage { get; set; } = "An unexpected error occurred. Please try again later.";

        /// <summary>
        /// Gets or sets whether server errors (5xx) should be logged.
        /// Default: true.
        /// </summary>
        public bool LogServerErrors { get; set; } = true;

        /// <summary>
        /// Gets or sets whether client errors (4xx) should be logged.
        /// Default: false.
        /// </summary>
        public bool LogClientErrors { get; set; } = false;

        /// <summary>
        /// Gets or sets custom exception mappings.
        /// </summary>
        public Dictionary<Type, ExceptionMapping> CustomMappings { get; set; } = new();

        /// <summary>
        /// Adds a custom mapping for an exception type.
        /// </summary>
        /// <typeparam name="TException">The exception type.</typeparam>
        /// <param name="statusCode">The status code to use.</param>
        /// <param name="errorCode">The error code to use.</param>
        /// <param name="messageFactory">Optional factory to create custom messages.</param>
        public void AddMapping<TException>(
            ResultStatusCode statusCode,
            string errorCode,
            Func<Exception, string>? messageFactory = null)
            where TException : Exception
        {
            CustomMappings[typeof(TException)] = new ExceptionMapping
            {
                StatusCode = statusCode,
                ErrorCode = errorCode,
                MessageFactory = messageFactory
            };
        }

        /// <summary>
        /// Adds a custom mapping for an exception type.
        /// </summary>
        public void AddMapping(
            Type exceptionType,
            ResultStatusCode statusCode,
            string errorCode,
            Func<Exception, string>? messageFactory = null)
        {
            CustomMappings[exceptionType] = new ExceptionMapping
            {
                StatusCode = statusCode,
                ErrorCode = errorCode,
                MessageFactory = messageFactory
            };
        }
    }

    /// <summary>
    /// Represents a mapping from an exception type to result properties.
    /// </summary>
    public class ExceptionMapping
    {
        /// <summary>
        /// Gets or sets the status code for this exception type.
        /// </summary>
        public ResultStatusCode StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the error code for this exception type.
        /// </summary>
        public string ErrorCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a factory function to create custom error messages.
        /// If null, the exception message is used.
        /// </summary>
        public Func<Exception, string>? MessageFactory { get; set; }

        /// <summary>
        /// Gets or sets whether this exception type should be logged.
        /// </summary>
        public bool? ShouldLog { get; set; }

        /// <summary>
        /// Gets or sets whether details should be included for this exception type.
        /// </summary>
        public bool? IncludeDetails { get; set; }
    }
}

