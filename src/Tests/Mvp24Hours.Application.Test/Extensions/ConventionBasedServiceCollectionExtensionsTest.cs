using Mvp24Hours.Application.Extensions;
using Mvp24Hours.Core.Contract.Infrastructure.DependencyInjection;

namespace Mvp24Hours.Application.Test.Extensions;

[Trait("Category", "Unit")]
public class ConventionBasedServiceCollectionExtensionsTest
{
    [Fact]
    public void AddMvp24HoursServicesByConvention_WithNullServices_ShouldThrow()
    {
        Action act = () => ConventionBasedServiceCollectionExtensions
            .AddMvp24HoursServicesByConvention(null!, typeof(ConventionScopedService).Assembly);

        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddMvp24HoursServicesByConvention_WithoutAssemblies_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddMvp24HoursServicesByConvention();

        act.Should().Throw<ArgumentException>().WithParameterName("assemblies");
    }

    [Fact]
    public void AddMvp24HoursServicesByConvention_WithFilterNull_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddMvp24HoursServicesByConvention(
            [typeof(ConventionScopedService).Assembly],
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("filter");
    }

    [Fact]
    public void AddMvp24HoursServicesByConvention_ShouldRegisterMarkerServices()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursServicesByConvention(
            [typeof(ConventionScopedService).Assembly],
            type => type == typeof(ConventionScopedService)
                 || type == typeof(ConventionSingletonService)
                 || type == typeof(ConventionTransientService));
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IConventionScopedService>().Should().BeOfType<ConventionScopedService>();
        provider.GetRequiredService<IConventionSingletonService>().Should().BeOfType<ConventionSingletonService>();
        provider.GetRequiredService<IConventionTransientService>().Should().BeOfType<ConventionTransientService>();
    }

    [Fact]
    public void AddMvp24HoursServicesByConventionFromAssemblyContaining_ShouldAddRegistrations()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursServicesByConventionFromAssemblyContaining<ConventionScopedService>();

        services.Should().Contain(d =>
            d.ServiceType == typeof(IConventionScopedService) &&
            d.ImplementationType == typeof(ConventionScopedService));
    }

    [Fact]
    public void AddMvp24HoursScopedServicesByConvention_ShouldRegisterOnlyScopedServices()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursServicesByConvention(
            [typeof(ConventionScopedService).Assembly],
            type => type == typeof(ConventionScopedService));
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IConventionScopedService>().Should().BeOfType<ConventionScopedService>();
        services.Should().NotContain(d => d.ImplementationType == typeof(ConventionSingletonService));
    }

    [Fact]
    public void AddMvp24HoursSingletonServicesByConvention_ShouldRegisterOnlySingletonServices()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursServicesByConvention(
            [typeof(ConventionSingletonService).Assembly],
            type => type == typeof(ConventionSingletonService));

        services.Should().ContainSingle(d =>
            d.ServiceType == typeof(IConventionSingletonService) &&
            d.ImplementationType == typeof(ConventionSingletonService) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddMvp24HoursTransientServicesByConvention_ShouldRegisterOnlyTransientServices()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursServicesByConvention(
            [typeof(ConventionTransientService).Assembly],
            type => type == typeof(ConventionTransientService));

        services.Should().ContainSingle(d =>
            d.ServiceType == typeof(IConventionTransientService) &&
            d.ImplementationType == typeof(ConventionTransientService) &&
            d.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void AddMvp24HoursServicesByConvention_WithFilter_ShouldRegisterMatchingTypesOnly()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursServicesByConvention(
            [typeof(ConventionScopedService).Assembly],
            type => type == typeof(ConventionScopedService));

        services.Should().ContainSingle(d =>
            d.ServiceType == typeof(IConventionScopedService) &&
            d.ImplementationType == typeof(ConventionScopedService));
    }

    [Fact]
    public void AddMvp24HoursServicesByConvention_ShouldIgnoreServiceIgnoreAttribute()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursServicesByConvention(typeof(IgnoredConventionService).Assembly);

        services.Should().NotContain(d => d.ImplementationType == typeof(IgnoredConventionService));
    }

    [Fact]
    public void AddMvp24HoursServicesByConvention_WithServiceReplace_ShouldReplaceExistingRegistration()
    {
        var services = new ServiceCollection();
        services.AddScoped<IConventionReplaceService, ConventionReplaceServiceDefault>();

        services.AddMvp24HoursServicesByConvention(
            [typeof(ConventionReplaceServiceReplacement).Assembly],
            type => type == typeof(ConventionReplaceServiceReplacement));
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IConventionReplaceService>()
            .Should().BeOfType<ConventionReplaceServiceReplacement>();
    }

    [Fact]
    public void AddMvp24HoursServicesByConvention_WithServiceTryAdd_ShouldNotReplaceExistingRegistration()
    {
        var services = new ServiceCollection();
        services.AddScoped<IConventionTryAddService, ConventionTryAddServicePrimary>();

        services.AddMvp24HoursServicesByConvention(
            [typeof(ConventionTryAddServiceFallback).Assembly],
            type => type == typeof(ConventionTryAddServiceFallback));
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IConventionTryAddService>()
            .Should().BeOfType<ConventionTryAddServicePrimary>();
    }

    [Fact]
    public void AddMvp24HoursServicesByConvention_WithKeyedService_ShouldRegisterKeyedDescriptor()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursServicesByConvention(
            [typeof(KeyedConventionService).Assembly],
            type => type == typeof(KeyedConventionService));

        services.Should().Contain(d =>
            d.ServiceType == typeof(IConventionKeyedService) &&
            d.KeyedImplementationType == typeof(KeyedConventionService) &&
            d.IsKeyedService &&
            Equals(d.ServiceKey, "primary"));
    }

    [Fact]
    public void AddMvp24HoursServicesByConvention_WithSelfRegistering_ShouldRegisterConcreteType()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursServicesByConvention(
            [typeof(SelfRegisteringConventionService).Assembly],
            type => type == typeof(SelfRegisteringConventionService));

        services.Should().Contain(d =>
            d.ServiceType == typeof(SelfRegisteringConventionService) &&
            d.ImplementationType == typeof(SelfRegisteringConventionService));
    }

    [Fact]
    public void GetConventionRegistrations_ShouldReturnDiscoveredServices()
    {
        IEnumerable<ConventionServiceRegistration> registrations =
            ConventionBasedServiceCollectionExtensions.GetConventionRegistrations(typeof(ConventionScopedService).Assembly);

        registrations.Should().Contain(r =>
            r.ServiceType == typeof(IConventionScopedService) &&
            r.ImplementationType == typeof(ConventionScopedService) &&
            r.Lifetime == ServiceLifetime.Scoped &&
            r.IsKeyedService == false);
        registrations.Should().NotContain(r => r.ImplementationType == typeof(IgnoredConventionService));
    }

    [Fact]
    public void AddMvp24HoursScopedServicesByConvention_ShouldRegisterOnlyScopedMarkerServices()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursScopedServicesByConvention(typeof(ConventionScopedService).Assembly);

        services.Should().Contain(d =>
            d.ServiceType == typeof(IConventionScopedService) &&
            d.ImplementationType == typeof(ConventionScopedService));
        services.Should().NotContain(d => d.ImplementationType == typeof(ConventionSingletonService));
    }

    [Fact]
    public void AddMvp24HoursSingletonServicesByConvention_ShouldRegisterOnlySingletonMarkerServices()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursSingletonServicesByConvention(typeof(ConventionSingletonService).Assembly);

        services.Should().ContainSingle(d =>
            d.ServiceType == typeof(IConventionSingletonService) &&
            d.ImplementationType == typeof(ConventionSingletonService) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddMvp24HoursTransientServicesByConvention_ShouldRegisterOnlyTransientMarkerServices()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursTransientServicesByConvention(typeof(ConventionTransientService).Assembly);

        services.Should().ContainSingle(d =>
            d.ServiceType == typeof(IConventionTransientService) &&
            d.ImplementationType == typeof(ConventionTransientService) &&
            d.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void AddMvp24HoursServicesByConvention_WithServiceOrder_ShouldRegisterInOrder()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursServicesByConvention(
            [typeof(OrderedConventionServiceFirst).Assembly],
            type => type == typeof(OrderedConventionServiceFirst) || type == typeof(OrderedConventionServiceSecond));

        List<Type?> implementationTypes = services
            .Where(d => d.ImplementationType == typeof(OrderedConventionServiceFirst)
                     || d.ImplementationType == typeof(OrderedConventionServiceSecond))
            .Select(d => d.ImplementationType)
            .ToList();

        implementationTypes[0].Should().Be(typeof(OrderedConventionServiceFirst));
        implementationTypes[1].Should().Be(typeof(OrderedConventionServiceSecond));
    }

    public interface IConventionScopedService;

    public sealed class ConventionScopedService : IConventionScopedService, IScopedService;

    public interface IConventionSingletonService;

    public sealed class ConventionSingletonService : IConventionSingletonService, ISingletonService;

    public interface IConventionTransientService;

    public sealed class ConventionTransientService : IConventionTransientService, ITransientService;

    public interface IConventionReplaceService;

    public sealed class ConventionReplaceServiceDefault : IConventionReplaceService, IScopedService;

    [ServiceReplace]
    public sealed class ConventionReplaceServiceReplacement : IConventionReplaceService, IScopedService;

    public interface IConventionTryAddService;

    public sealed class ConventionTryAddServicePrimary : IConventionTryAddService, IScopedService;

    [ServiceTryAdd]
    public sealed class ConventionTryAddServiceFallback : IConventionTryAddService, IScopedService;

    public interface IConventionKeyedService;

    [Core.Contract.Infrastructure.DependencyInjection.ServiceKey("primary")]
    public sealed class KeyedConventionService : IConventionKeyedService, IScopedService, IKeyedService;

    public sealed class SelfRegisteringConventionService : ISelfRegistering, IScopedService;

    [ServiceIgnore]
    public sealed class IgnoredConventionService : IConventionScopedService, IScopedService;

    public interface IOrderedConventionServiceFirst;

    [ServiceOrder(1)]
    public sealed class OrderedConventionServiceFirst : IOrderedConventionServiceFirst, IScopedService;

    public interface IOrderedConventionServiceSecond;

    [ServiceOrder(2)]
    public sealed class OrderedConventionServiceSecond : IOrderedConventionServiceSecond, IScopedService;
}
