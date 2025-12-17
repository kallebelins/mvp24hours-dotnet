//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Core.Contract.Infrastructure.Pipe
{
    /// <summary>
    /// Maps exceptions to structured message results for consistent error handling.
    /// </summary>
    public interface IPipelineExceptionMapper
    {
        /// <summary>
        /// Maps an exception to one or more message results.
        /// </summary>
        /// <param name="exception">The exception to map.</param>
        /// <returns>A collection of message results representing the error.</returns>
        IEnumerable<IMessageResult> Map(Exception exception);

        /// <summary>
        /// Determines if the exception should cause the pipeline to fail.
        /// </summary>
        /// <param name="exception">The exception to evaluate.</param>
        /// <returns>True if the pipeline should be marked as faulty.</returns>
        bool ShouldFail(Exception exception);

        /// <summary>
        /// Determines if the exception should be rethrown after handling.
        /// </summary>
        /// <param name="exception">The exception to evaluate.</param>
        /// <returns>True if the exception should be propagated.</returns>
        bool ShouldPropagate(Exception exception);
    }

    /// <summary>
    /// Exception mapping configuration entry.
    /// </summary>
    /// <typeparam name="TException">Type of exception to handle.</typeparam>
    public interface IExceptionMappingRule<TException> where TException : Exception
    {
        /// <summary>
        /// Gets the exception type this rule handles.
        /// </summary>
        Type ExceptionType => typeof(TException);

        /// <summary>
        /// Maps the exception to message results.
        /// </summary>
        /// <param name="exception">The exception to map.</param>
        /// <returns>Collection of message results.</returns>
        IEnumerable<IMessageResult> Map(TException exception);

        /// <summary>
        /// Determines if this exception should mark pipeline as faulty.
        /// </summary>
        bool ShouldFail { get; }

        /// <summary>
        /// Determines if this exception should be propagated.
        /// </summary>
        bool ShouldPropagate { get; }
    }
}

