//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Moq;
using Mvp24Hours.Infrastructure.Email.Bulk;
using Mvp24Hours.Infrastructure.Email.Contract;
using Mvp24Hours.Infrastructure.Email.Models;
using Mvp24Hours.Infrastructure.Email.RateLimiting;
using Mvp24Hours.Infrastructure.Email.Results;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.Email.Bulk;

[Trait("Category", "Unit")]
public class EmailBulkSenderTest
{
    [Fact]
    public void Constructor_WithNullEmailService_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new EmailBulkSender(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("emailService");
    }

    [Fact]
    public async Task SendBulkAsync_WithNullMessages_ShouldThrowArgumentNullException()
    {
        var sender = new EmailBulkSender(EmailTestHelpers.CreateMockEmailService().Object);

        Func<Task> act = () => sender.SendBulkAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("messages");
    }

    [Fact]
    public async Task SendBulkAsync_Sequential_AllSuccessful_ShouldReturnSuccessResult()
    {
        Mock<IEmailService> emailService = EmailTestHelpers.CreateMockEmailService(
            EmailSendResult.Successful("bulk-1"));
        var sender = new EmailBulkSender(emailService.Object);
        List<EmailMessage> messages =
        [
            EmailTestHelpers.CreateValidMessage(to: "a@example.com"),
            EmailTestHelpers.CreateValidMessage(to: "b@example.com")
        ];

        EmailBulkSendResult result = await sender.SendBulkAsync(messages);

        result.TotalCount.Should().Be(2);
        result.SuccessCount.Should().Be(2);
        result.FailureCount.Should().Be(0);
        result.SuccessRate.Should().Be(1.0);
        result.Results.Should().OnlyContain(r => r.Success);
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        result.StartTime.Should().BeBefore(result.EndTime);
    }

    [Fact]
    public async Task SendBulkAsync_WithMixedResults_ShouldReturnCorrectCounts()
    {
        var emailService = new Mock<IEmailService>();
        emailService.Setup(s => s.SendAsync(It.Is<EmailMessage>(m => m.To.Contains("ok@example.com")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmailSendResult.Successful("ok-id"));
        emailService.Setup(s => s.SendAsync(It.Is<EmailMessage>(m => m.To.Contains("fail@example.com")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmailSendResult.Failed("Rejected"));
        var sender = new EmailBulkSender(emailService.Object);
        List<EmailMessage> messages =
        [
            EmailTestHelpers.CreateValidMessage(to: "ok@example.com"),
            EmailTestHelpers.CreateValidMessage(to: "fail@example.com")
        ];

        EmailBulkSendResult result = await sender.SendBulkAsync(messages);

        result.TotalCount.Should().Be(2);
        result.SuccessCount.Should().Be(1);
        result.FailureCount.Should().Be(1);
        result.SuccessRate.Should().Be(0.5);
    }

    [Fact]
    public async Task SendBulkAsync_WhenServiceThrows_ShouldReturnFailedResult()
    {
        var emailService = new Mock<IEmailService>();
        emailService.Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Provider down"));
        var sender = new EmailBulkSender(emailService.Object);

        EmailBulkSendResult result = await sender.SendBulkAsync(
            [EmailTestHelpers.CreateValidMessage()]);

        result.FailureCount.Should().Be(1);
        result.Results.Single().Success.Should().BeFalse();
        result.Results.Single().Exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task SendBulkAsync_ShouldInvokeProgressCallback()
    {
        var sender = new EmailBulkSender(EmailTestHelpers.CreateMockEmailService().Object);
        List<EmailMessage> messages =
        [
            EmailTestHelpers.CreateValidMessage(to: "a@example.com"),
            EmailTestHelpers.CreateValidMessage(to: "b@example.com"),
            EmailTestHelpers.CreateValidMessage(to: "c@example.com")
        ];
        var progressUpdates = new List<BulkSendProgress>();

        await sender.SendBulkAsync(
            messages,
            progressCallback: progress => progressUpdates.Add(new BulkSendProgress
            {
                ProcessedCount = progress.ProcessedCount,
                TotalCount = progress.TotalCount,
                SuccessCount = progress.SuccessCount,
                FailureCount = progress.FailureCount
            }));

        progressUpdates.Should().HaveCount(3);
        progressUpdates.Last().ProcessedCount.Should().Be(3);
        progressUpdates.Last().TotalCount.Should().Be(3);
        progressUpdates.Last().ProgressPercentage.Should().Be(100);
    }

    [Fact]
    public async Task SendBulkAsync_WithMaxConcurrencyGreaterThanOne_ShouldSendInParallel()
    {
        int concurrent = 0;
        int maxConcurrent = 0;
        var gate = new SemaphoreSlim(0, 3);
        var emailService = new Mock<IEmailService>();
        emailService.Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                int current = Interlocked.Increment(ref concurrent);
                int observedMax;
                do
                {
                    observedMax = maxConcurrent;
                }
                while (current > observedMax &&
                       Interlocked.CompareExchange(ref maxConcurrent, current, observedMax) != observedMax);

                await gate.WaitAsync(TimeSpan.FromSeconds(2));
                Interlocked.Decrement(ref concurrent);
                return EmailSendResult.Successful("parallel-id");
            });
        var sender = new EmailBulkSender(emailService.Object);
        List<EmailMessage> messages =
        [
            EmailTestHelpers.CreateValidMessage(to: "a@example.com"),
            EmailTestHelpers.CreateValidMessage(to: "b@example.com"),
            EmailTestHelpers.CreateValidMessage(to: "c@example.com")
        ];
        var options = new BulkSendOptions { MaxConcurrency = 3 };

        Task<EmailBulkSendResult> sendTask = sender.SendBulkAsync(messages, options);
        await Task.Delay(100);
        gate.Release(3);
        EmailBulkSendResult result = await sendTask;

        result.SuccessCount.Should().Be(3);
        maxConcurrent.Should().BeGreaterThan(1);
    }

    [Fact]
    public void BulkSendProgress_ProgressPercentage_ShouldComputeCorrectly()
    {
        var progress = new BulkSendProgress
        {
            ProcessedCount = 2,
            TotalCount = 4,
            SuccessCount = 2,
            FailureCount = 0
        };

        progress.ProgressPercentage.Should().Be(50);
    }

    [Fact]
    public void EmailBulkSendResult_SuccessRate_ShouldComputeCorrectly()
    {
        var result = new EmailBulkSendResult
        {
            TotalCount = 4,
            SuccessCount = 3,
            FailureCount = 1
        };

        result.SuccessRate.Should().Be(0.75);
    }

    [Fact]
    public async Task SendBulkAsync_WithRateLimiter_ShouldCompleteAllSends()
    {
        Mock<IEmailService> emailService = EmailTestHelpers.CreateMockEmailService(
            EmailSendResult.Successful("limited-id"));
        var rateLimiter = new EmailRateLimiter(new RateLimitOptions
        {
            MaxRequestsPerWindow = 100,
            WindowSize = TimeSpan.FromSeconds(1),
            Strategy = RateLimitStrategy.FixedWindow
        });
        var sender = new EmailBulkSender(emailService.Object, rateLimiter);
        List<EmailMessage> messages =
        [
            EmailTestHelpers.CreateValidMessage(to: "a@example.com"),
            EmailTestHelpers.CreateValidMessage(to: "b@example.com")
        ];

        EmailBulkSendResult result = await sender.SendBulkAsync(messages);

        result.SuccessCount.Should().Be(2);
        emailService.Verify(
            s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }
}
