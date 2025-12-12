//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Core.Contract.Infrastructure
{
    /// <summary>
    /// Provides access to the current user's identity information.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface provides a simple way to get the current user's identifier
    /// for use in infrastructure components like interceptors and repositories.
    /// </para>
    /// <para>
    /// Implementations can obtain the user from various sources:
    /// - HTTP context (for web applications)
    /// - Thread principal
    /// - Ambient context
    /// - CQRS request context
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple implementation using ClaimsPrincipal:
    /// public class HttpContextCurrentUserProvider : ICurrentUserProvider
    /// {
    ///     private readonly IHttpContextAccessor _httpContextAccessor;
    ///     
    ///     public HttpContextCurrentUserProvider(IHttpContextAccessor httpContextAccessor)
    ///     {
    ///         _httpContextAccessor = httpContextAccessor;
    ///     }
    ///     
    ///     public string UserId => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    ///     public string UserName => _httpContextAccessor.HttpContext?.User?.Identity?.Name;
    /// }
    /// </code>
    /// </example>
    public interface ICurrentUserProvider
    {
        /// <summary>
        /// Gets the unique identifier of the current user.
        /// </summary>
        /// <value>
        /// The user's unique identifier, or null if no user is authenticated.
        /// This could be a user ID, email, username, or other unique identifier.
        /// </value>
        string UserId { get; }

        /// <summary>
        /// Gets the display name of the current user.
        /// </summary>
        /// <value>
        /// The user's display name, or null if not available.
        /// </value>
        string UserName { get; }
    }

    /// <summary>
    /// A static/singleton implementation of <see cref="ICurrentUserProvider"/> for simple scenarios.
    /// </summary>
    /// <remarks>
    /// This implementation uses AsyncLocal to store the current user, making it
    /// thread-safe and usable in async contexts. Set the user at the beginning
    /// of a request/operation and it will be available throughout.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Set at the beginning of request:
    /// AsyncLocalCurrentUserProvider.SetCurrentUser("user123", "John Doe");
    /// 
    /// // Get anywhere in the call stack:
    /// var userId = AsyncLocalCurrentUserProvider.Instance.UserId; // "user123"
    /// 
    /// // Clear at the end:
    /// AsyncLocalCurrentUserProvider.ClearCurrentUser();
    /// </code>
    /// </example>
    public class AsyncLocalCurrentUserProvider : ICurrentUserProvider
    {
        private static readonly System.Threading.AsyncLocal<(string UserId, string UserName)> _current
            = new System.Threading.AsyncLocal<(string, string)>();

        /// <summary>
        /// Gets the singleton instance of the provider.
        /// </summary>
        public static AsyncLocalCurrentUserProvider Instance { get; } = new AsyncLocalCurrentUserProvider();

        /// <inheritdoc />
        public string UserId => _current.Value.UserId;

        /// <inheritdoc />
        public string UserName => _current.Value.UserName;

        /// <summary>
        /// Sets the current user for the current async context.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="userName">The user name.</param>
        public static void SetCurrentUser(string userId, string userName = null)
        {
            _current.Value = (userId, userName);
        }

        /// <summary>
        /// Clears the current user from the async context.
        /// </summary>
        public static void ClearCurrentUser()
        {
            _current.Value = (null, null);
        }
    }

    /// <summary>
    /// A default implementation that returns "System" as the user.
    /// </summary>
    /// <remarks>
    /// Use this implementation when no user context is available or needed,
    /// such as in background jobs or automated processes.
    /// </remarks>
    public class SystemUserProvider : ICurrentUserProvider
    {
        private readonly string _userId;
        private readonly string _userName;

        /// <summary>
        /// Gets the default instance using "System" as the user.
        /// </summary>
        public static SystemUserProvider Default { get; } = new SystemUserProvider("System", "System");

        /// <summary>
        /// Initializes a new instance with custom system user values.
        /// </summary>
        /// <param name="userId">The system user ID.</param>
        /// <param name="userName">The system user name.</param>
        public SystemUserProvider(string userId = "System", string userName = "System")
        {
            _userId = userId;
            _userName = userName;
        }

        /// <inheritdoc />
        public string UserId => _userId;

        /// <inheritdoc />
        public string UserName => _userName;
    }
}

