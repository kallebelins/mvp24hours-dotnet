//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Moq;
using Mvp24Hours.Infrastructure.DistributedLocking.Exceptions;
using Mvp24Hours.Infrastructure.DistributedLocking.Metrics;
using Mvp24Hours.Infrastructure.DistributedLocking.Options;
using Mvp24Hours.Infrastructure.DistributedLocking.Providers;
using Mvp24Hours.Infrastructure.DistributedLocking.Results;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.DistributedLocking;

[Trait("Category", "Unit")]
public class InMemoryDistributedLockProviderTest
{
    [Fact]
    public async Task TryAcquireAsync_WithNullOrWhiteSpaceResource_ShouldThrow()
    {
        InMemoryDistributedLockProvider provider = DistributedLockingTestHelpers.CreateInMemory();

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.TryAcquireAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => provider.TryAcquireAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => provider.TryAcquireAsync("   "));
    }

    [Fact]
    public async Task TryAcquireAsync_WhenAvailable_ShouldAcquireAndReturnHandle()
    {
        InMemoryDistributedLockProvider provider = DistributedLockingTestHelpers.CreateInMemory();
        string resource = DistributedLockingTestHelpers.UniqueResource();
        DistributedLockOptions options = DistributedLockingTestHelpers.FastFailOptions();

        LockAcquisitionResult result = await provider.TryAcquireAsync(resource, options);

        try
        {
            result.IsAcquired.Should().BeTrue();
            result.LockHandle.Should().NotBeNull();
            result.LockHandle!.Resource.Should().Be(resource);
            result.LockHandle.IsValid.Should().BeTrue();
            result.LockHandle.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
            (await provider.IsLockedAsync(resource)).Should().BeTrue();
        }
        finally
        {
            if (result.LockHandle is not null)
            {
                await result.LockHandle.DisposeAsync();
            }
        }
    }

    [Fact]
    public async Task TryAcquireAsync_WhenAlreadyHeld_ShouldTimeoutOrFail()
    {
        InMemoryDistributedLockProvider provider = DistributedLockingTestHelpers.CreateInMemory();
        string resource = DistributedLockingTestHelpers.UniqueResource();
        DistributedLockOptions holdOptions = DistributedLockingTestHelpers.FastFailOptions(
            lockDuration: TimeSpan.FromMinutes(1));
        DistributedLockOptions contendOptions = DistributedLockingTestHelpers.FastFailOptions(
            acquisitionTimeout: TimeSpan.Zero,
            retryDelay: TimeSpan.FromMilliseconds(10));

        LockAcquisitionResult first = await provider.TryAcquireAsync(resource, holdOptions);
        first.IsAcquired.Should().BeTrue();

        try
        {
            LockAcquisitionResult second = await provider.TryAcquireAsync(resource, contendOptions);

            second.IsAcquired.Should().BeFalse();
            (second.IsTimeout || second.IsFailed).Should().BeTrue();
            second.LockHandle.Should().BeNull();
        }
        finally
        {
            await first.LockHandle!.DisposeAsync();
        }
    }

    [Fact]
    public async Task TryAcquireAsync_WithThrowOnFailureAndTimeout_ShouldThrow()
    {
        InMemoryDistributedLockProvider provider = DistributedLockingTestHelpers.CreateInMemory();
        string resource = DistributedLockingTestHelpers.UniqueResource();
        DistributedLockOptions holdOptions = DistributedLockingTestHelpers.FastFailOptions();
        DistributedLockOptions contendOptions = DistributedLockingTestHelpers.FastFailOptions(
            acquisitionTimeout: TimeSpan.Zero,
            throwOnFailure: true);

        LockAcquisitionResult first = await provider.TryAcquireAsync(resource, holdOptions);
        first.IsAcquired.Should().BeTrue();

        try
        {
            Func<Task> act = () => provider.TryAcquireAsync(resource, contendOptions);

            DistributedLockAcquisitionException ex = (await act.Should()
                .ThrowAsync<DistributedLockAcquisitionException>()).Which;
            ex.Resource.Should().Be(resource);
            // Zero/short timeout may surface as Timeout (loop exit) or Failed (OCE on retry delay).
            ex.Status.Should().BeOneOf(LockAcquisitionStatus.Timeout, LockAcquisitionStatus.Failed);
        }
        finally
        {
            await first.LockHandle!.DisposeAsync();
        }
    }

    [Fact]
    public async Task TryAcquireAsync_WhenCancelled_ShouldReturnTimeout()
    {
        InMemoryDistributedLockProvider provider = DistributedLockingTestHelpers.CreateInMemory();
        string resource = DistributedLockingTestHelpers.UniqueResource();
        DistributedLockOptions holdOptions = DistributedLockingTestHelpers.FastFailOptions();
        DistributedLockOptions contendOptions = DistributedLockingTestHelpers.FastFailOptions(
            acquisitionTimeout: TimeSpan.FromSeconds(5),
            retryDelay: TimeSpan.FromMilliseconds(50));

        LockAcquisitionResult first = await provider.TryAcquireAsync(resource, holdOptions);
        first.IsAcquired.Should().BeTrue();

        try
        {
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            LockAcquisitionResult result = await provider.TryAcquireAsync(resource, contendOptions, cts.Token);

            result.IsAcquired.Should().BeFalse();
            (result.IsTimeout || result.IsFailed).Should().BeTrue();
        }
        finally
        {
            await first.LockHandle!.DisposeAsync();
        }
    }

    [Fact]
    public async Task TryAcquireAsync_AfterRelease_ShouldAllowReacquire()
    {
        InMemoryDistributedLockProvider provider = DistributedLockingTestHelpers.CreateInMemory();
        string resource = DistributedLockingTestHelpers.UniqueResource();
        DistributedLockOptions options = DistributedLockingTestHelpers.FastFailOptions();

        LockAcquisitionResult first = await provider.TryAcquireAsync(resource, options);
        first.IsAcquired.Should().BeTrue();
        await first.LockHandle!.ReleaseAsync();

        LockAcquisitionResult second = await provider.TryAcquireAsync(resource, options);

        try
        {
            second.IsAcquired.Should().BeTrue();
        }
        finally
        {
            if (second.LockHandle is not null)
            {
                await second.LockHandle.DisposeAsync();
            }
        }
    }

    [Fact]
    public async Task TryAcquireAsync_AfterExpiry_ShouldAllowTakeover()
    {
        InMemoryDistributedLockProvider provider = DistributedLockingTestHelpers.CreateInMemory();
        string resource = DistributedLockingTestHelpers.UniqueResource();
        DistributedLockOptions shortLived = DistributedLockingTestHelpers.FastFailOptions(
            lockDuration: TimeSpan.FromMilliseconds(80));

        LockAcquisitionResult first = await provider.TryAcquireAsync(resource, shortLived);
        first.IsAcquired.Should().BeTrue();

        await Task.Delay(120);

        LockAcquisitionResult second = await provider.TryAcquireAsync(
            resource,
            DistributedLockingTestHelpers.FastFailOptions());

        try
        {
            second.IsAcquired.Should().BeTrue();
            (await first.LockHandle!.ReleaseAsync()).Should().BeFalse();
        }
        finally
        {
            if (second.LockHandle is not null)
            {
                await second.LockHandle.DisposeAsync();
            }
        }
    }

    [Fact]
    public async Task IsLockedAsync_WhenExpired_ShouldReturnFalseAndCleanup()
    {
        InMemoryDistributedLockProvider provider = DistributedLockingTestHelpers.CreateInMemory();
        string resource = DistributedLockingTestHelpers.UniqueResource();
        DistributedLockOptions shortLived = DistributedLockingTestHelpers.FastFailOptions(
            lockDuration: TimeSpan.FromMilliseconds(50));

        LockAcquisitionResult result = await provider.TryAcquireAsync(resource, shortLived);
        result.IsAcquired.Should().BeTrue();

        await Task.Delay(80);

        (await provider.IsLockedAsync(resource)).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IsLockedAsync_WithInvalidResource_ShouldThrow(string? resource)
    {
        InMemoryDistributedLockProvider provider = DistributedLockingTestHelpers.CreateInMemory();

        Func<Task> act = () => provider.IsLockedAsync(resource!);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RenewAsync_WhenHeld_ShouldExtendExpiration()
    {
        InMemoryDistributedLockProvider provider = DistributedLockingTestHelpers.CreateInMemory();
        string resource = DistributedLockingTestHelpers.UniqueResource();
        DistributedLockOptions options = DistributedLockingTestHelpers.FastFailOptions(
            lockDuration: TimeSpan.FromSeconds(2));

        LockAcquisitionResult result = await provider.TryAcquireAsync(resource, options);
        result.IsAcquired.Should().BeTrue();

        try
        {
            DateTimeOffset before = result.LockHandle!.ExpiresAt;
            await Task.Delay(20);

            bool renewed = await result.LockHandle.RenewAsync();

            renewed.Should().BeTrue();
            result.LockHandle.ExpiresAt.Should().BeAfter(before);
            result.LockHandle.IsValid.Should().BeTrue();
        }
        finally
        {
            await result.LockHandle!.DisposeAsync();
        }
    }

    [Fact]
    public async Task RenewAsync_AfterRelease_ShouldReturnFalse()
    {
        InMemoryDistributedLockProvider provider = DistributedLockingTestHelpers.CreateInMemory();
        string resource = DistributedLockingTestHelpers.UniqueResource();

        LockAcquisitionResult result = await provider.TryAcquireAsync(
            resource,
            DistributedLockingTestHelpers.FastFailOptions());
        await result.LockHandle!.ReleaseAsync();

        (await result.LockHandle.RenewAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task ReleaseAsync_Twice_ShouldReturnFalseOnSecondCall()
    {
        InMemoryDistributedLockProvider provider = DistributedLockingTestHelpers.CreateInMemory();
        string resource = DistributedLockingTestHelpers.UniqueResource();

        LockAcquisitionResult result = await provider.TryAcquireAsync(
            resource,
            DistributedLockingTestHelpers.FastFailOptions());

        (await result.LockHandle!.ReleaseAsync()).Should().BeTrue();
        (await result.LockHandle.ReleaseAsync()).Should().BeFalse();
        result.LockHandle.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task DisposeAsync_ShouldReleaseLock()
    {
        InMemoryDistributedLockProvider provider = DistributedLockingTestHelpers.CreateInMemory();
        string resource = DistributedLockingTestHelpers.UniqueResource();

        LockAcquisitionResult result = await provider.TryAcquireAsync(
            resource,
            DistributedLockingTestHelpers.FastFailOptions());

        await result.LockHandle!.DisposeAsync();

        (await provider.IsLockedAsync(resource)).Should().BeFalse();
        result.LockHandle.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Dispose_ShouldReleaseLock()
    {
        InMemoryDistributedLockProvider provider = DistributedLockingTestHelpers.CreateInMemory();
        string resource = DistributedLockingTestHelpers.UniqueResource();

        LockAcquisitionResult result = await provider.TryAcquireAsync(
            resource,
            DistributedLockingTestHelpers.FastFailOptions());

        result.LockHandle!.Dispose();

        (await provider.IsLockedAsync(resource)).Should().BeFalse();
    }

    [Fact]
    public async Task TryAcquireWithFenceAsync_WithoutFencingSupport_ShouldAcquireNormally()
    {
        InMemoryDistributedLockProvider provider = DistributedLockingTestHelpers.CreateInMemory();
        string resource = DistributedLockingTestHelpers.UniqueResource();

        LockAcquisitionResult result = await provider.TryAcquireWithFenceAsync(
            resource,
            DistributedLockingTestHelpers.FastFailOptions());

        try
        {
            result.IsAcquired.Should().BeTrue();
            result.FencedToken.Should().BeNull();
        }
        finally
        {
            if (result.LockHandle is not null)
            {
                await result.LockHandle.DisposeAsync();
            }
        }
    }

    [Fact]
    public async Task TryAcquireAsync_WithMetrics_ShouldRecordAcquisitionAndRelease()
    {
        var metrics = new DistributedLockMetrics();
        var provider = new InMemoryDistributedLockProvider(metrics: metrics);
        string resource = DistributedLockingTestHelpers.UniqueResource();

        LockAcquisitionResult result = await provider.TryAcquireAsync(
            resource,
            DistributedLockingTestHelpers.FastFailOptions());
        result.IsAcquired.Should().BeTrue();
        await result.LockHandle!.ReleaseAsync();

        LockResourceMetrics? snapshot = metrics.GetMetrics(resource);
        snapshot.Should().NotBeNull();
        snapshot!.SuccessfulAttempts.Should().Be(1);
        snapshot.Releases.Should().Be(1);
    }

    [Fact]
    public async Task TryAcquireAsync_WithLogger_ShouldNotThrow()
    {
        var logger = new Mock<ILogger<InMemoryDistributedLockProvider>>();
        var provider = new InMemoryDistributedLockProvider(logger.Object);
        string resource = DistributedLockingTestHelpers.UniqueResource();

        LockAcquisitionResult result = await provider.TryAcquireAsync(
            resource,
            DistributedLockingTestHelpers.FastFailOptions());

        try
        {
            result.IsAcquired.Should().BeTrue();
        }
        finally
        {
            if (result.LockHandle is not null)
            {
                await result.LockHandle.DisposeAsync();
            }
        }
    }

    [Fact]
    public async Task AutoRenewal_WhenEnabled_ShouldKeepLockValid()
    {
        InMemoryDistributedLockProvider provider = DistributedLockingTestHelpers.CreateInMemory();
        string resource = DistributedLockingTestHelpers.UniqueResource();
        DistributedLockOptions options = DistributedLockingTestHelpers.FastFailOptions(
            lockDuration: TimeSpan.FromMilliseconds(400),
            enableAutoRenewal: true,
            renewalInterval: TimeSpan.FromMilliseconds(100));

        LockAcquisitionResult result = await provider.TryAcquireAsync(resource, options);
        result.IsAcquired.Should().BeTrue();

        try
        {
            await Task.Delay(350);

            result.LockHandle!.IsValid.Should().BeTrue();
            (await provider.IsLockedAsync(resource)).Should().BeTrue();
        }
        finally
        {
            await result.LockHandle!.DisposeAsync();
        }
    }

    [Fact]
    public async Task DifferentResources_ShouldNotContend()
    {
        InMemoryDistributedLockProvider provider = DistributedLockingTestHelpers.CreateInMemory();
        string resourceA = DistributedLockingTestHelpers.UniqueResource("a");
        string resourceB = DistributedLockingTestHelpers.UniqueResource("b");
        DistributedLockOptions options = DistributedLockingTestHelpers.FastFailOptions();

        LockAcquisitionResult a = await provider.TryAcquireAsync(resourceA, options);
        LockAcquisitionResult b = await provider.TryAcquireAsync(resourceB, options);

        try
        {
            a.IsAcquired.Should().BeTrue();
            b.IsAcquired.Should().BeTrue();
        }
        finally
        {
            if (a.LockHandle is not null)
            {
                await a.LockHandle.DisposeAsync();
            }

            if (b.LockHandle is not null)
            {
                await b.LockHandle.DisposeAsync();
            }
        }
    }
}
