//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.Infrastructure.Pipe.Validation
{
    /// <summary>
    /// Default pipeline validator that checks for common issues.
    /// </summary>
    public class DefaultPipelineValidator : IPipelineValidator
    {
        private readonly List<Func<IEnumerable<object>, IEnumerable<PipelineValidationError>>> _validationRules = new();
        private int _maxOperations = 1000;
        private bool _requireAtLeastOneOperation = false;
        private readonly HashSet<Type> _requiredOperationTypes = new();

        /// <summary>
        /// Sets the maximum number of operations allowed.
        /// </summary>
        /// <param name="maxOperations">Maximum operations (default: 1000).</param>
        /// <returns>This validator for chaining.</returns>
        public DefaultPipelineValidator WithMaxOperations(int maxOperations)
        {
            _maxOperations = maxOperations;
            return this;
        }

        /// <summary>
        /// Requires at least one operation in the pipeline.
        /// </summary>
        /// <returns>This validator for chaining.</returns>
        public DefaultPipelineValidator RequireAtLeastOneOperation()
        {
            _requireAtLeastOneOperation = true;
            return this;
        }

        /// <summary>
        /// Requires a specific operation type to be present.
        /// </summary>
        /// <typeparam name="T">The required operation type.</typeparam>
        /// <returns>This validator for chaining.</returns>
        public DefaultPipelineValidator RequireOperation<T>() where T : class
        {
            _requiredOperationTypes.Add(typeof(T));
            return this;
        }

        /// <summary>
        /// Adds a custom validation rule.
        /// </summary>
        /// <param name="rule">The validation rule function.</param>
        /// <returns>This validator for chaining.</returns>
        public DefaultPipelineValidator AddRule(Func<IEnumerable<object>, IEnumerable<PipelineValidationError>> rule)
        {
            _validationRules.Add(rule ?? throw new ArgumentNullException(nameof(rule)));
            return this;
        }

        /// <inheritdoc />
        public PipelineValidationResult Validate(IEnumerable<object> operations)
        {
            var errors = new List<PipelineValidationError>();
            var operationList = operations?.ToList() ?? new List<object>();

            // Check for null operations
            var nullIndex = operationList.FindIndex(o => o == null);
            if (nullIndex >= 0)
            {
                errors.Add(new PipelineValidationError(
                    "NULL_OPERATION",
                    "Operation cannot be null",
                    null,
                    nullIndex));
            }

            // Check minimum operations requirement
            if (_requireAtLeastOneOperation && operationList.Count == 0)
            {
                errors.Add(new PipelineValidationError(
                    "NO_OPERATIONS",
                    "Pipeline must contain at least one operation"));
            }

            // Check maximum operations
            if (operationList.Count > _maxOperations)
            {
                errors.Add(new PipelineValidationError(
                    "TOO_MANY_OPERATIONS",
                    $"Pipeline has {operationList.Count} operations but maximum is {_maxOperations}"));
            }

            // Check required operation types
            foreach (var requiredType in _requiredOperationTypes)
            {
                if (!operationList.Any(o => requiredType.IsInstanceOfType(o)))
                {
                    errors.Add(new PipelineValidationError(
                        "MISSING_REQUIRED_OPERATION",
                        $"Pipeline must contain an operation of type {requiredType.Name}"));
                }
            }

            // Check for duplicate operations (same instance)
            var seenOperations = new HashSet<object>(ReferenceEqualityComparer.Instance);
            for (int i = 0; i < operationList.Count; i++)
            {
                var op = operationList[i];
                if (op != null && !seenOperations.Add(op))
                {
                    errors.Add(new PipelineValidationError(
                        "DUPLICATE_OPERATION_INSTANCE",
                        "Same operation instance appears multiple times",
                        op.GetType().Name,
                        i));
                }
            }

            // Run custom validation rules
            foreach (var rule in _validationRules)
            {
                errors.AddRange(rule(operationList));
            }

            return errors.Count > 0
                ? PipelineValidationResult.Failure(errors.ToArray())
                : PipelineValidationResult.Success();
        }
    }

    /// <summary>
    /// Comparer that uses reference equality.
    /// </summary>
    internal class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new();

        private ReferenceEqualityComparer() { }

        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}

