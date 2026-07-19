using Microsoft.Extensions.Logging;
using Moq;
using Mvp24Hours.Infrastructure.Data.MongoDb.Interceptors;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Interceptors;

[Trait("Category", "Unit")]
public class InterceptorsTest
{
    [Fact]
    public async Task Pipeline_ShouldExecuteInterceptorsInOrderOnInsert()
    {
        var calls = new List<string>();
        IMongoDbInterceptor first = new RecordingInterceptor(0, calls);
        IMongoDbInterceptor second = new RecordingInterceptor(100, calls);

        var pipeline = new MongoDbInterceptorPipeline([second, first]);
        pipeline.InterceptorCount.Should().Be(2);
        pipeline.HasInterceptors.Should().BeTrue();

        var entity = new TestEntity { Name = "insert" };
        bool executed = false;

        await pipeline.ExecuteInsertAsync(entity, () =>
        {
            executed = true;
            calls.Add("operation");
            return Task.CompletedTask;
        });

        executed.Should().BeTrue();
        calls.Should().Equal("before:0", "before:100", "operation", "after:100", "after:0");
    }

    [Fact]
    public async Task Pipeline_ShouldConvertDeleteToSoftDelete()
    {
        var interceptor = new SoftDeleteInterceptorMock();
        var pipeline = new MongoDbInterceptorPipeline([interceptor]);

        bool hardDelete = false;
        bool softDelete = false;
        var entity = new TestEntity();

        bool wasSoft = await pipeline.ExecuteDeleteAsync(
            entity,
            () => { hardDelete = true; return Task.CompletedTask; },
            () => { softDelete = true; return Task.CompletedTask; });

        wasSoft.Should().BeTrue();
        softDelete.Should().BeTrue();
        hardDelete.Should().BeFalse();
    }

    [Fact]
    public async Task Pipeline_ShouldSuppressDeleteWhenInterceptorRequests()
    {
        var interceptor = new SuppressDeleteInterceptor();
        var pipeline = new MongoDbInterceptorPipeline([interceptor]);

        bool hardDelete = false;
        bool softDelete = false;

        bool wasSoft = await pipeline.ExecuteDeleteAsync(
            new TestEntity(),
            () => { hardDelete = true; return Task.CompletedTask; },
            () => { softDelete = true; return Task.CompletedTask; });

        wasSoft.Should().BeFalse();
        hardDelete.Should().BeFalse();
        softDelete.Should().BeFalse();
    }

    [Fact]
    public async Task NoOpInterceptorPipeline_ShouldExecuteOperationDirectly()
    {
        IMongoDbInterceptorPipeline pipeline = NoOpInterceptorPipeline.Instance;
        pipeline.HasInterceptors.Should().BeFalse();

        bool executed = false;
        await pipeline.ExecuteInsertAsync(new TestEntity(), () =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        executed.Should().BeTrue();
    }

    [Fact]
    public async Task TenantInterceptor_ShouldSetTenantOnInsert()
    {
        var provider = new FakeTenantProvider("tenant-1");
        var interceptor = new TenantInterceptor(provider);

        var entity = new TenantInvoice();
        await interceptor.OnBeforeInsertAsync(entity);

        entity.TenantId.Should().Be("tenant-1");
    }

    [Fact]
    public async Task TenantInterceptor_ShouldThrowOnCrossTenantUpdate()
    {
        var provider = new FakeTenantProvider("tenant-a");
        var interceptor = new TenantInterceptor(provider);

        var entity = new TenantInvoice { TenantId = "tenant-b" };

        Func<Task> act = () => interceptor.OnBeforeUpdateAsync(entity);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task TenantInterceptor_ShouldSetGenericTenantIdAsGuid()
    {
        var tenantGuid = Guid.NewGuid();
        var provider = new FakeTenantProvider(tenantGuid.ToString());
        var interceptor = new TenantInterceptor(provider);

        var entity = new TenantOrder();
        await interceptor.OnBeforeInsertAsync(entity);

        entity.TenantId.Should().Be(tenantGuid);
    }

    [Fact]
    public async Task TenantInterceptor_ShouldThrowWhenTenantMissingOnInsert()
    {
        var interceptor = new TenantInterceptor(new FakeTenantProvider(null));

        Func<Task> act = () => interceptor.OnBeforeInsertAsync(new TenantInvoice());
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CommandLogger_ShouldTrackInsertLifecycle()
    {
        var logger = new Mock<ILogger<CommandLogger>>();
        var commandLogger = new CommandLogger(logger.Object, logAllOperations: true);

        var entity = new TestEntity { Name = "logged" };
        await commandLogger.OnBeforeInsertAsync(entity);
        await commandLogger.OnAfterInsertAsync(entity);

        logger.Verify(
            l => l.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Insert", StringComparison.OrdinalIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void CommandLogger_ShouldLogOperationFailure()
    {
        var logger = new Mock<ILogger<CommandLogger>>();
        var commandLogger = new CommandLogger(logger.Object);
        var entity = new TestEntity();

        commandLogger.LogOperationFailure("Update", entity, new InvalidOperationException("boom"));

        logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private sealed class RecordingInterceptor(int order, List<string> calls) : MongoDbInterceptorBase
    {
        public override int Order => order;

        public override Task OnBeforeInsertAsync<T>(T entity, CancellationToken cancellationToken = default)
        {
            calls.Add($"before:{Order}");
            return Task.CompletedTask;
        }

        public override Task OnAfterInsertAsync<T>(T entity, CancellationToken cancellationToken = default)
        {
            calls.Add($"after:{Order}");
            return Task.CompletedTask;
        }
    }

    private sealed class SoftDeleteInterceptorMock : MongoDbInterceptorBase
    {
        public override Task<DeleteInterceptionResult> OnBeforeDeleteAsync<T>(T entity, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DeleteInterceptionResult.SoftDelete());
        }
    }

    private sealed class SuppressDeleteInterceptor : MongoDbInterceptorBase
    {
        public override Task<DeleteInterceptionResult> OnBeforeDeleteAsync<T>(T entity, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DeleteInterceptionResult.SuppressOperation());
        }
    }
}
