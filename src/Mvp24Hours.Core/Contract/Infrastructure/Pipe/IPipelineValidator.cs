//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Core.Contract.Infrastructure.Pipe
{
    /// <summary>
    /// Validates pipeline structure before execution.
    /// </summary>
    public interface IPipelineValidator
    {
        /// <summary>
        /// Validates the pipeline structure and configuration.
        /// </summary>
        /// <param name="operations">The operations to validate.</param>
        /// <returns>Validation result with any errors found.</returns>
        PipelineValidationResult Validate(IEnumerable<object> operations);
    }

    /// <summary>
    /// Result of pipeline validation.
    /// </summary>
    public class PipelineValidationResult
    {
        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        public static PipelineValidationResult Success() => new() { IsValid = true };

        /// <summary>
        /// Creates a failed validation result with errors.
        /// </summary>
        public static PipelineValidationResult Failure(params PipelineValidationError[] errors) =>
            new() { IsValid = false, Errors = new List<PipelineValidationError>(errors) };

        /// <summary>
        /// Indicates whether the pipeline is valid.
        /// </summary>
        public bool IsValid { get; init; }

        /// <summary>
        /// List of validation errors if not valid.
        /// </summary>
        public IReadOnlyList<PipelineValidationError> Errors { get; init; } = Array.Empty<PipelineValidationError>();

        /// <summary>
        /// Throws an exception if validation failed.
        /// </summary>
        public void ThrowIfInvalid()
        {
            if (!IsValid)
            {
                throw new PipelineValidationException(Errors);
            }
        }
    }

    /// <summary>
    /// Represents a single validation error.
    /// </summary>
    public record PipelineValidationError(
        string Code,
        string Message,
        string? OperationName = null,
        int? OperationIndex = null
    );

    /// <summary>
    /// Exception thrown when pipeline validation fails.
    /// </summary>
    public class PipelineValidationException : Exception
    {
        /// <summary>
        /// The validation errors that caused this exception.
        /// </summary>
        public IReadOnlyList<PipelineValidationError> Errors { get; }

        public PipelineValidationException(IReadOnlyList<PipelineValidationError> errors)
            : base($"Pipeline validation failed with {errors.Count} error(s): {string.Join("; ", errors)}")
        {
            Errors = errors;
        }
    }
}

