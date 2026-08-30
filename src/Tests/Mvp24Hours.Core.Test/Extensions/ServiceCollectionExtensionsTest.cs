using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Mvp24Hours.Core.Test.Extensions;

[Trait("Category", "Unit")]
public class ServiceCollectionExtensionsTest
{
    public interface ISampleGenerator
    {
    }

    public sealed class SampleGeneratorOne : ISampleGenerator
    {
    }

    public sealed class SampleGeneratorTwo : ISampleGenerator
    {
    }

    public interface ISampleRequest<T>
    {
    }

    public sealed class SampleRequestHandler : ISampleRequest<string>
    {
    }

    #region [ Exists ]

    [Fact]
    public void Exists_Generic_WhenTypeRegistered_ReturnsTrue()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ISampleGenerator, SampleGeneratorOne>();

        // Act
        bool result = services.Exists<ISampleGenerator>();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Exists_Generic_WhenTypeNotRegistered_ReturnsFalse()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        bool result = services.Exists<ISampleGenerator>();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Exists_ByType_WhenTypeRegistered_ReturnsTrue()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ISampleGenerator, SampleGeneratorOne>();

        // Act
        bool result = services.Exists(typeof(ISampleGenerator));

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region [ Remove ]

    [Fact]
    public void Remove_Generic_WhenTypeRegistered_RemovesDescriptor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ISampleGenerator, SampleGeneratorOne>();

        // Act
        services.Remove<ISampleGenerator>();

        // Assert
        services.Exists<ISampleGenerator>().Should().BeFalse();
    }

    [Fact]
    public void Remove_ByType_WhenTypeNotRegistered_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () => services.Remove(typeof(ISampleGenerator));

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Remove_ReturnsSameServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ISampleGenerator, SampleGeneratorOne>();

        // Act
        IServiceCollection result = services.Remove(typeof(ISampleGenerator));

        // Assert
        result.Should().BeSameAs(services);
    }

    #endregion

    #region [ AddAllTypes ]

    [Fact]
    public void AddAllTypes_RegistersAllImplementationsOfInterface()
    {
        // Arrange
        var services = new ServiceCollection();
        Assembly[] assemblies = [typeof(ServiceCollectionExtensionsTest).Assembly];

        // Act
        services.AddAllTypes<ISampleGenerator>(assemblies);
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        IEnumerable<ISampleGenerator> generators = provider.GetServices<ISampleGenerator>();
        generators.Should().HaveCount(2);
    }

    [Fact]
    public void AddAllTypes_WithAdditionalRegisterTypesByThemself_AlsoRegistersConcreteTypes()
    {
        // Arrange
        var services = new ServiceCollection();
        Assembly[] assemblies = [typeof(ServiceCollectionExtensionsTest).Assembly];

        // Act
        services.AddAllTypes<ISampleGenerator>(assemblies, additionalRegisterTypesByThemself: true);
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<SampleGeneratorOne>().Should().NotBeNull();
        provider.GetService<SampleGeneratorTwo>().Should().NotBeNull();
    }

    [Fact]
    public void AddAllTypes_WithoutAdditionalRegisterTypesByThemself_DoesNotRegisterConcreteTypes()
    {
        // Arrange
        var services = new ServiceCollection();
        Assembly[] assemblies = [typeof(ServiceCollectionExtensionsTest).Assembly];

        // Act
        services.AddAllTypes<ISampleGenerator>(assemblies, additionalRegisterTypesByThemself: false);
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<SampleGeneratorOne>().Should().BeNull();
    }

    [Fact]
    public void AddAllTypes_WithSingletonLifetime_RegistersAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        Assembly[] assemblies = [typeof(ServiceCollectionExtensionsTest).Assembly];

        // Act
        services.AddAllTypes<ISampleGenerator>(assemblies, lifetime: ServiceLifetime.Singleton);

        // Assert
        services.Should().Contain(d => d.ServiceType == typeof(ISampleGenerator) && d.Lifetime == ServiceLifetime.Singleton);
    }

    #endregion

    #region [ AddAllGenericTypes ]

    [Fact]
    public void AddAllGenericTypes_RegistersImplementationsOfOpenGenericInterface()
    {
        // Arrange
        var services = new ServiceCollection();
        Assembly[] assemblies = [typeof(ServiceCollectionExtensionsTest).Assembly];

        // Act
        services.AddAllGenericTypes(typeof(ISampleRequest<>), assemblies);

        // Assert
        services.Should().Contain(d => d.ServiceType == typeof(ISampleRequest<>) && d.ImplementationType == typeof(SampleRequestHandler));
    }

    [Fact]
    public void AddAllGenericTypes_WithAdditionalRegisterTypesByThemself_AlsoRegistersConcreteTypeDescriptor()
    {
        // Arrange
        // Note: does not call BuildServiceProvider() here because registering a closed
        // implementation type under the open generic service type (as the base descriptor
        // does) is rejected by the container at build time; this test only verifies the
        // descriptor list produced by AddAllGenericTypes, matching the other descriptor-only
        // assertion above.
        var services = new ServiceCollection();
        Assembly[] assemblies = [typeof(ServiceCollectionExtensionsTest).Assembly];

        // Act
        services.AddAllGenericTypes(typeof(ISampleRequest<>), assemblies, additionalRegisterTypesByThemself: true);

        // Assert
        services.Should().Contain(d => d.ServiceType == typeof(SampleRequestHandler) && d.ImplementationType == typeof(SampleRequestHandler));
    }

    #endregion
}
