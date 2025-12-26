//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Sms.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Sms.Contract
{
    /// <summary>
    /// Handler interface for processing delivery reports from SMS providers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Delivery reports are status updates about SMS/MMS messages sent through the provider.
    /// These reports are typically received via webhooks and provide information about delivery
    /// status, failures, and timestamps.
    /// </para>
    /// <para>
    /// <strong>Webhook Integration:</strong>
    /// SMS providers send delivery reports to configured webhook URLs. The webhook endpoint should
    /// parse the provider-specific format and create a <see cref="DeliveryReport"/>, then call
    /// the handler to process it.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class MyDeliveryReportHandler : IDeliveryReportHandler
    /// {
    ///     public async Task HandleAsync(DeliveryReport report, CancellationToken cancellationToken)
    ///     {
    ///         // Update database with delivery status
    ///         await UpdateMessageStatus(report.MessageId, report.Status);
    ///         
    ///         // Send notification if failed
    ///         if (report.IsFailed)
    ///         {
    ///             await NotifyFailure(report);
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IDeliveryReportHandler
    {
        /// <summary>
        /// Handles a delivery report.
        /// </summary>
        /// <param name="report">The delivery report to process.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// <para>
        /// This method is called when a delivery report is received from the SMS provider.
        /// Implementations should process the report (e.g., update database, send notifications,
        /// log analytics).
        /// </para>
        /// <para>
        /// <strong>Error Handling:</strong>
        /// If an exception is thrown during processing, the webhook framework should handle it
        /// appropriately (e.g., log error, retry, or send to dead letter queue).
        /// </para>
        /// </remarks>
        Task HandleAsync(DeliveryReport report, CancellationToken cancellationToken = default);
    }
}

