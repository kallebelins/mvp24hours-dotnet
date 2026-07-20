//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Email.Models;
using Mvp24Hours.Infrastructure.Email.Options;
using Mvp24Hours.Infrastructure.Email.Queue;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.Email.Queue;

[Trait("Category", "Unit")]
public class InMemoryEmailQueueTest
{
    [Fact]
    public async Task EnqueueAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        var queue = new InMemoryEmailQueue();

        Func<Task> act = () => queue.EnqueueAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("message");
    }

    [Fact]
    public async Task EnqueueAsync_ShouldReturnIdAndQueuedStatus()
    {
        var queue = new InMemoryEmailQueue();
        EmailMessage message = EmailTestHelpers.CreateValidMessage();

        string id = await queue.EnqueueAsync(message);
        EmailQueueItemStatus status = await queue.GetStatusAsync(id);

        id.Should().NotBeNullOrWhiteSpace();
        id.Should().StartWith("email-queue-");
        status.QueueItemId.Should().Be(id);
        status.Status.Should().Be(EmailQueueStatus.Queued);
        status.QueuedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task EnqueueScheduledAsync_ShouldSetScheduledStatusAndSendTime()
    {
        var queue = new InMemoryEmailQueue();
        EmailMessage message = EmailTestHelpers.CreateValidMessage();
        DateTimeOffset scheduledTime = DateTimeOffset.UtcNow.AddHours(1);

        string id = await queue.EnqueueScheduledAsync(message, scheduledTime);
        EmailQueueItemStatus status = await queue.GetStatusAsync(id);

        status.Status.Should().Be(EmailQueueStatus.Scheduled);
        status.ScheduledSendTime.Should().Be(scheduledTime);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetStatusAsync_WithEmptyId_ShouldThrowArgumentException(string? queueItemId)
    {
        var queue = new InMemoryEmailQueue();

        Func<Task> act = () => queue.GetStatusAsync(queueItemId!);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("queueItemId");
    }

    [Fact]
    public async Task GetStatusAsync_WithUnknownId_ShouldThrowInvalidOperationException()
    {
        var queue = new InMemoryEmailQueue();

        Func<Task> act = () => queue.GetStatusAsync("unknown-id");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*unknown-id*not found*");
    }

    [Fact]
    public async Task CancelAsync_ForQueuedItem_ShouldSetCancelledStatus()
    {
        var queue = new InMemoryEmailQueue();
        string id = await queue.EnqueueAsync(EmailTestHelpers.CreateValidMessage());

        await queue.CancelAsync(id);
        EmailQueueItemStatus status = await queue.GetStatusAsync(id);

        status.Status.Should().Be(EmailQueueStatus.Cancelled);
    }

    [Fact]
    public async Task CancelAsync_ForScheduledItem_ShouldSetCancelledStatus()
    {
        var queue = new InMemoryEmailQueue();
        string id = await queue.EnqueueScheduledAsync(
            EmailTestHelpers.CreateValidMessage(),
            DateTimeOffset.UtcNow.AddMinutes(30));

        await queue.CancelAsync(id);
        EmailQueueItemStatus status = await queue.GetStatusAsync(id);

        status.Status.Should().Be(EmailQueueStatus.Cancelled);
    }

    [Fact]
    public async Task CancelAsync_WhenAlreadySent_ShouldThrowInvalidOperationException()
    {
        var queue = new InMemoryEmailQueue();
        string id = await queue.EnqueueAsync(EmailTestHelpers.CreateValidMessage());
        queue.MarkAsSent(id, "sent-msg-id");

        Func<Task> act = () => queue.CancelAsync(id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already sent*");
    }

    [Fact]
    public async Task EnqueueAsync_WhenCancellationRequested_ShouldThrowOperationCanceledException()
    {
        var queue = new InMemoryEmailQueue();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = () => queue.EnqueueAsync(EmailTestHelpers.CreateValidMessage(), cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetStatusAsync_WhenCancellationRequested_ShouldThrowOperationCanceledException()
    {
        var queue = new InMemoryEmailQueue();
        string id = await queue.EnqueueAsync(EmailTestHelpers.CreateValidMessage());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = () => queue.GetStatusAsync(id, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task CancelAsync_WhenCancellationRequested_ShouldThrowOperationCanceledException()
    {
        var queue = new InMemoryEmailQueue();
        string id = await queue.EnqueueAsync(EmailTestHelpers.CreateValidMessage());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = () => queue.CancelAsync(id, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetNextItem_ShouldReturnHighestPriorityQueuedItem()
    {
        var queue = new InMemoryEmailQueue();
        string normalId = await queue.EnqueueAsync(
            EmailTestHelpers.CreateValidMessage(to: "normal@example.com"),
            EmailPriority.Normal);
        string highId = await queue.EnqueueAsync(
            EmailTestHelpers.CreateValidMessage(to: "high@example.com"),
            EmailPriority.High);

        IEmailQueueItem? next = queue.GetNextItem();

        next.Should().NotBeNull();
        next!.QueueItemId.Should().Be(highId);
        next.QueueItemId.Should().NotBe(normalId);
    }
}
