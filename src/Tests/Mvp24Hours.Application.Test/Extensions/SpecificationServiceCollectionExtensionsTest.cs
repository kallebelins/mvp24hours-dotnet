//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Extensions;
using Mvp24Hours.Core.Contract.Domain.Specifications;
using Mvp24Hours.Core.Domain.Specifications;

namespace Mvp24Hours.Application.Test.Extensions;

[Trait("Category", "Unit")]
public class SpecificationServiceCollectionExtensionsTest
{
    public sealed class CustomCustomerEvaluator : ISpecificationEvaluator<Customer>
    {
        public IQueryable<Customer> GetQuery(IQueryable<Customer> inputQuery, ISpecificationQuery<Customer> specification)
        {
            return inputQuery;
        }

        public IQueryable<Customer> GetQuery(IQueryable<Customer> inputQuery, Specification<Customer> specification)
        {
            return inputQuery;
        }
    }

    public sealed class Customer
    {
        public int Id { get; set; }
    }

    [Fact]
    public void AddMvp24HoursInMemorySpecificationEvaluator_ShouldRegisterGenericAndNonGeneric()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursInMemorySpecificationEvaluator();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ISpecificationEvaluator<Customer>>()
            .Should().BeOfType<InMemorySpecificationEvaluator<Customer>>();
        provider.GetRequiredService<ISpecificationEvaluator>()
            .Should().BeOfType<InMemorySpecificationEvaluator>();
    }

    [Fact]
    public void AddMvp24HoursInMemorySpecificationEvaluator_WithCustomLifetime_ShouldRegisterWithThatLifetime()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursInMemorySpecificationEvaluator(ServiceLifetime.Singleton);

        services.Should().Contain(d =>
            d.ServiceType == typeof(ISpecificationEvaluator<>) &&
            d.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(d =>
            d.ServiceType == typeof(ISpecificationEvaluator) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddMvp24HoursInMemorySpecificationEvaluatorSingleton_ShouldRegisterDefaultInstance()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursInMemorySpecificationEvaluatorSingleton();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ISpecificationEvaluator>()
            .Should().BeSameAs(InMemorySpecificationEvaluator.Default);
    }

    [Fact]
    public void AddMvp24HoursSpecificationEvaluator_WithClosedGenericImplementation_ShouldRegisterMatchingInterface()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursSpecificationEvaluator<CustomCustomerEvaluator>();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ISpecificationEvaluator<Customer>>()
            .Should().BeOfType<CustomCustomerEvaluator>();
    }

    [Fact]
    public void AddMvp24HoursSpecificationEvaluator_WithFactory_ShouldUseFactory()
    {
        var services = new ServiceCollection();
        var instance = new CustomCustomerEvaluator();

        services.AddMvp24HoursSpecificationEvaluator<Customer>(_ => instance);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ISpecificationEvaluator<Customer>>().Should().BeSameAs(instance);
    }

    [Fact]
    public void AddMvp24HoursSpecificationEvaluator_WithFactoryAndLifetime_ShouldHonorLifetime()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursSpecificationEvaluator<Customer>(
            _ => new CustomCustomerEvaluator(),
            ServiceLifetime.Singleton);

        services.Should().Contain(d =>
            d.ServiceType == typeof(ISpecificationEvaluator<Customer>) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddMvp24HoursSpecificationEvaluatorInstance_ShouldRegisterSingletonInstance()
    {
        var services = new ServiceCollection();
        var instance = new CustomCustomerEvaluator();

        services.AddMvp24HoursSpecificationEvaluatorInstance(instance);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ISpecificationEvaluator<Customer>>().Should().BeSameAs(instance);
    }

    [Fact]
    public void AddMvp24HoursSpecificationEvaluatorInstance_WithNullInstance_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddMvp24HoursSpecificationEvaluatorInstance<Customer>(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("instance");
    }

    [Fact]
    public void AddMvp24HoursSpecificationPattern_WithDefaultParameter_ShouldRegisterInMemoryEvaluator()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursSpecificationPattern();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ISpecificationEvaluator<Customer>>()
            .Should().BeOfType<InMemorySpecificationEvaluator<Customer>>();
    }

    [Fact]
    public void AddMvp24HoursSpecificationPattern_WithUseInMemoryEvaluatorFalse_ShouldNotRegisterInMemoryEvaluator()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursSpecificationPattern(useInMemoryEvaluator: false);

        services.Should().NotContain(d => d.ServiceType == typeof(ISpecificationEvaluator<>));
        services.Should().NotContain(d => d.ServiceType == typeof(ISpecificationEvaluator));
    }
}
