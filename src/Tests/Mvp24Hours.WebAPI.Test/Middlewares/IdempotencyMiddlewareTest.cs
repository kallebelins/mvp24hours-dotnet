using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Idempotency;
using Mvp24Hours.WebAPI.Middlewares;
using Mvp24Hours.WebAPI.Test.Support;

namespace Mvp24Hours.WebAPI.Test.Middlewares;

[Trait("Category", "Unit")]
public class IdempotencyMiddlewareTest
{
    // -----------------------------------------------------------------------
    // Bypass scenarios
    // -----------------------------------------------------------------------

    [Fact]
    public async Task IdempotencyMiddleware_Should_Bypass_WhenDisabled()
    {
        var called = false;
        var sut = CreateMiddleware(
            new IdempotencyOptions { Enabled = false },
            _ => { called = true; return Task.CompletedTask; });

        var (store, keyGen) = CreateMocks();
        await sut.InvokeAsync(WebApiTestHelpers.CreateHttpContext(), store.Object, keyGen.Object);

        called.Should().BeTrue();
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IdempotencyMiddleware_Should_Bypass_ForGetMethod()
    {
        var called = false;
        var sut = CreateMiddleware(
            new IdempotencyOptions { IdempotentMethods = ["POST", "PUT", "PATCH"] },
            _ => { called = true; return Task.CompletedTask; });

        var (store, keyGen) = CreateMocks();
        var context = WebApiTestHelpers.CreateHttpContext(method: "GET");

        await sut.InvokeAsync(context, store.Object, keyGen.Object);

        called.Should().BeTrue();
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IdempotencyMiddleware_Should_Bypass_ForExcludedPath()
    {
        var called = false;
        var options = new IdempotencyOptions
        {
            ExcludedPaths = ["/health"]
        };
        var sut = CreateMiddleware(options, _ => { called = true; return Task.CompletedTask; });

        var (store, keyGen) = CreateMocks();
        keyGen.Setup(g => g.GenerateKeyAsync(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
            .ReturnsAsync(IdempotencyKeyResult.NoKey());

        var context = WebApiTestHelpers.CreateHttpContext(method: "POST", path: "/health");
        await sut.InvokeAsync(context, store.Object, keyGen.Object);

        called.Should().BeTrue();
    }

    // -----------------------------------------------------------------------
    // No key provided
    // -----------------------------------------------------------------------

    [Fact]
    public async Task IdempotencyMiddleware_Should_CallNext_WhenKeyMissingAndNotRequired()
    {
        var called = false;
        var options = new IdempotencyOptions { RequireIdempotencyKey = false };
        var sut = CreateMiddleware(options, _ => { called = true; return Task.CompletedTask; });

        var (store, keyGen) = CreateMocks();
        keyGen.Setup(g => g.GenerateKeyAsync(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
            .ReturnsAsync(IdempotencyKeyResult.NoKey());

        var context = WebApiTestHelpers.CreateHttpContext(method: "POST");
        await sut.InvokeAsync(context, store.Object, keyGen.Object);

        called.Should().BeTrue();
    }

    [Fact]
    public async Task IdempotencyMiddleware_Should_Return400_WhenKeyMissingAndRequired()
    {
        var options = new IdempotencyOptions
        {
            RequireIdempotencyKey = true,
            UseProblemDetails = false
        };
        var sut = CreateMiddleware(options, _ => Task.CompletedTask);

        var (store, keyGen) = CreateMocks();
        keyGen.Setup(g => g.GenerateKeyAsync(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
            .ReturnsAsync(IdempotencyKeyResult.NoKey());

        var context = WebApiTestHelpers.CreateHttpContext(method: "POST");
        await sut.InvokeAsync(context, store.Object, keyGen.Object);

        context.Response.StatusCode.Should().Be(400);
    }

    // -----------------------------------------------------------------------
    // Lock acquired — execute and cache
    // -----------------------------------------------------------------------

    [Fact]
    public async Task IdempotencyMiddleware_Should_ExecuteAndCache_WhenLockAcquired()
    {
        var called = false;
        var options = new IdempotencyOptions
        {
            RequireIdempotencyKey = false,
            IncludeKeyInResponse = true,
            NonCacheableStatusCodes = [500]
        };
        var sut = CreateMiddleware(options, async c =>
        {
            called = true;
            c.Response.StatusCode = 201;
            await c.Response.WriteAsync("created");
        });

        var (store, keyGen) = CreateMocks();
        keyGen.Setup(g => g.GenerateKeyAsync(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
            .ReturnsAsync(IdempotencyKeyResult.FromHeader("key-1"));

        store.Setup(s => s.TryAcquireLockAsync(
            "key-1", It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<TimeSpan>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotencyLockResult { Acquired = true });

        store.Setup(s => s.CompleteAsync(
            "key-1", It.IsAny<int>(), It.IsAny<byte[]>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = WebApiTestHelpers.CreateHttpContext(method: "POST");
        await sut.InvokeAsync(context, store.Object, keyGen.Object);

        called.Should().BeTrue();
        store.Verify(s => s.CompleteAsync("key-1", 201, It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // -----------------------------------------------------------------------
    // Cached response (replay)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task IdempotencyMiddleware_Should_ReplayResponse_WhenAlreadyCompleted()
    {
        var options = new IdempotencyOptions { IncludeKeyInResponse = false };
        var sut = CreateMiddleware(options, _ => Task.CompletedTask);

        var existingRecord = new IdempotencyRecord
        {
            Key = "key-2",
            StatusCode = 200,
            ContentType = "application/json",
            ResponseBody = Encoding.UTF8.GetBytes("{\"ok\":true}"),
            Status = IdempotencyRecordStatus.Completed
        };

        var (store, keyGen) = CreateMocks();
        keyGen.Setup(g => g.GenerateKeyAsync(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
            .ReturnsAsync(IdempotencyKeyResult.FromHeader("key-2"));

        store.Setup(s => s.TryAcquireLockAsync(
            "key-2", It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<TimeSpan>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotencyLockResult
            {
                Acquired = false,
                ExistingRecord = existingRecord
            });

        var context = WebApiTestHelpers.CreateHttpContext(method: "POST");
        await sut.InvokeAsync(context, store.Object, keyGen.Object);

        context.Response.StatusCode.Should().Be(200);
    }

    // -----------------------------------------------------------------------
    // In-flight response
    // -----------------------------------------------------------------------

    [Fact]
    public async Task IdempotencyMiddleware_Should_Return409_WhenRequestInFlight()
    {
        var options = new IdempotencyOptions
        {
            InFlightStatusCode = 409,
            UseProblemDetails = false
        };
        var sut = CreateMiddleware(options, _ => Task.CompletedTask);

        var (store, keyGen) = CreateMocks();
        keyGen.Setup(g => g.GenerateKeyAsync(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
            .ReturnsAsync(IdempotencyKeyResult.FromHeader("key-3"));

        store.Setup(s => s.TryAcquireLockAsync(
            "key-3", It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<TimeSpan>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotencyLockResult
            {
                Acquired = false,
                ExistingRecord = new IdempotencyRecord { Status = IdempotencyRecordStatus.Processing }
            });

        var context = WebApiTestHelpers.CreateHttpContext(method: "POST");
        await sut.InvokeAsync(context, store.Object, keyGen.Object);

        context.Response.StatusCode.Should().Be(409);
    }

    // -----------------------------------------------------------------------
    // Non-cacheable status codes
    // -----------------------------------------------------------------------

    [Fact]
    public async Task IdempotencyMiddleware_Should_NotCache_WhenStatusIsNonCacheable()
    {
        var options = new IdempotencyOptions
        {
            NonCacheableStatusCodes = [500, 503]
        };
        var sut = CreateMiddleware(options, c =>
        {
            c.Response.StatusCode = 500;
            return Task.CompletedTask;
        });

        var (store, keyGen) = CreateMocks();
        keyGen.Setup(g => g.GenerateKeyAsync(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
            .ReturnsAsync(IdempotencyKeyResult.FromHeader("key-4"));

        store.Setup(s => s.TryAcquireLockAsync(
            "key-4", It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<TimeSpan>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotencyLockResult { Acquired = true });

        store.Setup(s => s.FailAsync("key-4", true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = WebApiTestHelpers.CreateHttpContext(method: "POST");
        await sut.InvokeAsync(context, store.Object, keyGen.Object);

        store.Verify(s => s.FailAsync("key-4", true, It.IsAny<CancellationToken>()), Times.Once);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static IdempotencyMiddleware CreateMiddleware(IdempotencyOptions options, RequestDelegate next)
    {
        return new IdempotencyMiddleware(
            next,
            Options.Create(options),
            NullLogger<IdempotencyMiddleware>.Instance);
    }

    private static (Mock<IIdempotencyStore> store, Mock<IIdempotencyKeyGenerator> keyGen) CreateMocks()
    {
        return (new Mock<IIdempotencyStore>(), new Mock<IIdempotencyKeyGenerator>());
    }
}
