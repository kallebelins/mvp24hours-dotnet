//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Pipe.Operations;
using Mvp24Hours.Infrastructure.Pipe.Typed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Integration.FluentValidation
{
    /// <summary>
    /// A generic validation operation that integrates with FluentValidation.
    /// Validates input using registered FluentValidation validators.
    /// </summary>
    /// <typeparam name="T">The type to validate.</typeparam>
    /// <remarks>
    /// <para>
    /// This operation can be used in typed pipelines to validate input before
    /// passing it to subsequent operations.
    /// </para>
    /// <para>
    /// Validators must be registered in the DI container using FluentValidation's
    /// assembly scanning or manual registration.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register validators
    /// services.AddValidatorsFromAssemblyContaining&lt;CreateOrderValidator&gt;();
    /// 
    /// // Use in pipeline
    /// pipeline
    ///     .Add(validationOperation)
    ///     .Add(createOrderOperation);
    /// </code>
    /// </example>
    public class FluentValidationOperation<T> : ITypedOperationAsync<T, T>
    {
        private readonly IEnumerable<IValidator<T>> _validators;
        private readonly ILogger<FluentValidationOperation<T>>? _logger;
        private readonly FluentValidationOptions _options;

        /// <summary>
        /// Creates a new instance of the FluentValidationOperation.
        /// </summary>
        /// <param name="validators">Collection of FluentValidation validators for type T.</param>
        /// <param name="logger">Optional logger for recording validation results.</param>
        /// <param name="options">Optional validation options.</param>
        public FluentValidationOperation(
            IEnumerable<IValidator<T>> validators,
            ILogger<FluentValidationOperation<T>>? logger = null,
            FluentValidationOptions? options = null)
        {
            _validators = validators ?? throw new ArgumentNullException(nameof(validators));
            _logger = logger;
            _options = options ?? new FluentValidationOptions();
        }

        /// <inheritdoc/>
        public bool IsRequired => _options.IsRequired;

        /// <inheritdoc/>
        public async Task<IOperationResult<T>> ExecuteAsync(T input, CancellationToken cancellationToken = default)
        {
            if (!_validators.Any())
            {
                _logger?.LogDebug("No validators found for type {TypeName}, skipping validation", typeof(T).Name);
                return OperationResult<T>.Success(input);
            }

            var validationContext = new ValidationContext<T>(input);
            var validationResults = new List<ValidationResult>();

            foreach (var validator in _validators)
            {
                try
                {
                    var result = await validator.ValidateAsync(validationContext, cancellationToken);
                    validationResults.Add(result);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Validator {ValidatorType} threw an exception", validator.GetType().Name);
                    
                    if (_options.ThrowOnValidatorException)
                    {
                        throw;
                    }

                    validationResults.Add(new ValidationResult(new[]
                    {
                        new ValidationFailure("Validator", $"Validator error: {ex.Message}")
                    }));
                }
            }

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count == 0)
            {
                _logger?.LogDebug("Validation succeeded for type {TypeName}", typeof(T).Name);
                return OperationResult<T>.Success(input);
            }

            var messages = ConvertToMessages(failures);

            _logger?.LogWarning(
                "Validation failed for type {TypeName} with {ErrorCount} error(s): {Errors}",
                typeof(T).Name,
                failures.Count,
                string.Join("; ", failures.Select(f => $"{f.PropertyName}: {f.ErrorMessage}")));

            if (_options.ThrowValidationException)
            {
                throw new Core.Exceptions.ValidationException(
                    $"Validation failed for {typeof(T).Name}",
                    "PIPELINE_VALIDATION_ERROR",
                    messages);
            }

            return OperationResult<T>.Create(input, false, messages);
        }

        /// <inheritdoc/>
        public Task RollbackAsync(T input, CancellationToken cancellationToken = default)
        {
            // Validation operations don't need rollback
            return Task.CompletedTask;
        }

        private static List<IMessageResult> ConvertToMessages(IEnumerable<ValidationFailure> failures)
        {
            return failures.Select(f => (IMessageResult)new MessageResult(
                f.PropertyName ?? f.ErrorCode ?? "Validation",
                f.ErrorMessage,
                GetMessageType(f.Severity)))
                .ToList();
        }

        private static MessageType GetMessageType(Severity severity)
        {
            return severity switch
            {
                Severity.Error => MessageType.Error,
                Severity.Warning => MessageType.Warning,
                Severity.Info => MessageType.Info,
                _ => MessageType.Error
            };
        }
    }

    /// <summary>
    /// A non-generic validation operation that validates IPipelineMessage content using FluentValidation.
    /// </summary>
    /// <typeparam name="T">The type of data to validate from the pipeline message.</typeparam>
    public class FluentValidationPipelineOperation<T> : OperationBaseAsync
    {
        private readonly IEnumerable<IValidator<T>> _validators;
        private readonly ILogger<FluentValidationPipelineOperation<T>>? _logger;
        private readonly FluentValidationOptions _options;
        private readonly string _tokenAlias;

        /// <summary>
        /// Creates a new instance of FluentValidationPipelineOperation.
        /// </summary>
        /// <param name="validators">Collection of validators.</param>
        /// <param name="tokenAlias">The token alias to retrieve data from the message. If null, uses typeof(T).Name.</param>
        /// <param name="logger">Optional logger.</param>
        /// <param name="options">Optional validation options.</param>
        public FluentValidationPipelineOperation(
            IEnumerable<IValidator<T>> validators,
            string? tokenAlias = null,
            ILogger<FluentValidationPipelineOperation<T>>? logger = null,
            FluentValidationOptions? options = null)
        {
            _validators = validators ?? throw new ArgumentNullException(nameof(validators));
            _tokenAlias = tokenAlias ?? typeof(T).Name;
            _logger = logger;
            _options = options ?? new FluentValidationOptions();
        }

        /// <inheritdoc/>
        public override async Task ExecuteAsync(IPipelineMessage input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            var data = input.GetContent<T>(_tokenAlias);
            if (data == null)
            {
                _logger?.LogWarning("No data found with token alias {TokenAlias} for validation", _tokenAlias);
                
                if (_options.FailOnMissingData)
                {
                    input.Messages.Add(new MessageResult(
                        _tokenAlias,
                        $"No data found for validation with key '{_tokenAlias}'",
                        MessageType.Error));
                    input.SetLock();
                }
                return;
            }

            if (!_validators.Any())
            {
                _logger?.LogDebug("No validators found for type {TypeName}", typeof(T).Name);
                return;
            }

            var validationContext = new ValidationContext<T>(data);
            var failures = new List<ValidationFailure>();

            foreach (var validator in _validators)
            {
                try
                {
                    var result = await validator.ValidateAsync(validationContext);
                    failures.AddRange(result.Errors);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Validator {ValidatorType} threw an exception", validator.GetType().Name);
                    
                    if (_options.ThrowOnValidatorException)
                    {
                        throw;
                    }

                    failures.Add(new ValidationFailure("Validator", $"Validator error: {ex.Message}"));
                }
            }

            if (failures.Count > 0)
            {
                foreach (var failure in failures)
                {
                    var messageType = GetMessageType(failure.Severity);
                    input.Messages.Add(new MessageResult(
                        failure.PropertyName ?? failure.ErrorCode ?? "Validation",
                        failure.ErrorMessage,
                        messageType));
                }

                _logger?.LogWarning(
                    "Validation failed for {TypeName} with {ErrorCount} error(s)",
                    typeof(T).Name,
                    failures.Count);

                if (_options.LockPipelineOnFailure)
                {
                    input.SetLock();
                }
            }
        }

        private static MessageType GetMessageType(Severity severity)
        {
            return severity switch
            {
                Severity.Error => MessageType.Error,
                Severity.Warning => MessageType.Warning,
                Severity.Info => MessageType.Info,
                _ => MessageType.Error
            };
        }
    }
}

