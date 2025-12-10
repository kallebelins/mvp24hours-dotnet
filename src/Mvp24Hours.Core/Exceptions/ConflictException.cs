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
    /// Exception thrown when there is a conflict with the current state of a resource.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exception is typically used in CQRS command handlers when:
    /// <list type="bullet">
    /// <item>An entity with the same unique identifier already exists (duplicate)</item>
    /// <item>A concurrency conflict occurs (optimistic locking failure)</item>
    /// <item>The operation cannot be completed due to the current state of the entity</item>
    /// </list>
    /// </para>
    /// <para>
    /// In a web API context, this exception usually maps to HTTP 409 Conflict.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public async Task&lt;Order&gt; Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    /// {
    ///     var existing = await _repository.GetByOrderNumberAsync(request.OrderNumber);
    ///     if (existing != null)
    ///     {
    ///         throw new ConflictException(
    ///             $"Order with number {request.OrderNumber} already exists",
    ///             "Order",
    ///             "OrderNumber",
    ///             request.OrderNumber);
    ///     }
    ///     // Create order...
    /// }
    /// </code>
    /// </example>
    [Serializable]
    public class ConflictException : Mvp24HoursException
    {
        /// <summary>
        /// Gets the name of the entity type that has the conflict.
        /// </summary>
        public string? EntityName { get; init; }

        /// <summary>
        /// Gets the name of the property that caused the conflict.
        /// </summary>
        public string? PropertyName { get; init; }

        /// <summary>
        /// Gets the conflicting value.
        /// </summary>
        public object? ConflictingValue { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictException"/> class.
        /// </summary>
        public ConflictException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ConflictException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictException"/> class with conflict details.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="entityName">The name of the entity type.</param>
        /// <param name="propertyName">The name of the property that caused the conflict.</param>
        /// <param name="conflictingValue">The conflicting value.</param>
        public ConflictException(string message, string entityName, string propertyName, object? conflictingValue = null)
            : base(message, "CONFLICT", new Dictionary<string, object>
            {
                ["EntityName"] = entityName,
                ["PropertyName"] = propertyName,
                ["ConflictingValue"] = conflictingValue ?? "N/A"
            })
        {
            EntityName = entityName;
            PropertyName = propertyName;
            ConflictingValue = conflictingValue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ConflictException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictException"/> class with serialized data.
        /// </summary>
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.")]
        protected ConflictException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Creates a ConflictException for a duplicate entity.
        /// </summary>
        /// <typeparam name="TEntity">The type of entity.</typeparam>
        /// <param name="propertyName">The name of the unique property.</param>
        /// <param name="value">The duplicate value.</param>
        /// <returns>A new ConflictException instance.</returns>
        public static ConflictException Duplicate<TEntity>(string propertyName, object value)
        {
            var entityName = typeof(TEntity).Name;
            return new ConflictException(
                $"{entityName} with {propertyName} '{value}' already exists.",
                entityName,
                propertyName,
                value);
        }

        /// <summary>
        /// Creates a ConflictException for a concurrency conflict.
        /// </summary>
        /// <typeparam name="TEntity">The type of entity.</typeparam>
        /// <param name="entityId">The entity identifier.</param>
        /// <returns>A new ConflictException instance.</returns>
        public static ConflictException ConcurrencyConflict<TEntity>(object entityId)
        {
            var entityName = typeof(TEntity).Name;
            return new ConflictException(
                $"{entityName} with ID '{entityId}' was modified by another process. Please refresh and try again.",
                entityName,
                "Version",
                entityId);
        }
    }
}

