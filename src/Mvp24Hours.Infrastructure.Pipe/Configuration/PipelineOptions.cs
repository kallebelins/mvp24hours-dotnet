//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using System;

namespace Mvp24Hours.Infrastructure.Pipe.Configuration
{
    /// <summary>
    /// Configuration options for the Pipeline.
    /// </summary>
    [Serializable]
    public sealed class PipelineOptions
    {
        /// <summary>
        /// Gets or sets whether the pipeline should break execution when an operation fails.
        /// Default: false.
        /// </summary>
        public bool IsBreakOnFail { get; set; }

        /// <summary>
        /// Gets or sets whether the pipeline should force rollback of executed operations on failure.
        /// Default: false.
        /// </summary>
        public bool ForceRollbackOnFalure { get; set; }

        /// <summary>
        /// Gets or sets whether exceptions should be propagated after handling.
        /// Default: false.
        /// </summary>
        public bool AllowPropagateException { get; set; }

        /// <summary>
        /// Gets or sets the default timeout for operations.
        /// Null means no timeout. Default: null.
        /// </summary>
        public TimeSpan? DefaultOperationTimeout { get; set; }

        /// <summary>
        /// Gets or sets whether to validate the pipeline before execution.
        /// Default: false.
        /// </summary>
        public bool ValidateBeforeExecute { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of operations allowed in the pipeline.
        /// Default: 1000.
        /// </summary>
        public int MaxOperations { get; set; } = 1000;

        /// <summary>
        /// Gets or sets whether to use middleware pattern for operation execution.
        /// Default: false (for backward compatibility).
        /// </summary>
        public bool UseMiddleware { get; set; }

        /// <summary>
        /// Gets or sets the exception mapper for custom exception handling.
        /// Default: null (uses default behavior).
        /// </summary>
        public IPipelineExceptionMapper? ExceptionMapper { get; set; }

        /// <summary>
        /// Gets or sets the pipeline validator.
        /// Default: null (no validation).
        /// </summary>
        public IPipelineValidator? Validator { get; set; }
    }
}
