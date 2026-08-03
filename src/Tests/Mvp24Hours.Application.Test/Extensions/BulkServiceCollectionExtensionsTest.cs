using Mvp24Hours.Application.Extensions;
using Mvp24Hours.Application.Test.Support;
using Mvp24Hours.Core.Contract.Logic;
using Mvp24Hours.Extensions;

namespace Mvp24Hours.Application.Test.Extensions;

[Trait("Category", "Unit")]
public class BulkServiceCollectionExtensionsTest
{
    [Fact]
    public void AddBulkCommandService_ShouldRegisterScopedImplementation()
    {
        var services = new ServiceCollection();

        services.AddBulkCommandService<ITestBulkMarkerService, TestBulkMarkerService>();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ITestBulkMarkerService>()
            .Should().BeOfType<TestBulkMarkerService>();
        services.Should().Contain(d =>
            d.ServiceType == typeof(ITestBulkMarkerService) &&
            d.ImplementationType == typeof(TestBulkMarkerService) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddBulkCommandService_WithLifetime_ShouldRegisterWithSpecifiedLifetime()
    {
        var services = new ServiceCollection();

        services.AddBulkCommandService<ITestBulkMarkerService, TestBulkMarkerService>(ServiceLifetime.Singleton);

        services.Should().Contain(d =>
            d.ServiceType == typeof(ITestBulkMarkerService) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddBulkCommandService_WithFactory_ShouldResolveFromFactory()
    {
        var services = new ServiceCollection();
        var instance = new TestBulkMarkerService();

        services.AddBulkCommandService<ITestBulkMarkerService>(_ => instance);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ITestBulkMarkerService>().Should().BeSameAs(instance);
    }

    [Fact]
    public void AddBulkCommandServiceWithDto_ShouldRegisterScopedImplementation()
    {
        var services = new ServiceCollection();

        services.AddBulkCommandServiceWithDto<ITestBulkDtoMarkerService, TestBulkDtoMarkerService>();

        services.Should().Contain(d =>
            d.ServiceType == typeof(ITestBulkDtoMarkerService) &&
            d.ImplementationType == typeof(TestBulkDtoMarkerService) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddBulkCommandServiceWithDto_WithLifetime_ShouldRegisterWithSpecifiedLifetime()
    {
        var services = new ServiceCollection();

        services.AddBulkCommandServiceWithDto<ITestBulkDtoMarkerService, TestBulkDtoMarkerService>(ServiceLifetime.Transient);

        services.Should().Contain(d =>
            d.ServiceType == typeof(ITestBulkDtoMarkerService) &&
            d.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void AddBulkCommandServiceAsync_ShouldRegisterEntityBulkInterface()
    {
        var services = new ServiceCollection();

        services.AddBulkCommandServiceAsync<AppTestEntity, TestBulkEntityService>();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IBulkCommandServiceAsync<AppTestEntity>>()
            .Should().BeOfType<TestBulkEntityService>();
    }

    [Fact]
    public void AddBulkCommandServiceWithDtoAsync_ShouldRegisterDtoBulkInterface()
    {
        var services = new ServiceCollection();

        services.AddBulkCommandServiceWithDtoAsync<AppTestEntityDto, TestBulkDtoServiceStub>();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IBulkCommandServiceWithDtoAsync<AppTestEntityDto>>()
            .Should().BeOfType<TestBulkDtoServiceStub>();
    }

    [Fact]
    public void AddBulkCommandServiceWithSeparateDtosAsync_ShouldRegisterSeparateDtoBulkInterface()
    {
        var services = new ServiceCollection();

        services.AddBulkCommandServiceWithSeparateDtosAsync<
            AppTestCreateDto,
            AppTestUpdateDto,
            TestBulkSeparateDtosServiceStub>();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IBulkCommandServiceWithSeparateDtosAsync<AppTestCreateDto, AppTestUpdateDto>>()
            .Should().BeOfType<TestBulkSeparateDtosServiceStub>();
    }

    [Fact]
    public void AddBulkCommandService_ShouldReturnSameServiceCollectionForChaining()
    {
        var services = new ServiceCollection();

        IServiceCollection result = services.AddBulkCommandService<ITestBulkMarkerService, TestBulkMarkerService>();

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddBulkCommandServiceWithDtoAsync_ShouldReturnSameServiceCollectionForChaining()
    {
        var services = new ServiceCollection();

        IServiceCollection result = services.AddBulkCommandServiceWithDtoAsync<AppTestEntityDto, TestBulkDtoServiceStub>();

        result.Should().BeSameAs(services);
    }

    public interface ITestBulkMarkerService;

    public sealed class TestBulkMarkerService : ITestBulkMarkerService;

    public interface ITestBulkDtoMarkerService;

    public sealed class TestBulkDtoMarkerService : ITestBulkDtoMarkerService;

    private sealed class TestBulkEntityService : IBulkCommandServiceAsync<AppTestEntity>
    {
        public Task<IBusinessResult<BulkOperationResult>> BulkAddAsync(IList<AppTestEntity> entities, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(BulkOperationResult.Success(entities.Count, TimeSpan.Zero).ToBusiness());
        }

        public Task<IBusinessResult<BulkOperationResult>> BulkAddAsync(IList<AppTestEntity> entities, BulkOperationOptions options, CancellationToken cancellationToken = default)
        {
            return BulkAddAsync(entities, cancellationToken);
        }

        public Task<IBusinessResult<BulkOperationResult>> BulkModifyAsync(IList<AppTestEntity> entities, CancellationToken cancellationToken = default)
        {
            return BulkAddAsync(entities, cancellationToken);
        }

        public Task<IBusinessResult<BulkOperationResult>> BulkModifyAsync(IList<AppTestEntity> entities, BulkOperationOptions options, CancellationToken cancellationToken = default)
        {
            return BulkAddAsync(entities, cancellationToken);
        }

        public Task<IBusinessResult<BulkOperationResult>> BulkRemoveAsync(IList<AppTestEntity> entities, CancellationToken cancellationToken = default)
        {
            return BulkAddAsync(entities, cancellationToken);
        }

        public Task<IBusinessResult<BulkOperationResult>> BulkRemoveAsync(IList<AppTestEntity> entities, BulkOperationOptions options, CancellationToken cancellationToken = default)
        {
            return BulkAddAsync(entities, cancellationToken);
        }
    }

    private sealed class TestBulkDtoServiceStub : IBulkCommandServiceWithDtoAsync<AppTestEntityDto>
    {
        public Task<IBusinessResult<BulkOperationResult>> BulkAddAsync(IList<AppTestEntityDto> dtos, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(BulkOperationResult.Success(dtos.Count, TimeSpan.Zero).ToBusiness());
        }

        public Task<IBusinessResult<BulkOperationResult>> BulkAddAsync(IList<AppTestEntityDto> dtos, BulkOperationOptions options, CancellationToken cancellationToken = default)
        {
            return BulkAddAsync(dtos, cancellationToken);
        }

        public Task<IBusinessResult<BulkOperationResult>> BulkModifyAsync(IList<AppTestEntityDto> dtos, CancellationToken cancellationToken = default)
        {
            return BulkAddAsync(dtos, cancellationToken);
        }

        public Task<IBusinessResult<BulkOperationResult>> BulkModifyAsync(IList<AppTestEntityDto> dtos, BulkOperationOptions options, CancellationToken cancellationToken = default)
        {
            return BulkAddAsync(dtos, cancellationToken);
        }

        public Task<IBusinessResult<BulkOperationResult>> BulkRemoveAsync(IList<AppTestEntityDto> dtos, CancellationToken cancellationToken = default)
        {
            return BulkAddAsync(dtos, cancellationToken);
        }

        public Task<IBusinessResult<BulkOperationResult>> BulkRemoveAsync(IList<AppTestEntityDto> dtos, BulkOperationOptions options, CancellationToken cancellationToken = default)
        {
            return BulkAddAsync(dtos, cancellationToken);
        }
    }

    private sealed class TestBulkSeparateDtosServiceStub
        : IBulkCommandServiceWithSeparateDtosAsync<AppTestCreateDto, AppTestUpdateDto>
    {
        public Task<IBusinessResult<BulkOperationResult>> BulkAddAsync(IList<AppTestCreateDto> dtos, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(BulkOperationResult.Success(dtos.Count, TimeSpan.Zero).ToBusiness());
        }

        public Task<IBusinessResult<BulkOperationResult>> BulkAddAsync(IList<AppTestCreateDto> dtos, BulkOperationOptions options, CancellationToken cancellationToken = default)
        {
            return BulkAddAsync(dtos, cancellationToken);
        }

        public Task<IBusinessResult<BulkOperationResult>> BulkModifyAsync(IList<AppTestUpdateDto> dtos, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(BulkOperationResult.Success(dtos.Count, TimeSpan.Zero).ToBusiness());
        }

        public Task<IBusinessResult<BulkOperationResult>> BulkModifyAsync(IList<AppTestUpdateDto> dtos, BulkOperationOptions options, CancellationToken cancellationToken = default)
        {
            return BulkModifyAsync(dtos, cancellationToken);
        }
    }
}
