using AutoMapper;
using Mvp24Hours.Application.Contract.Resilience;
using Mvp24Hours.Application.Contract.Transaction;
using Mvp24Hours.Application.Extensions;
using Mvp24Hours.Application.Contract.Cache;
using Mvp24Hours.Application.Logic.Pagination;
using Mvp24Hours.Application.Test.Support;
using Mvp24Hours.Core.Contract.Logic;
using Mvp24Hours.Extensions;

namespace Mvp24Hours.Application.Test.Extensions;

[Trait("Category", "Unit")]
public class ApplicationModuleServiceCollectionExtensionsTest
{
    [Fact]
    public void AddMvp24HoursApplicationModule_WithoutAssemblies_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddMvp24HoursApplicationModule();

        act.Should().Throw<ArgumentException>()
            .WithParameterName("assemblies");
    }

    [Fact]
    public void AddMvp24HoursApplicationModule_ShouldRegisterCoreServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IUnitOfWorkAsync, MockUnitOfWorkAsync>();
        services.AddSingleton<IUnitOfWork, MockUnitOfWork>();

        services.AddMvp24HoursApplicationModule(typeof(TestApplicationServiceAsync).Assembly);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IMapper>().Should().NotBeNull();
        provider.GetRequiredService<IValidationService<AppTestEntity>>().Should().NotBeNull();
        provider.GetRequiredService<ITransactionScopeFactory>().Should().NotBeNull();
        provider.GetRequiredService<IExceptionToResultMapper>().Should().NotBeNull();
        provider.GetRequiredService<PaginationOptions>().Should().NotBeNull();
        services.Should().Contain(d =>
            d.ServiceType == typeof(IApplicationServiceAsync<AppTestEntity>) &&
            d.ImplementationType == typeof(TestApplicationServiceAsync));
    }

    [Fact]
    public void AddMvp24HoursApplicationMinimal_ShouldRegisterOnlyMapperAndApplicationServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IUnitOfWorkAsync, MockUnitOfWorkAsync>();

        services.AddMvp24HoursApplicationMinimal(typeof(TestApplicationServiceAsync).Assembly);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IMapper>().Should().NotBeNull();
        services.Should().Contain(d =>
            d.ServiceType == typeof(IApplicationServiceAsync<AppTestEntity>) &&
            d.ImplementationType == typeof(TestApplicationServiceAsync));
        provider.GetService<IValidationService<AppTestEntity>>().Should().BeNull();
        provider.GetService<ITransactionScopeFactory>().Should().BeNull();
    }

    [Fact]
    public void AddMvp24HoursApplicationForApi_ShouldEnableValidationTransactionsAndPagination()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IUnitOfWorkAsync, MockUnitOfWorkAsync>();
        services.AddSingleton<IUnitOfWork, MockUnitOfWork>();

        services.AddMvp24HoursApplicationForApi(typeof(TestApplicationServiceAsync).Assembly);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IValidationService<AppTestEntity>>().Should().NotBeNull();
        provider.GetRequiredService<ITransactionScopeFactory>().Should().NotBeNull();
        provider.GetRequiredService<PaginationOptions>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursApplicationFull_WithCacheEnabled_ShouldRegisterCacheProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IUnitOfWorkAsync, MockUnitOfWorkAsync>();
        services.AddSingleton<IUnitOfWork, MockUnitOfWork>();
        services.AddDistributedMemoryCache();

        services.AddMvp24HoursApplicationFull(typeof(TestApplicationServiceAsync).Assembly);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IValidationService<AppTestEntity>>().Should().NotBeNull();
        services.Should().Contain(d => d.ServiceType == typeof(IQueryCacheProvider));
    }

    [Fact]
    public void AddMvp24HoursApplicationModuleFromAssemblyContaining_ShouldRegisterServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IUnitOfWorkAsync, MockUnitOfWorkAsync>();
        services.AddSingleton<IUnitOfWork, MockUnitOfWork>();

        services.AddMvp24HoursApplicationModuleFromAssemblyContaining<TestApplicationServiceAsync>();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<TestApplicationServiceAsync>().Should().NotBeNull();
    }
}
