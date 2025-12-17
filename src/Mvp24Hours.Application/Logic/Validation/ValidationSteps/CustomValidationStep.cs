//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Contract.Validation;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Logic.Validation
{
    /// <summary>
    /// Base class for creating custom validation steps with business rules.
    /// </summary>
    /// <typeparam name="T">The type to validate.</typeparam>
    public abstract class CustomValidationStep<T> : IValidationStep<T> where T : class
    {
        /// <inheritdoc/>
        public virtual int Order => 400;

        /// <inheritdoc/>
        public abstract string Name { get; }

        /// <inheritdoc/>
        public virtual bool IsEnabled => true;

        /// <inheritdoc/>
        public abstract ValidationServiceResult Execute(T instance, ValidationStepContext context);

        /// <inheritdoc/>
        public virtual Task<ValidationServiceResult> ExecuteAsync(T instance, ValidationStepContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Execute(instance, context));
        }

        /// <inheritdoc/>
        public virtual bool ShouldExecute(T instance, ValidationStepContext context)
        {
            return true;
        }

        /// <summary>
        /// Helper method to create a validation error.
        /// </summary>
        protected IMessageResult CreateError(string propertyName, string message, ValidationStepContext context)
        {
            var fullPath = context.Options.IncludePropertyPath && !string.IsNullOrEmpty(context.PropertyPath)
                ? $"{context.PropertyPath}.{propertyName}"
                : propertyName;

            return new Core.ValueObjects.Logic.MessageResult(
                fullPath,
                message,
                Core.Enums.MessageType.Error);
        }

        /// <summary>
        /// Helper method to create a validation warning.
        /// </summary>
        protected IMessageResult CreateWarning(string propertyName, string message, ValidationStepContext context)
        {
            var fullPath = context.Options.IncludePropertyPath && !string.IsNullOrEmpty(context.PropertyPath)
                ? $"{context.PropertyPath}.{propertyName}"
                : propertyName;

            return new Core.ValueObjects.Logic.MessageResult(
                fullPath,
                message,
                Core.Enums.MessageType.Warning);
        }
    }

    /// <summary>
    /// Validation step that uses a predicate function for validation.
    /// </summary>
    /// <typeparam name="T">The type to validate.</typeparam>
    public class PredicateValidationStep<T> : IValidationStep<T> where T : class
    {
        private readonly Func<T, ValidationStepContext, ValidationServiceResult> _validateFunc;
        private readonly Func<T, ValidationStepContext, Task<ValidationServiceResult>>? _validateFuncAsync;
        private readonly Func<T, ValidationStepContext, bool>? _shouldExecuteFunc;

        /// <summary>
        /// Creates a new predicate validation step.
        /// </summary>
        /// <param name="name">The name of this validation step.</param>
        /// <param name="validateFunc">The validation function.</param>
        /// <param name="order">The order of execution (default: 500).</param>
        /// <param name="shouldExecuteFunc">Optional function to determine if step should execute.</param>
        public PredicateValidationStep(
            string name,
            Func<T, ValidationStepContext, ValidationServiceResult> validateFunc,
            int order = 500,
            Func<T, ValidationStepContext, bool>? shouldExecuteFunc = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _validateFunc = validateFunc ?? throw new ArgumentNullException(nameof(validateFunc));
            Order = order;
            _shouldExecuteFunc = shouldExecuteFunc;
        }

        /// <summary>
        /// Creates a new predicate validation step with async support.
        /// </summary>
        /// <param name="name">The name of this validation step.</param>
        /// <param name="validateFunc">The synchronous validation function.</param>
        /// <param name="validateFuncAsync">The asynchronous validation function.</param>
        /// <param name="order">The order of execution (default: 500).</param>
        /// <param name="shouldExecuteFunc">Optional function to determine if step should execute.</param>
        public PredicateValidationStep(
            string name,
            Func<T, ValidationStepContext, ValidationServiceResult> validateFunc,
            Func<T, ValidationStepContext, Task<ValidationServiceResult>> validateFuncAsync,
            int order = 500,
            Func<T, ValidationStepContext, bool>? shouldExecuteFunc = null)
            : this(name, validateFunc, order, shouldExecuteFunc)
        {
            _validateFuncAsync = validateFuncAsync;
        }

        /// <inheritdoc/>
        public int Order { get; }

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public bool IsEnabled => true;

        /// <inheritdoc/>
        public ValidationServiceResult Execute(T instance, ValidationStepContext context)
        {
            return _validateFunc(instance, context);
        }

        /// <inheritdoc/>
        public Task<ValidationServiceResult> ExecuteAsync(T instance, ValidationStepContext context, CancellationToken cancellationToken = default)
        {
            if (_validateFuncAsync != null)
            {
                return _validateFuncAsync(instance, context);
            }

            return Task.FromResult(_validateFunc(instance, context));
        }

        /// <inheritdoc/>
        public bool ShouldExecute(T instance, ValidationStepContext context)
        {
            return _shouldExecuteFunc?.Invoke(instance, context) ?? true;
        }
    }

    /// <summary>
    /// Validation step that validates a collection of rules.
    /// </summary>
    /// <typeparam name="T">The type to validate.</typeparam>
    public class RuleBasedValidationStep<T> : IValidationStep<T> where T : class
    {
        private readonly List<ValidationRule<T>> _rules = new();

        /// <summary>
        /// Creates a new rule-based validation step.
        /// </summary>
        /// <param name="name">The name of this validation step.</param>
        /// <param name="order">The order of execution (default: 500).</param>
        public RuleBasedValidationStep(string name, int order = 500)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Order = order;
        }

        /// <inheritdoc/>
        public int Order { get; }

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public bool IsEnabled => true;

        /// <summary>
        /// Adds a validation rule.
        /// </summary>
        /// <param name="propertyName">The property name for error reporting.</param>
        /// <param name="predicate">The validation predicate (returns true if valid).</param>
        /// <param name="errorMessage">The error message if validation fails.</param>
        /// <returns>This instance for chaining.</returns>
        public RuleBasedValidationStep<T> AddRule(string propertyName, Func<T, bool> predicate, string errorMessage)
        {
            _rules.Add(new ValidationRule<T>(propertyName, predicate, errorMessage));
            return this;
        }

        /// <summary>
        /// Adds an async validation rule.
        /// </summary>
        /// <param name="propertyName">The property name for error reporting.</param>
        /// <param name="predicateAsync">The async validation predicate (returns true if valid).</param>
        /// <param name="errorMessage">The error message if validation fails.</param>
        /// <returns>This instance for chaining.</returns>
        public RuleBasedValidationStep<T> AddRuleAsync(string propertyName, Func<T, CancellationToken, Task<bool>> predicateAsync, string errorMessage)
        {
            _rules.Add(new ValidationRule<T>(propertyName, predicateAsync, errorMessage));
            return this;
        }

        /// <inheritdoc/>
        public ValidationServiceResult Execute(T instance, ValidationStepContext context)
        {
            var errors = new List<IMessageResult>();

            foreach (var rule in _rules.Where(r => r.Predicate != null))
            {
                if (!rule.Predicate!(instance))
                {
                    var fullPath = context.Options.IncludePropertyPath && !string.IsNullOrEmpty(context.PropertyPath)
                        ? $"{context.PropertyPath}.{rule.PropertyName}"
                        : rule.PropertyName;

                    errors.Add(new Core.ValueObjects.Logic.MessageResult(
                        fullPath,
                        rule.ErrorMessage,
                        Core.Enums.MessageType.Error));

                    if (context.Options.StopOnFirstError)
                    {
                        return ValidationServiceResult.Failure(errors);
                    }
                }
            }

            return errors.Any()
                ? ValidationServiceResult.Failure(errors)
                : ValidationServiceResult.Success();
        }

        /// <inheritdoc/>
        public async Task<ValidationServiceResult> ExecuteAsync(T instance, ValidationStepContext context, CancellationToken cancellationToken = default)
        {
            var errors = new List<IMessageResult>();

            foreach (var rule in _rules)
            {
                cancellationToken.ThrowIfCancellationRequested();

                bool isValid;

                if (rule.PredicateAsync != null)
                {
                    isValid = await rule.PredicateAsync(instance, cancellationToken);
                }
                else if (rule.Predicate != null)
                {
                    isValid = rule.Predicate(instance);
                }
                else
                {
                    continue;
                }

                if (!isValid)
                {
                    var fullPath = context.Options.IncludePropertyPath && !string.IsNullOrEmpty(context.PropertyPath)
                        ? $"{context.PropertyPath}.{rule.PropertyName}"
                        : rule.PropertyName;

                    errors.Add(new Core.ValueObjects.Logic.MessageResult(
                        fullPath,
                        rule.ErrorMessage,
                        Core.Enums.MessageType.Error));

                    if (context.Options.StopOnFirstError)
                    {
                        return ValidationServiceResult.Failure(errors);
                    }
                }
            }

            return errors.Any()
                ? ValidationServiceResult.Failure(errors)
                : ValidationServiceResult.Success();
        }

        /// <inheritdoc/>
        public bool ShouldExecute(T instance, ValidationStepContext context)
        {
            return _rules.Any();
        }
    }

    /// <summary>
    /// Represents a single validation rule.
    /// </summary>
    internal class ValidationRule<T>
    {
        public string PropertyName { get; }
        public Func<T, bool>? Predicate { get; }
        public Func<T, CancellationToken, Task<bool>>? PredicateAsync { get; }
        public string ErrorMessage { get; }

        public ValidationRule(string propertyName, Func<T, bool> predicate, string errorMessage)
        {
            PropertyName = propertyName;
            Predicate = predicate;
            ErrorMessage = errorMessage;
        }

        public ValidationRule(string propertyName, Func<T, CancellationToken, Task<bool>> predicateAsync, string errorMessage)
        {
            PropertyName = propertyName;
            PredicateAsync = predicateAsync;
            ErrorMessage = errorMessage;
        }
    }
}

