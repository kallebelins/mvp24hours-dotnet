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
    /// Exception thrown when a requested resource or entity is not found.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exception is typically used in CQRS query handlers when the requested
    /// entity does not exist in the data store.
    /// </para>
    /// <para>
    /// In a web API context, this exception usually maps to HTTP 404 Not Found.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public async Task&lt;Order&gt; Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    /// {
    ///     var order = await _repository.GetByIdAsync(request.OrderId);
    ///     if (order == null)
    ///     {
    ///         throw new NotFoundException(
    ///             $"Order with ID {request.OrderId} was not found",
    ///             "Order",
    ///             request.OrderId);
    ///     }
    ///     return order;
    /// }
    /// </code>
    /// </example>
    [Serializable]
    public class NotFoundException : Mvp24HoursException
    {
        /// <summary>
        /// Gets the name of the entity type that was not found.
        /// </summary>
        public string? EntityName { get; init; }

        /// <summary>
        /// Gets the identifier that was used in the search.
        /// </summary>
        public object? EntityId { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class.
        /// </summary>
        public NotFoundException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public NotFoundException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class with entity information.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="entityName">The name of the entity type that was not found.</param>
        /// <param name="entityId">The identifier that was used in the search.</param>
        public NotFoundException(string message, string entityName, object? entityId = null)
            : base(message, "NOT_FOUND", new Dictionary<string, object>
            {
                ["EntityName"] = entityName,
                ["EntityId"] = entityId ?? "N/A"
            })
        {
            EntityName = entityName;
            EntityId = entityId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public NotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class with serialized data.
        /// </summary>
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.")]
        protected NotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Creates a NotFoundException for a specific entity type and ID.
        /// </summary>
        /// <typeparam name="TEntity">The type of entity that was not found.</typeparam>
        /// <param name="entityId">The identifier that was used in the search.</param>
        /// <returns>A new NotFoundException instance.</returns>
        public static NotFoundException For<TEntity>(object entityId)
        {
            var entityName = typeof(TEntity).Name;
            return new NotFoundException(
                $"{entityName} with ID '{entityId}' was not found.",
                entityName,
                entityId);
        }
    }
}

