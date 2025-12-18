//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Application.Contract.Transaction
{
    /// <summary>
    /// Exception thrown when a transaction operation fails.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exception is thrown when:
    /// <list type="bullet">
    /// <item>Transaction commit fails</item>
    /// <item>Transaction rollback fails</item>
    /// <item>Savepoint operations fail</item>
    /// <item>Invalid transaction state transitions occur</item>
    /// </list>
    /// </para>
    /// </remarks>
    [Serializable]
    public class TransactionException : Exception
    {
        /// <summary>
        /// Gets the transaction ID associated with this exception.
        /// </summary>
        public Guid? TransactionId { get; }

        /// <summary>
        /// Gets the transaction status when the exception occurred.
        /// </summary>
        public TransactionStatus? TransactionStatus { get; }

        /// <summary>
        /// Gets the error code associated with this exception.
        /// </summary>
        public string? ErrorCode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionException"/> class.
        /// </summary>
        public TransactionException()
            : base("A transaction error occurred.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionException"/> class
        /// with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public TransactionException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionException"/> class
        /// with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of this exception.</param>
        public TransactionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionException"/> class
        /// with full details.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="transactionId">The transaction ID.</param>
        /// <param name="status">The transaction status when the error occurred.</param>
        /// <param name="errorCode">The error code.</param>
        /// <param name="innerException">The exception that is the cause of this exception.</param>
        public TransactionException(
            string message,
            Guid? transactionId,
            TransactionStatus? status = null,
            string? errorCode = null,
            Exception? innerException = null)
            : base(message, innerException)
        {
            TransactionId = transactionId;
            TransactionStatus = status;
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Creates a TransactionException for a commit failure.
        /// </summary>
        public static TransactionException CommitFailed(Guid transactionId, Exception innerException)
            => new(
                $"Failed to commit transaction {transactionId}.",
                transactionId,
                Contract.Transaction.TransactionStatus.Error,
                "COMMIT_FAILED",
                innerException);

        /// <summary>
        /// Creates a TransactionException for a rollback failure.
        /// </summary>
        public static TransactionException RollbackFailed(Guid transactionId, Exception innerException)
            => new(
                $"Failed to rollback transaction {transactionId}.",
                transactionId,
                Contract.Transaction.TransactionStatus.Error,
                "ROLLBACK_FAILED",
                innerException);

        /// <summary>
        /// Creates a TransactionException for an invalid state.
        /// </summary>
        public static TransactionException InvalidState(
            Guid transactionId,
            TransactionStatus currentStatus,
            string expectedOperation)
            => new(
                $"Cannot perform '{expectedOperation}' on transaction {transactionId} in status '{currentStatus}'.",
                transactionId,
                currentStatus,
                "INVALID_STATE");
    }
}

