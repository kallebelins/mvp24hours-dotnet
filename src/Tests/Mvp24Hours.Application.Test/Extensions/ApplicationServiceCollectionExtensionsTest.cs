//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using AutoMapper;
using Mvp24Hours.Application.Extensions;
using Mvp24Hours.Application.Test.Support;
using Mvp24Hours.Core.Contract.Logic;

namespace Mvp24Hours.Application.Test.Extensions;

[Trait("Category", "Unit")]
public class ApplicationServiceCollectionExtensionsTest
{
    [Fact]
    public void AddMvp24HoursAutoMapper_WithoutAssemblies_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddMvp24HoursAutoMapper();

        act.Should().Throw<ArgumentException>()
            .WithParameterName("assemblies");
    }

    [Fact]
    public void AddMvp24HoursAutoMapper_ShouldRegisterIMapper()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursAutoMapper(typeof(TestAutoMapperProfile).Assembly);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IMapper>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursAutoMapperFromAssemblyContaining_ShouldRegisterIMapper()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursAutoMapperFromAssemblyContaining<TestAutoMapperProfile>();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IMapper>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursApplicationServices_WithoutAssemblies_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddMvp24HoursApplicationServices();

        act.Should().Throw<ArgumentException>()
            .WithParameterName("assemblies");
    }

    [Fact]
    public void AddMvp24HoursApplicationService_ShouldRegisterService()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IUnitOfWorkAsync, MockUnitOfWorkAsync>();

        services.AddMvp24HoursApplicationService<IApplicationServiceAsync<AppTestEntity>, TestApplicationServiceAsync>();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IApplicationServiceAsync<AppTestEntity>>()
            .Should().BeOfType<TestApplicationServiceAsync>();
    }

    [Fact]
    public void AddMvp24HoursApplicationServicesFromAssemblyContaining_ShouldRegisterScannedServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IUnitOfWorkAsync, MockUnitOfWorkAsync>();

        services.AddMvp24HoursApplicationServicesFromAssemblyContaining<TestApplicationServiceAsync>();

        services.Should().Contain(d =>
            d.ServiceType == typeof(IApplicationServiceAsync<AppTestEntity>) &&
            d.ImplementationType == typeof(TestApplicationServiceAsync));
        services.Should().Contain(d =>
            d.ServiceType == typeof(TestApplicationServiceAsync) &&
            d.ImplementationType == typeof(TestApplicationServiceAsync));
    }

    [Fact]
    public void AddMvp24HoursApplicationServices_ShouldRegisterByInterfaceAndConcreteType()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IUnitOfWorkAsync, MockUnitOfWorkAsync>();

        services.AddMvp24HoursApplicationServices(typeof(TestApplicationServiceAsync).Assembly);

        services.Should().Contain(d =>
            d.ServiceType == typeof(IApplicationServiceAsync<AppTestEntity>) &&
            d.ImplementationType == typeof(TestApplicationServiceAsync));
        services.Should().Contain(d =>
            d.ServiceType == typeof(TestApplicationServiceAsync) &&
            d.ImplementationType == typeof(TestApplicationServiceAsync));
    }

    [Fact]
    public void AddMvp24HoursValidators_ShouldRegisterFluentValidators()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursValidators(typeof(AppTestEntityValidator).Assembly);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IValidator<AppTestEntity>>().Should().BeOfType<AppTestEntityValidator>();
    }

    [Fact]
    public void AddMvp24HoursValidators_WithoutAssemblies_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddMvp24HoursValidators();

        act.Should().Throw<ArgumentException>()
            .WithParameterName("assemblies");
    }

    [Fact]
    public void AddMvp24HoursApplication_WithoutAssemblies_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddMvp24HoursApplication();

        act.Should().Throw<ArgumentException>()
            .WithParameterName("assemblies");
    }

    [Fact]
    public void AddMvp24HoursApplication_ShouldRegisterMapperAndServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IUnitOfWorkAsync, MockUnitOfWorkAsync>();

        services.AddMvp24HoursApplication(typeof(TestApplicationServiceAsync).Assembly);

        services.Should().Contain(d => d.ServiceType == typeof(IMapper));
        services.Should().Contain(d =>
            d.ServiceType == typeof(IApplicationServiceAsync<AppTestEntity>) &&
            d.ImplementationType == typeof(TestApplicationServiceAsync));
    }

    [Fact]
    public void AddMvp24HoursApplicationFromAssemblyContaining_ShouldRegisterServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IUnitOfWorkAsync, MockUnitOfWorkAsync>();

        services.AddMvp24HoursApplicationFromAssemblyContaining<TestApplicationServiceAsync>();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<TestApplicationServiceAsync>().Should().NotBeNull();
    }
}
