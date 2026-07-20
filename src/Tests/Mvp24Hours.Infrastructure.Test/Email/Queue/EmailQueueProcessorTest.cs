//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.Infrastructure.Email.Contract;
using Mvp24Hours.Infrastructure.Email.Models;
using Mvp24Hours.Infrastructure.Email.Queue;
using Mvp24Hours.Infrastructure.Email.Results;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.Email.Queue;

[Trait("Category", "Unit")]
public class EmailQueueProcessorTest
{
    [Fact]
    public void Constructor_WithNullQueue_ShouldThrowArgumentNullException()
    {
        Mock<IEmailService> emailService = EmailTestHelpers.CreateMockEmailService();
        IOptions<EmailQueueProcessorOptions> options = EmailTestHelpers.AsOptions(new EmailQueueProcessorOptions());

        Action act = () => _ = new EmailQueueProcessor(null!, emailService.Object, options);

        act.Should().Throw<ArgumentNullException>().WithParameterName("emailQueue");
    }

    [Fact]
    public void Constructor_WithNullEmailService_ShouldThrowArgumentNullException()
    {
        var queue = new InMemoryEmailQueue();
        IOptions<EmailQueueProcessorOptions> options = EmailTestHelpers.AsOptions(new EmailQueueProcessorOptions());

        Action act = () => _ = new EmailQueueProcessor(queue, null!, options);

        act.Should().Throw<ArgumentNullException>().WithParameterName("emailService");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        var queue = new InMemoryEmailQueue();
        Mock<IEmailService> emailService = EmailTestHelpers.CreateMockEmailService();

        Action act = () => _ = new EmailQueueProcessor(queue, emailService.Object, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void EmailQueueProcessorOptions_Defaults_ShouldUseExpectedValues()
    {
        var options = new EmailQueueProcessorOptions();

        options.PollInterval.Should().Be(TimeSpan.FromSeconds(5));
        options.MaxRetryAttempts.Should().Be(3);
        options.RetryDelay.Should().Be(TimeSpan.FromMinutes(1));
        options.MaxConcurrency.Should().Be(1);
    }

    [Fact]
    public async Task StartAsync_WithSuccessfulSend_ShouldMarkQueueItemAsSent()
    {
        var queue = new InMemoryEmailQueue();
        Mock<IEmailService> emailService = EmailTestHelpers.CreateMockEmailService(
            EmailSendResult.Successful("processor-msg-1"));
        IOptions<EmailQueueProcessorOptions> options = EmailTestHelpers.AsOptions(new EmailQueueProcessorOptions
        {
            PollInterval = TimeSpan.FromMilliseconds(100)
        });
        var processor = new EmailQueueProcessor(queue, emailService.Object, options);

        await processor.StartAsync(CancellationToken.None);
        try
        {
            string id = await queue.EnqueueAsync(EmailTestHelpers.CreateValidMessage());

            await EmailTestHelpers.WaitUntilAsync(async () =>
            {
                EmailQueueItemStatus status = await queue.GetStatusAsync(id);
                return status.Status == EmailQueueStatus.Sent;
            }, TimeSpan.FromSeconds(5));

            EmailQueueItemStatus finalStatus = await queue.GetStatusAsync(id);
            finalStatus.MessageId.Should().Be("processor-msg-1");
            finalStatus.SentAt.Should().NotBeNull();
            emailService.Verify(
                s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }
        finally
        {
            await processor.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task StartAsync_WhenSendFailsAfterMaxRetries_ShouldMarkQueueItemAsFailed()
    {
        var queue = new InMemoryEmailQueue();
        Mock<IEmailService> emailService = EmailTestHelpers.CreateMockEmailService(
            EmailSendResult.Failed("SMTP rejected"));
        IOptions<EmailQueueProcessorOptions> options = EmailTestHelpers.AsOptions(new EmailQueueProcessorOptions
        {
            PollInterval = TimeSpan.FromMilliseconds(100),
            MaxRetryAttempts = 1
        });
        var processor = new EmailQueueProcessor(queue, emailService.Object, options);

        await processor.StartAsync(CancellationToken.None);
        try
        {
            string id = await queue.EnqueueAsync(EmailTestHelpers.CreateValidMessage());

            await EmailTestHelpers.WaitUntilAsync(async () =>
            {
                EmailQueueItemStatus status = await queue.GetStatusAsync(id);
                return status.Status == EmailQueueStatus.Failed;
            }, TimeSpan.FromSeconds(5));

            EmailQueueItemStatus finalStatus = await queue.GetStatusAsync(id);
            finalStatus.LastError.Should().Contain("Max retries reached");
        }
        finally
        {
            await processor.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task StartAsync_WithNonInMemoryQueue_ShouldNotSendEmails()
    {
        var mockQueue = new Mock<IEmailQueue>();
        Mock<IEmailService> emailService = EmailTestHelpers.CreateMockEmailService();
        var logger = new Mock<ILogger<EmailQueueProcessor>>();
        IOptions<EmailQueueProcessorOptions> options = EmailTestHelpers.AsOptions(new EmailQueueProcessorOptions
        {
            PollInterval = TimeSpan.FromMilliseconds(100)
        });
        var processor = new EmailQueueProcessor(
            mockQueue.Object,
            emailService.Object,
            options,
            logger.Object);

        await processor.StartAsync(CancellationToken.None);
        try
        {
            await Task.Delay(300);

            emailService.Verify(
                s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
                Times.Never);
            logger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("InMemoryEmailQueue")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }
        finally
        {
            await processor.StopAsync(CancellationToken.None);
        }
    }
}
