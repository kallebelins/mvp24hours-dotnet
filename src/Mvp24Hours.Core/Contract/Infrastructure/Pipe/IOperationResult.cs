//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using System.Collections.Generic;

namespace Mvp24Hours.Core.Contract.Infrastructure.Pipe
{
    /// <summary>
    /// Represents the result of a typed operation execution.
    /// </summary>
    /// <typeparam name="T">The type of the operation output.</typeparam>
    public interface IOperationResult<out T>
    {
        /// <summary>
        /// Gets a value indicating whether the operation succeeded.
        /// </summary>
        bool IsSuccess { get; }

        /// <summary>
        /// Gets a value indicating whether the operation failed.
        /// </summary>
        bool IsFailure { get; }

        /// <summary>
        /// Gets the output value if the operation succeeded.
        /// </summary>
        T? Value { get; }

        /// <summary>
        /// Gets the list of messages (errors, warnings, info) from the operation.
        /// </summary>
        IReadOnlyList<IMessageResult> Messages { get; }

        /// <summary>
        /// Gets the error message if the operation failed.
        /// </summary>
        string? ErrorMessage { get; }
    }

    /// <summary>
    /// Represents the result of a typed operation execution without output.
    /// </summary>
    public interface IOperationResult : IOperationResult<object>
    {
    }
}

