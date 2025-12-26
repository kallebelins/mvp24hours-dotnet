//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.BackgroundJobs.Contract
{
    /// <summary>
    /// Defines a background job that can be executed asynchronously.
    /// </summary>
    /// <typeparam name="TArgs">The type of arguments passed to the job.</typeparam>
    /// <remarks>
    /// <para>
    /// This interface represents a background job that can be scheduled and executed
    /// asynchronously. Jobs are defined by implementing this interface with a specific
    /// argument type. The job scheduler will instantiate and execute the job when
    /// scheduled.
    /// </para>
    /// <para>
    /// <strong>Job Arguments:</strong>
    /// Jobs can accept strongly-typed arguments. The arguments are serialized when the
    /// job is scheduled and deserialized when the job is executed. Arguments should be
    /// serializable (typically using JSON).
    /// </para>
    /// <para>
    /// <strong>Job Execution:</strong>
    /// The <see cref="ExecuteAsync"/> method is called when the job is executed. It receives:
    /// - The job arguments (deserialized)
    /// - The job context (ID, attempt number, cancellation token, metadata)
    /// - A cancellation token for graceful cancellation
    /// </para>
    /// <para>
    /// <strong>Error Handling:</strong>
    /// If the job throws an exception, the job scheduler will handle it according to
    /// the retry policy configured for the job. Jobs should throw exceptions to indicate
    /// failure, which will trigger retries if configured.
    /// </para>
    /// <para>
    /// <strong>Cancellation:</strong>
    /// Jobs should periodically check the cancellation token and stop processing when
    /// cancellation is requested. This allows for graceful shutdown and cancellation
    /// of long-running jobs.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Define job arguments
    /// public class SendEmailArgs
    /// {
    ///     public string To { get; set; }
    ///     public string Subject { get; set; }
    ///     public string Body { get; set; }
    /// }
    /// 
    /// // Implement the job
    /// public class SendEmailJob : IBackgroundJob&lt;SendEmailArgs&gt;
    /// {
    ///     private readonly IEmailService _emailService;
    ///     
    ///     public SendEmailJob(IEmailService emailService)
    ///     {
    ///         _emailService = emailService;
    ///     }
    ///     
    ///     public async Task ExecuteAsync(
    ///         SendEmailArgs args,
    ///         IJobContext context,
    ///         CancellationToken cancellationToken)
    ///     {
    ///         // Check cancellation
    ///         cancellationToken.ThrowIfCancellationRequested();
    ///         
    ///         // Log job execution
    ///         Console.WriteLine($"Sending email to {args.To} (Job ID: {context.JobId})");
    ///         
    ///         // Execute the job
    ///         var message = new EmailMessage
    ///         {
    ///             To = new[] { args.To },
    ///             Subject = args.Subject,
    ///             PlainTextBody = args.Body
    ///         };
    ///         
    ///         await _emailService.SendAsync(message, cancellationToken);
    ///     }
    /// }
    /// 
    /// // Schedule the job
    /// await jobScheduler.EnqueueAsync&lt;SendEmailJob, SendEmailArgs&gt;(
    ///     new SendEmailArgs
    ///     {
    ///         To = "user@example.com",
    ///         Subject = "Hello",
    ///         Body = "This is a test email."
    ///     });
    /// </code>
    /// </example>
    public interface IBackgroundJob<in TArgs>
    {
        /// <summary>
        /// Executes the background job asynchronously.
        /// </summary>
        /// <param name="args">The job arguments (deserialized from storage).</param>
        /// <param name="context">The job execution context (ID, attempt, cancellation token, metadata).</param>
        /// <param name="cancellationToken">Cancellation token for graceful cancellation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// <para>
        /// This method is called by the job scheduler when the job is executed. The job
        /// should perform its work and return when complete. If the job throws an exception,
        /// it will be handled according to the retry policy configured for the job.
        /// </para>
        /// <para>
        /// <strong>Best Practices:</strong>
        /// - Check cancellation token periodically for long-running jobs
        /// - Use structured logging with job ID for traceability
        /// - Throw exceptions to indicate failure (don't catch and swallow)
        /// - Use dependency injection for services (constructor injection)
        /// - Keep job execution idempotent when possible
        /// </para>
        /// </remarks>
        /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
        Task ExecuteAsync(
            TArgs args,
            IJobContext context,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// Defines a background job that doesn't require arguments.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a convenience interface for jobs that don't need any arguments.
    /// It's equivalent to <see cref="IBackgroundJob{TArgs}"/> with <c>object</c> or
    /// a custom empty argument type.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class CleanupJob : IBackgroundJob
    /// {
    ///     public async Task ExecuteAsync(
    ///         IJobContext context,
    ///         CancellationToken cancellationToken)
    ///     {
    ///         // Perform cleanup work
    ///         await CleanupOldDataAsync(cancellationToken);
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IBackgroundJob
    {
        /// <summary>
        /// Executes the background job asynchronously.
        /// </summary>
        /// <param name="context">The job execution context (ID, attempt, cancellation token, metadata).</param>
        /// <param name="cancellationToken">Cancellation token for graceful cancellation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ExecuteAsync(
            IJobContext context,
            CancellationToken cancellationToken);
    }
}

