//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Application.Contract.Resilience
{
    /// <summary>
    /// Maps exceptions to <see cref="IBusinessResultWithStatus{T}"/> instances.
    /// Provides configurable mapping of exception types to status codes and error messages.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface enables:
    /// <list type="bullet">
    /// <item>Consistent exception-to-result mapping across the application.</item>
    /// <item>Custom mappings for domain-specific exceptions.</item>
    /// <item>Separation of exception handling logic from business logic.</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class MyService
    /// {
    ///     private readonly IExceptionToResultMapper _mapper;
    ///     
    ///     public IBusinessResultWithStatus&lt;Order&gt; GetOrder(int id)
    ///     {
    ///         try
    ///         {
    ///             var order = _repository.GetById(id);
    ///             return BusinessResultWithStatus.Success(order);
    ///         }
    ///         catch (Exception ex)
    ///         {
    ///             return _mapper.Map&lt;Order&gt;(ex);
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IExceptionToResultMapper
    {
        /// <summary>
        /// Maps an exception to a business result with appropriate status code and messages.
        /// </summary>
        /// <typeparam name="T">The expected result type.</typeparam>
        /// <param name="exception">The exception to map.</param>
        /// <returns>A failure result with the mapped status code and error message.</returns>
        IBusinessResultWithStatus<T> Map<T>(Exception exception);

        /// <summary>
        /// Maps an exception to a business result with a custom message.
        /// </summary>
        /// <typeparam name="T">The expected result type.</typeparam>
        /// <param name="exception">The exception to map.</param>
        /// <param name="customMessage">A custom message to use instead of the exception message.</param>
        /// <returns>A failure result with the custom message.</returns>
        IBusinessResultWithStatus<T> Map<T>(Exception exception, string customMessage);

        /// <summary>
        /// Gets the status code for a specific exception type.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns>The status code for the exception type.</returns>
        ResultStatusCode GetStatusCode(Exception exception);

        /// <summary>
        /// Gets the error code for a specific exception type.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns>The error code string.</returns>
        string GetErrorCode(Exception exception);

        /// <summary>
        /// Checks if the exception should be logged (server errors typically should be logged).
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns>True if the exception should be logged.</returns>
        bool ShouldLog(Exception exception);

        /// <summary>
        /// Checks if detailed exception information should be included in the result.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns>True if details should be included.</returns>
        bool ShouldIncludeDetails(Exception exception);
    }
}

