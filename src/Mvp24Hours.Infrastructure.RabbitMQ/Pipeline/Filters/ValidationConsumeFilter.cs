//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Filters;

/// <summary>
/// Consume filter that provides FluentValidation integration for message validation.
/// Validates messages before processing and optionally sends invalid messages to DLQ.
/// </summary>
/// <remarks>
/// Creates a new validation consume filter.
/// </remarks>
/// <param name="logger">Optional logger instance.</param>
/// <param name="options">Validation options.</param>
public class ValidationConsumeFilter(
    ILogger<ValidationConsumeFilter>? logger = null,
    IOptions<ValidationFilterOptions>? options = null) : IConsumeFilter
{
    private readonly ILogger<ValidationConsumeFilter>? _logger = logger;
    private readonly ValidationFilterOptions _options = options?.Value ?? new ValidationFilterOptions();

    /// <inheritdoc />
    public async Task ConsumeAsync<TMessage>(
        IConsumeFilterContext<TMessage> context,
        ConsumeFilterDelegate<TMessage> next,
        CancellationToken cancellationToken = default) where TMessage : class
    {
        string messageType = typeof(TMessage).Name;
        string messageId = context.MessageId;

        // Try to get a validator for this message type
        IValidator<TMessage>? validator = context.ServiceProvider.GetService<IValidator<TMessage>>();

        if (validator == null)
        {
            // No validator registered - continue without validation
            LogNoValidator(messageType, messageId);
            await next(context, cancellationToken);
            return;
        }

        // Validate the message
        ValidationResult validationResult = await validator.ValidateAsync(context.Message, cancellationToken);

        if (validationResult.IsValid)
        {
            LogValidationPassed(messageType, messageId);
            await next(context, cancellationToken);
            return;
        }

        // Validation failed
        var errors = validationResult.Errors
            .Select(e => new ValidationError(e.PropertyName, e.ErrorMessage, e.ErrorCode))
            .ToList();

        LogValidationFailed(messageType, messageId, errors);

        // Store validation errors in context
        context.Items["ValidationErrors"] = errors;
        context.Items["ValidationFailed"] = true;

        if (_options.ThrowOnValidationFailure)
        {
            throw new MessageValidationException(messageType, errors);
        }

        if (_options.SendInvalidToDeadLetter)
        {
            string reason = FormatValidationErrors(errors);
            context.SendToDeadLetter($"Validation failed: {reason}");
            context.SkipRemainingFilters();
        }
        else if (_options.SkipInvalidMessages)
        {
            context.SkipRemainingFilters();
        }
        else
        {
            // Continue processing even with validation errors (let consumer decide)
            await next(context, cancellationToken);
        }
    }

    private void LogNoValidator(string messageType, string messageId)
    {
        if (_options.LogMissingValidators)
        {
            _logger?.LogDebug(
                "No validator found for message. Type={MessageType}, MessageId={MessageId}",
                messageType, messageId);
        }
    }

    private void LogValidationPassed(string messageType, string messageId)
    {
        _logger?.LogDebug(
            "Message validation passed. Type={MessageType}, MessageId={MessageId}",
            messageType, messageId);
    }

    private void LogValidationFailed(string messageType, string messageId, List<ValidationError> errors)
    {
        string errorSummary = FormatValidationErrors(errors);
        _logger?.LogWarning(
            "Message validation failed. Type={MessageType}, MessageId={MessageId}, Errors={ErrorSummary}",
            messageType, messageId, errorSummary);
    }

    private static string FormatValidationErrors(List<ValidationError> errors)
    {
        return string.Join("; ", errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
    }
}

/// <summary>
/// Publish filter that provides FluentValidation integration for message validation before publishing.
/// </summary>
/// <remarks>
/// Creates a new validation publish filter.
/// </remarks>
/// <param name="logger">Optional logger instance.</param>
/// <param name="options">Validation options.</param>
public class ValidationPublishFilter(
    ILogger<ValidationPublishFilter>? logger = null,
    IOptions<ValidationFilterOptions>? options = null) : IPublishFilter
{
    private readonly ILogger<ValidationPublishFilter>? _logger = logger;
    private readonly ValidationFilterOptions _options = options?.Value ?? new ValidationFilterOptions();

    /// <inheritdoc />
    public async Task PublishAsync<TMessage>(
        IPublishFilterContext<TMessage> context,
        PublishFilterDelegate<TMessage> next,
        CancellationToken cancellationToken = default) where TMessage : class
    {
        string messageType = typeof(TMessage).Name;
        string messageId = context.MessageId;

        // Try to get a validator for this message type
        IValidator<TMessage>? validator = context.ServiceProvider.GetService<IValidator<TMessage>>();

        if (validator == null)
        {
            // No validator registered - continue without validation
            await next(context, cancellationToken);
            return;
        }

        // Validate the message
        ValidationResult validationResult = await validator.ValidateAsync(context.Message, cancellationToken);

        if (validationResult.IsValid)
        {
            await next(context, cancellationToken);
            return;
        }

        // Validation failed
        var errors = validationResult.Errors
            .Select(e => new ValidationError(e.PropertyName, e.ErrorMessage, e.ErrorCode))
            .ToList();

        LogPublishValidationFailed(messageType, messageId, errors);

        // Store validation errors in context
        context.Items["ValidationErrors"] = errors;
        context.Items["ValidationFailed"] = true;

        if (_options.ThrowOnValidationFailure)
        {
            throw new MessageValidationException(messageType, errors);
        }

        if (_options.CancelInvalidPublish)
        {
            string reason = FormatValidationErrors(errors);
            context.CancelPublish($"Validation failed: {reason}");
        }
        else
        {
            // Continue publishing even with validation errors
            await next(context, cancellationToken);
        }
    }

    private void LogPublishValidationFailed(string messageType, string messageId, List<ValidationError> errors)
    {
        string errorSummary = FormatValidationErrors(errors);
        _logger?.LogWarning(
            "Message publish validation failed. Type={MessageType}, MessageId={MessageId}, Errors={ErrorSummary}",
            messageType, messageId, errorSummary);
    }

    private static string FormatValidationErrors(List<ValidationError> errors)
    {
        return string.Join("; ", errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
    }
}

/// <summary>
/// Options for the validation filter.
/// </summary>
public class ValidationFilterOptions
{
    /// <summary>
    /// Gets or sets whether to throw an exception on validation failure. Default is false.
    /// </summary>
    public bool ThrowOnValidationFailure { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to send invalid messages to dead letter queue. Default is true.
    /// </summary>
    public bool SendInvalidToDeadLetter { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to skip invalid messages without processing. Default is false.
    /// </summary>
    public bool SkipInvalidMessages { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to cancel publish of invalid messages. Default is true.
    /// </summary>
    public bool CancelInvalidPublish { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to log when no validator is found. Default is false.
    /// </summary>
    public bool LogMissingValidators { get; set; } = false;
}

/// <summary>
/// Represents a validation error.
/// </summary>
/// <remarks>
/// Creates a new validation error.
/// </remarks>
/// <param name="propertyName">The property that failed validation.</param>
/// <param name="errorMessage">The error message.</param>
/// <param name="errorCode">Optional error code.</param>
public class ValidationError(string propertyName, string errorMessage, string? errorCode = null)
{

    /// <summary>
    /// Gets the property name that failed validation.
    /// </summary>
    public string PropertyName { get; } = propertyName;

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string ErrorMessage { get; } = errorMessage;

    /// <summary>
    /// Gets the error code.
    /// </summary>
    public string? ErrorCode { get; } = errorCode;
}

/// <summary>
/// Exception thrown when message validation fails.
/// </summary>
/// <remarks>
/// Creates a new message validation exception.
/// </remarks>
/// <param name="messageType">The type of message that failed validation.</param>
/// <param name="errors">The validation errors.</param>
public class MessageValidationException(string messageType, IReadOnlyList<ValidationError> errors) : Exception($"Validation failed for message type '{messageType}': {FormatErrors(errors)}")
{

    /// <summary>
    /// Gets the type of message that failed validation.
    /// </summary>
    public string MessageType { get; } = messageType;

    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public IReadOnlyList<ValidationError> ValidationErrors { get; } = errors;

    private static string FormatErrors(IReadOnlyList<ValidationError> errors)
    {
        return string.Join("; ", errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
    }
}

