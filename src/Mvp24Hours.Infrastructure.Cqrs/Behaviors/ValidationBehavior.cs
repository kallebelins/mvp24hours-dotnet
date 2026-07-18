//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.ValueObjects.Logic;

namespace Mvp24Hours.Infrastructure.Cqrs.Behaviors;

/// <summary>
/// Pipeline behavior that validates requests before they reach the handler.
/// Integrates with FluentValidation validators registered in the DI container.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <remarks>
/// <para>
/// This behavior automatically discovers and runs all <see cref="IValidator{T}"/> 
/// implementations registered for the request type.
/// </para>
/// <para>
/// If any validation failures occur, a <see cref="Mvp24Hours.Core.Exceptions.ValidationException"/>
/// is thrown with detailed error information.
/// </para>
/// <para>
/// <strong>Registration:</strong> Validators must be registered in the DI container
/// for this behavior to find them. Use FluentValidation's assembly scanning:
/// <code>
/// services.AddValidatorsFromAssembly(typeof(MyValidator).Assembly);
/// </code>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Define a validator
/// public class CreateOrderCommandValidator : AbstractValidator&lt;CreateOrderCommand&gt;
/// {
///     public CreateOrderCommandValidator()
///     {
///         RuleFor(x => x.CustomerName)
///             .NotEmpty()
///             .MaximumLength(100);
///             
///         RuleFor(x => x.Items)
///             .NotEmpty()
///             .WithMessage("Order must have at least one item");
///     }
/// }
/// 
/// // Register in DI
/// services.AddTransient(typeof(IPipelineBehavior&lt;,&gt;), typeof(ValidationBehavior&lt;,&gt;));
/// services.AddValidatorsFromAssemblyContaining&lt;CreateOrderCommandValidator&gt;();
/// </code>
/// </example>
/// <remarks>
/// Creates a new instance of the ValidationBehavior.
/// </remarks>
/// <param name="validators">The collection of validators for the request type.</param>
/// <param name="logger">Optional logger for recording validation failures.</param>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    ILogger<ValidationBehavior<TRequest, TResponse>>? logger = null) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators = validators ?? [];
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>>? _logger = logger;

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        string requestName = typeof(TRequest).Name;

        // Create validation context
        var context = new ValidationContext<TRequest>(request);

        // Run all validators and collect failures
        ValidationResult[] validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure != null)
            .ToList();

        if (failures.Count != 0)
        {
            // Convert FluentValidation errors to IMessageResult
            var validationErrors = failures
                .Select(failure => (IMessageResult)new MessageResult(
                    failure.PropertyName ?? failure.ErrorCode,
                    failure.ErrorMessage,
                    MessageType.Error))
                .ToList();

            _logger?.LogWarning(
                "[Validation] {RequestName} failed validation with {ErrorCount} error(s): {Errors}",
                requestName,
                failures.Count,
                string.Join("; ", failures.Select(f => $"{f.PropertyName}: {f.ErrorMessage}")));

            throw new Mvp24Hours.Core.Exceptions.ValidationException(
                $"Validation failed for {requestName}",
                "VALIDATION_ERROR",
                validationErrors);
        }

        return await next();
    }
}

