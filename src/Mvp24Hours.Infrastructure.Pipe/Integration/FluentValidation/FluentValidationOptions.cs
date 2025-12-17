//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Pipe.Integration.FluentValidation
{
    /// <summary>
    /// Options for configuring FluentValidation operations in the pipeline.
    /// </summary>
    public class FluentValidationOptions
    {
        /// <summary>
        /// Gets or sets whether the validation operation is required even if previous operations failed.
        /// Default is false.
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Gets or sets whether to throw a ValidationException when validation fails.
        /// If false, returns a failed OperationResult instead.
        /// Default is false.
        /// </summary>
        public bool ThrowValidationException { get; set; }

        /// <summary>
        /// Gets or sets whether to re-throw exceptions from validators.
        /// If false, validator exceptions are converted to validation failures.
        /// Default is false.
        /// </summary>
        public bool ThrowOnValidatorException { get; set; }

        /// <summary>
        /// Gets or sets whether to fail validation if the data to validate is not found.
        /// Default is true.
        /// </summary>
        public bool FailOnMissingData { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to lock the pipeline message when validation fails.
        /// Only applies to IPipelineMessage-based operations.
        /// Default is true.
        /// </summary>
        public bool LockPipelineOnFailure { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include info/warning messages in validation results.
        /// If false, only errors are included.
        /// Default is true.
        /// </summary>
        public bool IncludeNonErrorMessages { get; set; } = true;

        /// <summary>
        /// Gets or sets the default ruleset to use for validation.
        /// If null, the default ruleset is used.
        /// </summary>
        public string? RuleSet { get; set; }
    }
}

