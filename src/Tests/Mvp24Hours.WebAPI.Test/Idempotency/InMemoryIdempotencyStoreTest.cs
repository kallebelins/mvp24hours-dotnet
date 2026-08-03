using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.WebAPI.Idempotency;

namespace Mvp24Hours.WebAPI.Test.Idempotency;

[Trait("Category", "Unit")]
public class InMemoryIdempotencyStoreTest
{
    [Fact]
    public async Task TryAcquireLockAsync_Should_AcquireLock_OnFirstRequest()
    {
        using var sut = new InMemoryIdempotencyStore(NullLogger<InMemoryIdempotencyStore>.Instance);

        IdempotencyLockResult result = await sut.TryAcquireLockAsync(
            "key-1", "/api/orders", "POST", null, TimeSpan.FromMinutes(5), "corr-1");

        result.Acquired.Should().BeTrue();
        (await sut.ExistsAsync("key-1")).Should().BeTrue();
    }

    [Fact]
    public async Task TryAcquireLockAsync_Should_ReturnExistingRecord_OnDuplicateKey()
    {
        using var sut = new InMemoryIdempotencyStore();
        await sut.TryAcquireLockAsync("key-1", "/api/orders", "POST", null, TimeSpan.FromMinutes(5));

        IdempotencyLockResult second = await sut.TryAcquireLockAsync("key-1", "/api/orders", "POST", null, TimeSpan.FromMinutes(5));

        second.Acquired.Should().BeFalse();
        second.ExistingRecord.Should().NotBeNull();
        second.IsInFlight.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteAsync_Should_MarkRecordCompleted()
    {
        using var sut = new InMemoryIdempotencyStore();
        await sut.TryAcquireLockAsync("key-1", "/api/orders", "POST", null, TimeSpan.FromMinutes(5));

        await sut.CompleteAsync("key-1", 201, Encoding.UTF8.GetBytes("{\"ok\":true}"), "application/json");
        IdempotencyRecord? record = await sut.GetAsync("key-1");

        record.Should().NotBeNull();
        record!.IsCompleted.Should().BeTrue();
        record.StatusCode.Should().Be(201);
        record.ResponseBody.Should().NotBeEmpty();
    }

    [Fact]
    public async Task FailAsync_Should_RemoveRecord_WhenRemoveRecordIsTrue()
    {
        using var sut = new InMemoryIdempotencyStore();
        await sut.TryAcquireLockAsync("key-1", "/api/orders", "POST", null, TimeSpan.FromMinutes(5));

        await sut.FailAsync("key-1", removeRecord: true);

        (await sut.ExistsAsync("key-1")).Should().BeFalse();
    }

    [Fact]
    public async Task FailAsync_Should_MarkFailed_WhenRemoveRecordIsFalse()
    {
        using var sut = new InMemoryIdempotencyStore();
        await sut.TryAcquireLockAsync("key-1", "/api/orders", "POST", null, TimeSpan.FromMinutes(5));

        await sut.FailAsync("key-1", removeRecord: false);
        IdempotencyRecord? record = await sut.GetAsync("key-1");

        record.Should().NotBeNull();
        record!.Status.Should().Be(IdempotencyRecordStatus.Failed);
    }

    [Fact]
    public async Task RemoveAsync_Should_DeleteRecord()
    {
        using var sut = new InMemoryIdempotencyStore();
        await sut.TryAcquireLockAsync("key-1", "/api/orders", "POST", null, TimeSpan.FromMinutes(5));

        await sut.RemoveAsync("key-1");

        (await sut.ExistsAsync("key-1")).Should().BeFalse();
    }

    [Fact]
    public async Task GetAsync_Should_ReturnNull_ForExpiredRecord()
    {
        using var sut = new InMemoryIdempotencyStore();
        await sut.TryAcquireLockAsync("key-1", "/api/orders", "POST", null, TimeSpan.FromMilliseconds(1));
        await Task.Delay(15);

        IdempotencyRecord? record = await sut.GetAsync("key-1");

        record.Should().BeNull();
    }

    [Fact]
    public async Task TryAcquireLockAsync_Should_ReplaceExpiredRecord()
    {
        using var sut = new InMemoryIdempotencyStore();
        await sut.TryAcquireLockAsync("key-1", "/api/orders", "POST", null, TimeSpan.FromMilliseconds(1));
        await Task.Delay(15);

        IdempotencyLockResult result = await sut.TryAcquireLockAsync("key-1", "/api/orders", "POST", null, TimeSpan.FromMinutes(5));

        result.Acquired.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteAsync_Should_LogWarning_WhenRecordMissing()
    {
        using var sut = new InMemoryIdempotencyStore(NullLogger<InMemoryIdempotencyStore>.Instance);

        Func<Task> act = () => sut.CompleteAsync("missing", 200, [], "application/json");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Operations_Should_HonorCancellationToken()
    {
        using var sut = new InMemoryIdempotencyStore();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = () => sut.TryAcquireLockAsync("key-1", "/api", "POST", null, TimeSpan.FromMinutes(1), cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Dispose_Should_ClearRecords()
    {
        var sut = new InMemoryIdempotencyStore();
        await sut.TryAcquireLockAsync("key-1", "/api/orders", "POST", null, TimeSpan.FromMinutes(5));

        sut.Dispose();

        IdempotencyLockResult result = await sut.TryAcquireLockAsync("key-1", "/api/orders", "POST", null, TimeSpan.FromMinutes(5));
        result.Acquired.Should().BeTrue();
    }
}
