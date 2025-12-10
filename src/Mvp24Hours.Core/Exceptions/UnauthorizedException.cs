//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Mvp24Hours.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when authentication is required but not provided or is invalid.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exception is used when:
    /// <list type="bullet">
    /// <item>No authentication credentials were provided</item>
    /// <item>The provided credentials are invalid or expired</item>
    /// <item>The authentication token is malformed</item>
    /// </list>
    /// </para>
    /// <para>
    /// In a web API context, this exception usually maps to HTTP 401 Unauthorized.
    /// </para>
    /// <para>
    /// <strong>Note:</strong> This is different from <see cref="ForbiddenException"/>
    /// which is used when the user is authenticated but lacks the required permissions.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public async Task&lt;Order&gt; Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    /// {
    ///     var currentUser = _userContext.CurrentUser;
    ///     if (currentUser == null)
    ///     {
    ///         throw new UnauthorizedException("Authentication is required to access this resource.");
    ///     }
    ///     // Continue processing...
    /// }
    /// </code>
    /// </example>
    [Serializable]
    public class UnauthorizedException : Mvp24HoursException
    {
        /// <summary>
        /// Gets the authentication scheme that was expected or used.
        /// </summary>
        public string? AuthenticationScheme { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnauthorizedException"/> class.
        /// </summary>
        public UnauthorizedException()
            : base("Authentication is required.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnauthorizedException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public UnauthorizedException(string message)
            : base(message, "UNAUTHORIZED")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnauthorizedException"/> class with authentication details.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="authenticationScheme">The expected authentication scheme (e.g., "Bearer", "Basic").</param>
        public UnauthorizedException(string message, string authenticationScheme)
            : base(message, "UNAUTHORIZED", new Dictionary<string, object>
            {
                ["AuthenticationScheme"] = authenticationScheme
            })
        {
            AuthenticationScheme = authenticationScheme;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnauthorizedException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public UnauthorizedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnauthorizedException"/> class with serialized data.
        /// </summary>
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.")]
        protected UnauthorizedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Creates an UnauthorizedException for an expired token.
        /// </summary>
        /// <returns>A new UnauthorizedException instance.</returns>
        public static UnauthorizedException TokenExpired()
        {
            return new UnauthorizedException("The authentication token has expired. Please log in again.", "Bearer");
        }

        /// <summary>
        /// Creates an UnauthorizedException for invalid credentials.
        /// </summary>
        /// <returns>A new UnauthorizedException instance.</returns>
        public static UnauthorizedException InvalidCredentials()
        {
            return new UnauthorizedException("The provided credentials are invalid.");
        }
    }
}

