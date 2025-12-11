//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Abstractions;

/// <summary>
/// Defines an exception handler for specific request and exception type combinations.
/// Allows fine-grained exception handling in the mediator pipeline.
/// </summary>
/// <typeparam name="TRequest">The type of request that may throw the exception.</typeparam>
/// <typeparam name="TResponse">The type of response expected from the handler.</typeparam>
/// <typeparam name="TException">The type of exception to handle.</typeparam>
/// <remarks>
/// <para>
/// Exception handlers provide a way to recover from exceptions or transform them
/// into domain-specific errors without using try-catch blocks in every handler.
/// </para>
/// <para>
/// <strong>Execution Order:</strong>
/// <list type="number">
/// <item>Type-specific handlers (exact exception type) are checked first</item>
/// <item>Base exception handlers are checked next</item>
/// <item>Global exception handlers are checked last</item>
/// </list>
/// </para>
/// <para>
/// <strong>Use Cases:</strong>
/// <list type="bullet">
/// <item>Converting database exceptions to domain exceptions</item>
/// <item>Returning error results instead of throwing</item>
/// <item>Logging and rethrowing with additional context</item>
/// <item>Graceful degradation on specific failures</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class ValidationExceptionHandler&lt;TRequest, TResponse&gt; 
///     : IExceptionHandler&lt;TRequest, TResponse, ValidationException&gt;
///     where TRequest : IMediatorRequest&lt;TResponse&gt;
///     where TResponse : IBusinessResult
/// {
///     public Task&lt;ExceptionHandlingResult&lt;TResponse&gt;&gt; HandleAsync(
///         TRequest request,
///         ValidationException exception,
///         CancellationToken cancellationToken)
///     {
///         // Return a failure result instead of throwing
///         var result = BusinessResult&lt;TResponse&gt;.Failure(
///             exception.Errors.Select(e => e.ErrorMessage).ToArray()
///         );
///         return Task.FromResult(ExceptionHandlingResult&lt;TResponse&gt;.Handled((TResponse)result));
///     }
/// }
/// </code>
/// </example>
public interface IExceptionHandler<in TRequest, TResponse, in TException>
    where TException : Exception
{
    /// <summary>
    /// Handles an exception thrown during request processing.
    /// </summary>
    /// <param name="request">The request that caused the exception.</param>
    /// <param name="exception">The exception that was thrown.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A result indicating how the exception was handled.</returns>
    /// <remarks>
    /// <para>
    /// The return value determines the pipeline behavior:
    /// <list type="bullet">
    /// <item><see cref="ExceptionHandlingResult{TResponse}.Handled(TResponse)"/> - Exception is handled, use the provided response</item>
    /// <item><see cref="ExceptionHandlingResult{TResponse}.NotHandled"/> - Exception should propagate to the next handler</item>
    /// <item><see cref="ExceptionHandlingResult{TResponse}.Rethrow(Exception)"/> - Replace with a different exception</item>
    /// </list>
    /// </para>
    /// </remarks>
    Task<ExceptionHandlingResult<TResponse>> HandleAsync(
        TRequest request,
        TException exception,
        CancellationToken cancellationToken);
}

/// <summary>
/// Global exception handler that handles exceptions for any request type.
/// </summary>
/// <typeparam name="TException">The type of exception to handle.</typeparam>
/// <remarks>
/// <para>
/// Global handlers are checked after type-specific handlers.
/// They are useful for handling exceptions consistently across all requests.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class GlobalDatabaseExceptionHandler : IExceptionHandlerGlobal&lt;DbUpdateException&gt;
/// {
///     private readonly ILogger _logger;
///     
///     public GlobalDatabaseExceptionHandler(ILogger&lt;GlobalDatabaseExceptionHandler&gt; logger)
///     {
///         _logger = logger;
///     }
///     
///     public Task&lt;ExceptionHandlingResult&lt;object?&gt;&gt; HandleAsync(
///         object request,
///         DbUpdateException exception,
///         CancellationToken cancellationToken)
///     {
///         _logger.LogError(exception, "Database error processing {RequestType}", request.GetType().Name);
///         
///         // Wrap in a domain-specific exception
///         var domainEx = new DomainException("PERSISTENCE_ERROR", "Failed to save changes", exception);
///         return Task.FromResult(ExceptionHandlingResult&lt;object?&gt;.Rethrow(domainEx));
///     }
/// }
/// </code>
/// </example>
public interface IExceptionHandlerGlobal<in TException>
    where TException : Exception
{
    /// <summary>
    /// Handles an exception thrown during any request processing.
    /// </summary>
    /// <param name="request">The request object that caused the exception.</param>
    /// <param name="exception">The exception that was thrown.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A result indicating how the exception was handled.</returns>
    Task<ExceptionHandlingResult<object?>> HandleAsync(
        object request,
        TException exception,
        CancellationToken cancellationToken);
}

/// <summary>
/// Represents the result of exception handling.
/// </summary>
/// <typeparam name="TResponse">The type of response if the exception was handled.</typeparam>
public readonly struct ExceptionHandlingResult<TResponse>
{
    /// <summary>
    /// Gets whether the exception was handled and a response is available.
    /// </summary>
    public bool IsHandled { get; }

    /// <summary>
    /// Gets the response if the exception was handled.
    /// </summary>
    public TResponse? Response { get; }

    /// <summary>
    /// Gets the exception to rethrow, if any.
    /// </summary>
    public Exception? ExceptionToRethrow { get; }

    /// <summary>
    /// Gets whether a different exception should be thrown.
    /// </summary>
    public bool ShouldRethrow => ExceptionToRethrow is not null;

    private ExceptionHandlingResult(bool isHandled, TResponse? response, Exception? exceptionToRethrow)
    {
        IsHandled = isHandled;
        Response = response;
        ExceptionToRethrow = exceptionToRethrow;
    }

    /// <summary>
    /// Creates a result indicating the exception was handled and provides a replacement response.
    /// </summary>
    /// <param name="response">The response to return instead of throwing.</param>
    /// <returns>A handled result with the provided response.</returns>
    public static ExceptionHandlingResult<TResponse> Handled(TResponse response)
        => new(true, response, null);

    /// <summary>
    /// Creates a result indicating the exception was not handled and should propagate.
    /// </summary>
    public static ExceptionHandlingResult<TResponse> NotHandled
        => new(false, default, null);

    /// <summary>
    /// Creates a result indicating a different exception should be thrown.
    /// </summary>
    /// <param name="exception">The exception to throw instead.</param>
    /// <returns>A result that will cause the provided exception to be thrown.</returns>
    public static ExceptionHandlingResult<TResponse> Rethrow(Exception exception)
        => new(false, default, exception);
}

