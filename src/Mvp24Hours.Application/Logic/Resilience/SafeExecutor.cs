//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Contract.Resilience;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Logic.Resilience
{
    /// <summary>
    /// Provides structured try/catch execution with automatic exception-to-result conversion.
    /// Enables safe execution of operations with consistent error handling.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SafeExecutor provides a clean pattern for:
    /// <list type="bullet">
    /// <item>Executing operations that may throw exceptions.</item>
    /// <item>Converting exceptions to business results automatically.</item>
    /// <item>Logging errors based on configuration.</item>
    /// <item>Maintaining separation between business logic and error handling.</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple execution
    /// var result = SafeExecutor.Execute(() => _repository.GetById(id), _mapper);
    /// 
    /// // Async execution with logging
    /// var result = await SafeExecutor.ExecuteAsync(
    ///     async () => await _repository.GetByIdAsync(id),
    ///     _mapper,
    ///     _logger
    /// );
    /// 
    /// // Using extension methods
    /// var result = await this.TryExecuteAsync(
    ///     () => _repository.GetByIdAsync(id)
    /// );
    /// </code>
    /// </example>
    public static class SafeExecutor
    {
        #region [ Synchronous Methods ]

        /// <summary>
        /// Executes an operation and returns a result, converting any exception to a failure result.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="mapper">The exception mapper.</param>
        /// <param name="logger">Optional logger for error logging.</param>
        /// <returns>Success result with data or failure result with error.</returns>
        public static IBusinessResultWithStatus<T> Execute<T>(
            Func<T> operation,
            IExceptionToResultMapper mapper,
            ILogger? logger = null)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            try
            {
                var result = operation();
                return BusinessResultWithStatus.Success(result);
            }
            catch (Exception ex)
            {
                LogIfNeeded(ex, mapper, logger);
                return mapper.Map<T>(ex);
            }
        }

        /// <summary>
        /// Executes an operation and returns a result, with custom error message.
        /// </summary>
        public static IBusinessResultWithStatus<T> Execute<T>(
            Func<T> operation,
            IExceptionToResultMapper mapper,
            string errorMessage,
            ILogger? logger = null)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            try
            {
                var result = operation();
                return BusinessResultWithStatus.Success(result);
            }
            catch (Exception ex)
            {
                LogIfNeeded(ex, mapper, logger, errorMessage);
                return mapper.Map<T>(ex, errorMessage);
            }
        }

        /// <summary>
        /// Executes an operation that returns void.
        /// </summary>
        public static IBusinessResultWithStatus<bool> Execute(
            Action operation,
            IExceptionToResultMapper mapper,
            ILogger? logger = null)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            try
            {
                operation();
                return BusinessResultWithStatus.Success(true);
            }
            catch (Exception ex)
            {
                LogIfNeeded(ex, mapper, logger);
                return mapper.Map<bool>(ex);
            }
        }

        /// <summary>
        /// Executes an operation and maps the result.
        /// </summary>
        public static IBusinessResultWithStatus<TResult> Execute<T, TResult>(
            Func<T> operation,
            Func<T, TResult> resultMapper,
            IExceptionToResultMapper exceptionMapper,
            ILogger? logger = null)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (resultMapper == null) throw new ArgumentNullException(nameof(resultMapper));
            if (exceptionMapper == null) throw new ArgumentNullException(nameof(exceptionMapper));

            try
            {
                var data = operation();
                var mappedResult = resultMapper(data);
                return BusinessResultWithStatus.Success(mappedResult);
            }
            catch (Exception ex)
            {
                LogIfNeeded(ex, exceptionMapper, logger);
                return exceptionMapper.Map<TResult>(ex);
            }
        }

        /// <summary>
        /// Executes an operation with automatic null-to-NotFound conversion.
        /// </summary>
        public static IBusinessResultWithStatus<T> ExecuteOrNotFound<T>(
            Func<T?> operation,
            IExceptionToResultMapper mapper,
            string notFoundMessage = "Resource not found",
            ILogger? logger = null) where T : class
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            try
            {
                var result = operation();
                if (result == null)
                {
                    return BusinessResultWithStatus.NotFound<T>(notFoundMessage);
                }
                return BusinessResultWithStatus.Success(result);
            }
            catch (Exception ex)
            {
                LogIfNeeded(ex, mapper, logger);
                return mapper.Map<T>(ex);
            }
        }

        #endregion

        #region [ Asynchronous Methods ]

        /// <summary>
        /// Asynchronously executes an operation and returns a result.
        /// </summary>
        public static async Task<IBusinessResultWithStatus<T>> ExecuteAsync<T>(
            Func<Task<T>> operation,
            IExceptionToResultMapper mapper,
            ILogger? logger = null)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            try
            {
                var result = await operation();
                return BusinessResultWithStatus.Success(result);
            }
            catch (Exception ex)
            {
                LogIfNeeded(ex, mapper, logger);
                return mapper.Map<T>(ex);
            }
        }

        /// <summary>
        /// Asynchronously executes an operation with custom error message.
        /// </summary>
        public static async Task<IBusinessResultWithStatus<T>> ExecuteAsync<T>(
            Func<Task<T>> operation,
            IExceptionToResultMapper mapper,
            string errorMessage,
            ILogger? logger = null)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            try
            {
                var result = await operation();
                return BusinessResultWithStatus.Success(result);
            }
            catch (Exception ex)
            {
                LogIfNeeded(ex, mapper, logger, errorMessage);
                return mapper.Map<T>(ex, errorMessage);
            }
        }

        /// <summary>
        /// Asynchronously executes a void operation.
        /// </summary>
        public static async Task<IBusinessResultWithStatus<bool>> ExecuteAsync(
            Func<Task> operation,
            IExceptionToResultMapper mapper,
            ILogger? logger = null)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            try
            {
                await operation();
                return BusinessResultWithStatus.Success(true);
            }
            catch (Exception ex)
            {
                LogIfNeeded(ex, mapper, logger);
                return mapper.Map<bool>(ex);
            }
        }

        /// <summary>
        /// Asynchronously executes an operation and maps the result.
        /// </summary>
        public static async Task<IBusinessResultWithStatus<TResult>> ExecuteAsync<T, TResult>(
            Func<Task<T>> operation,
            Func<T, TResult> resultMapper,
            IExceptionToResultMapper exceptionMapper,
            ILogger? logger = null)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (resultMapper == null) throw new ArgumentNullException(nameof(resultMapper));
            if (exceptionMapper == null) throw new ArgumentNullException(nameof(exceptionMapper));

            try
            {
                var data = await operation();
                var mappedResult = resultMapper(data);
                return BusinessResultWithStatus.Success(mappedResult);
            }
            catch (Exception ex)
            {
                LogIfNeeded(ex, exceptionMapper, logger);
                return exceptionMapper.Map<TResult>(ex);
            }
        }

        /// <summary>
        /// Asynchronously executes with automatic null-to-NotFound conversion.
        /// </summary>
        public static async Task<IBusinessResultWithStatus<T>> ExecuteOrNotFoundAsync<T>(
            Func<Task<T?>> operation,
            IExceptionToResultMapper mapper,
            string notFoundMessage = "Resource not found",
            ILogger? logger = null) where T : class
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            try
            {
                var result = await operation();
                if (result == null)
                {
                    return BusinessResultWithStatus.NotFound<T>(notFoundMessage);
                }
                return BusinessResultWithStatus.Success(result);
            }
            catch (Exception ex)
            {
                LogIfNeeded(ex, mapper, logger);
                return mapper.Map<T>(ex);
            }
        }

        /// <summary>
        /// Asynchronously executes with async result mapping.
        /// </summary>
        public static async Task<IBusinessResultWithStatus<TResult>> ExecuteAsync<T, TResult>(
            Func<Task<T>> operation,
            Func<T, Task<TResult>> resultMapper,
            IExceptionToResultMapper exceptionMapper,
            ILogger? logger = null)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (resultMapper == null) throw new ArgumentNullException(nameof(resultMapper));
            if (exceptionMapper == null) throw new ArgumentNullException(nameof(exceptionMapper));

            try
            {
                var data = await operation();
                var mappedResult = await resultMapper(data);
                return BusinessResultWithStatus.Success(mappedResult);
            }
            catch (Exception ex)
            {
                LogIfNeeded(ex, exceptionMapper, logger);
                return exceptionMapper.Map<TResult>(ex);
            }
        }

        #endregion

        #region [ Private Methods ]

        private static void LogIfNeeded(
            Exception ex,
            IExceptionToResultMapper mapper,
            ILogger? logger,
            string? customMessage = null)
        {
            if (logger == null || !mapper.ShouldLog(ex)) return;

            var message = customMessage ?? ex.Message;
            var errorCode = mapper.GetErrorCode(ex);
            var statusCode = mapper.GetStatusCode(ex);

            logger.LogError(
                ex,
                "Operation failed. StatusCode={StatusCode}, ErrorCode={ErrorCode}, Message={Message}",
                statusCode,
                errorCode,
                message);
        }

        #endregion
    }
}

