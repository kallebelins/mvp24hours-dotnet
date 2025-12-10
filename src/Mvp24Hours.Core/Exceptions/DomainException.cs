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
    /// Exception thrown when a domain rule or invariant is violated.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exception is used for business logic violations that occur within the domain layer.
    /// It represents a violation of domain rules, invariants, or business constraints.
    /// </para>
    /// <para>
    /// <strong>Examples of domain violations:</strong>
    /// <list type="bullet">
    /// <item>Order total is negative</item>
    /// <item>User tries to cancel an already shipped order</item>
    /// <item>Withdrawal amount exceeds account balance</item>
    /// <item>Invalid state transition (e.g., cannot move from 'Completed' to 'Pending')</item>
    /// </list>
    /// </para>
    /// <para>
    /// In a web API context, this exception usually maps to HTTP 422 Unprocessable Entity
    /// or HTTP 400 Bad Request.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class Order
    /// {
    ///     public void Cancel()
    ///     {
    ///         if (Status == OrderStatus.Shipped)
    ///         {
    ///             throw new DomainException(
    ///                 "Cannot cancel an order that has already been shipped.",
    ///                 "Order",
    ///                 "ORDER_ALREADY_SHIPPED");
    ///         }
    ///         Status = OrderStatus.Cancelled;
    ///     }
    /// }
    /// </code>
    /// </example>
    [Serializable]
    public class DomainException : Mvp24HoursException
    {
        /// <summary>
        /// Gets the name of the entity or aggregate where the violation occurred.
        /// </summary>
        public string? EntityName { get; init; }

        /// <summary>
        /// Gets the name of the violated rule or invariant.
        /// </summary>
        public string? RuleName { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainException"/> class.
        /// </summary>
        public DomainException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public DomainException(string message)
            : base(message, "DOMAIN_ERROR")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainException"/> class with domain details.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="entityName">The name of the entity where the violation occurred.</param>
        /// <param name="ruleName">The name of the violated rule (optional).</param>
        public DomainException(string message, string entityName, string? ruleName = null)
            : base(message, ruleName ?? "DOMAIN_ERROR", new Dictionary<string, object>
            {
                ["EntityName"] = entityName,
                ["RuleName"] = ruleName ?? "N/A"
            })
        {
            EntityName = entityName;
            RuleName = ruleName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public DomainException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainException"/> class with serialized data.
        /// </summary>
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.")]
        protected DomainException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Creates a DomainException for an invalid state transition.
        /// </summary>
        /// <typeparam name="TEntity">The type of entity.</typeparam>
        /// <param name="currentState">The current state.</param>
        /// <param name="targetState">The target state that was attempted.</param>
        /// <returns>A new DomainException instance.</returns>
        public static DomainException InvalidStateTransition<TEntity>(string currentState, string targetState)
        {
            var entityName = typeof(TEntity).Name;
            return new DomainException(
                $"Cannot transition {entityName} from '{currentState}' to '{targetState}'.",
                entityName,
                "INVALID_STATE_TRANSITION");
        }

        /// <summary>
        /// Creates a DomainException for a violated business rule.
        /// </summary>
        /// <typeparam name="TEntity">The type of entity.</typeparam>
        /// <param name="rule">A description of the violated rule.</param>
        /// <returns>A new DomainException instance.</returns>
        public static DomainException RuleViolation<TEntity>(string rule)
        {
            var entityName = typeof(TEntity).Name;
            return new DomainException(
                $"Business rule violation in {entityName}: {rule}",
                entityName,
                "BUSINESS_RULE_VIOLATION");
        }

        /// <summary>
        /// Creates a DomainException for an invariant violation.
        /// </summary>
        /// <typeparam name="TEntity">The type of entity.</typeparam>
        /// <param name="invariant">A description of the violated invariant.</param>
        /// <returns>A new DomainException instance.</returns>
        public static DomainException InvariantViolation<TEntity>(string invariant)
        {
            var entityName = typeof(TEntity).Name;
            return new DomainException(
                $"Invariant violation in {entityName}: {invariant}",
                entityName,
                "INVARIANT_VIOLATION");
        }
    }
}

