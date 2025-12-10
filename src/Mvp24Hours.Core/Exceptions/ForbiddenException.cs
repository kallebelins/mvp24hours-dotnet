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
    /// Exception thrown when an authenticated user lacks the required permissions for an operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exception is used when:
    /// <list type="bullet">
    /// <item>The user is authenticated but lacks required roles or permissions</item>
    /// <item>The user is trying to access a resource they don't own</item>
    /// <item>A policy-based authorization check fails</item>
    /// </list>
    /// </para>
    /// <para>
    /// In a web API context, this exception usually maps to HTTP 403 Forbidden.
    /// </para>
    /// <para>
    /// <strong>Note:</strong> This is different from <see cref="UnauthorizedException"/>
    /// which is used when authentication is missing or invalid.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public async Task&lt;Unit&gt; Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
    /// {
    ///     var order = await _repository.GetByIdAsync(request.OrderId);
    ///     if (order.OwnerId != _currentUser.Id &amp;&amp; !_currentUser.IsAdmin)
    ///     {
    ///         throw new ForbiddenException(
    ///             "You don't have permission to delete this order.",
    ///             "Order",
    ///             "Delete");
    ///     }
    ///     // Continue with deletion...
    /// }
    /// </code>
    /// </example>
    [Serializable]
    public class ForbiddenException : Mvp24HoursException
    {
        /// <summary>
        /// Gets the name of the resource that was denied.
        /// </summary>
        public string? ResourceName { get; init; }

        /// <summary>
        /// Gets the name of the action that was attempted.
        /// </summary>
        public string? ActionName { get; init; }

        /// <summary>
        /// Gets the required permission or role that was missing.
        /// </summary>
        public string? RequiredPermission { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ForbiddenException"/> class.
        /// </summary>
        public ForbiddenException()
            : base("You don't have permission to perform this action.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ForbiddenException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ForbiddenException(string message)
            : base(message, "FORBIDDEN")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ForbiddenException"/> class with resource and action details.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="resourceName">The name of the resource that was denied.</param>
        /// <param name="actionName">The name of the action that was attempted.</param>
        /// <param name="requiredPermission">The required permission or role (optional).</param>
        public ForbiddenException(string message, string resourceName, string actionName, string? requiredPermission = null)
            : base(message, "FORBIDDEN", new Dictionary<string, object>
            {
                ["ResourceName"] = resourceName,
                ["ActionName"] = actionName,
                ["RequiredPermission"] = requiredPermission ?? "N/A"
            })
        {
            ResourceName = resourceName;
            ActionName = actionName;
            RequiredPermission = requiredPermission;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ForbiddenException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ForbiddenException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ForbiddenException"/> class with serialized data.
        /// </summary>
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.")]
        protected ForbiddenException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Creates a ForbiddenException for a missing role.
        /// </summary>
        /// <param name="requiredRole">The required role.</param>
        /// <returns>A new ForbiddenException instance.</returns>
        public static ForbiddenException MissingRole(string requiredRole)
        {
            return new ForbiddenException(
                $"This action requires the '{requiredRole}' role.",
                "Application",
                "Access",
                requiredRole);
        }

        /// <summary>
        /// Creates a ForbiddenException for resource ownership violation.
        /// </summary>
        /// <typeparam name="TEntity">The type of entity.</typeparam>
        /// <param name="entityId">The entity identifier.</param>
        /// <returns>A new ForbiddenException instance.</returns>
        public static ForbiddenException NotOwner<TEntity>(object entityId)
        {
            var entityName = typeof(TEntity).Name;
            return new ForbiddenException(
                $"You don't have permission to access {entityName} with ID '{entityId}'.",
                entityName,
                "Access");
        }
    }
}

