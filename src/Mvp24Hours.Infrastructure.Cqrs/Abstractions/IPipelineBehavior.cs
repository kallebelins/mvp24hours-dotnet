//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Abstractions;

/// <summary>
/// Delegate that represents the next step in the pipeline execution.
/// Called to continue processing to the next behavior or the final handler.
/// </summary>
/// <typeparam name="TResponse">The type of response expected.</typeparam>
/// <returns>A task containing the response.</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// Defines a pipeline behavior that wraps the execution of a request handler.
/// Allows intercepting requests before and/or after the handler processes them.
/// Used for cross-cutting concerns like validation, logging, caching, transactions, etc.
/// </summary>
/// <typeparam name="TRequest">The type of request being processed.</typeparam>
/// <typeparam name="TResponse">The type of response from the handler.</typeparam>
/// <remarks>
/// <para>
/// Pipeline behaviors form a chain of responsibility around the request handler.
/// They are executed in the order they are registered in the DI container.
/// </para>
/// <para>
/// <strong>Important:</strong> This interface is different from the existing 
/// <c>IPipeline</c> and <c>IOperation</c> in <c>Mvp24Hours.Infrastructure.Pipe</c>
/// which are used for general-purpose pipelines. <see cref="IPipelineBehavior{TRequest, TResponse}"/>
/// is specifically designed for Mediator request/response interception.
/// </para>
/// <para>
/// <strong>Comparison:</strong>
/// <list type="bullet">
/// <item><c>IPipeline/IOperation</c> - General pipeline for business operations (existing)</item>
/// <item><c>IPipelineBehavior</c> - Mediator-specific interceptor for commands/queries (this interface)</item>
/// </list>
/// </para>
/// <para>
/// <strong>Common Use Cases:</strong>
/// <list type="bullet">
/// <item>Validation - Validate request before processing</item>
/// <item>Logging - Log request/response and timing</item>
/// <item>Caching - Cache query results</item>
/// <item>Transaction - Wrap commands in database transactions</item>
/// <item>Performance monitoring - Track slow requests</item>
/// <item>Exception handling - Standardize error handling</item>
/// <item>Authorization - Check permissions before executing</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Validation behavior example
/// public class ValidationBehavior&lt;TRequest, TResponse&gt; : IPipelineBehavior&lt;TRequest, TResponse&gt;
///     where TRequest : IMediatorRequest&lt;TResponse&gt;
/// {
///     private readonly IEnumerable&lt;IValidator&lt;TRequest&gt;&gt; _validators;
///     
///     public ValidationBehavior(IEnumerable&lt;IValidator&lt;TRequest&gt;&gt; validators)
///     {
///         _validators = validators;
///     }
///     
///     public async Task&lt;TResponse&gt; Handle(
///         TRequest request,
///         RequestHandlerDelegate&lt;TResponse&gt; next,
///         CancellationToken cancellationToken)
///     {
///         // Execute BEFORE the handler
///         var context = new ValidationContext&lt;TRequest&gt;(request);
///         var failures = _validators
///             .Select(v => v.Validate(context))
///             .SelectMany(r => r.Errors)
///             .Where(f => f != null)
///             .ToList();
///             
///         if (failures.Count != 0)
///             throw new ValidationException(failures);
///         
///         // Call next behavior or handler
///         var response = await next();
///         
///         // Execute AFTER the handler (if needed)
///         return response;
///     }
/// }
/// </code>
/// </example>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    /// <summary>
    /// Handles the request in the pipeline, optionally calling the next behavior or handler.
    /// </summary>
    /// <param name="request">The request being processed.</param>
    /// <param name="next">Delegate to the next behavior or the final handler.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The response from the pipeline.</returns>
    /// <remarks>
    /// <para>
    /// You must call <paramref name="next"/>() to continue the pipeline execution.
    /// If you don't call it, the handler will never be executed.
    /// </para>
    /// <para>
    /// You can:
    /// <list type="bullet">
    /// <item>Execute code before calling next() (pre-processing)</item>
    /// <item>Execute code after calling next() (post-processing)</item>
    /// <item>Modify the response before returning</item>
    /// <item>Skip the handler entirely by not calling next()</item>
    /// <item>Catch and handle exceptions from the pipeline</item>
    /// </list>
    /// </para>
    /// </remarks>
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}

