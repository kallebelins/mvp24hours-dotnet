//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Extensions.KeyedServices;
using System;
using System.Linq;
using Xunit;

namespace Mvp24Hours.Core.Test;

/// <summary>
/// Unit tests for Keyed Services extensions.
/// Tests the .NET 8+ Keyed Services functionality wrapper.
/// </summary>
public class KeyedServicesTest
{
    #region Test Interfaces and Implementations

    public interface ITestService
    {
        string GetName();
    }

    public class ServiceA : ITestService
    {
        public string GetName() => "ServiceA";
    }

    public class ServiceB : ITestService
    {
        public string GetName() => "ServiceB";
    }

    public class ServiceC : ITestService
    {
        public string GetName() => "ServiceC";
    }

    public interface ITestDependency
    {
        string GetValue();
    }

    public class TestDependency : ITestDependency
    {
        public string GetValue() => "dependency-value";
    }

    public class ServiceWithDependency : ITestService
    {
        private readonly ITestDependency _dependency;

        public ServiceWithDependency(ITestDependency dependency)
        {
            _dependency = dependency;
        }

        public string GetName() => $"ServiceWithDependency:{_dependency.GetValue()}";
    }

    [KeyedService("test:attributed", typeof(ITestService))]
    public class AttributedService : ITestService
    {
        public string GetName() => "AttributedService";
    }

    [KeyedService("test:scoped", typeof(ITestService), Lifetime = ServiceLifetime.Scoped)]
    public class ScopedAttributedService : ITestService
    {
        public string GetName() => "ScopedAttributedService";
    }

    #endregion

    #region ServiceKeys Tests

    [Fact]
    public void ServiceKeys_FileStorage_HasCorrectValues()
    {
        // Assert
        Assert.Equal("FileStorage:Local", ServiceKeys.FileStorage.Local);
        Assert.Equal("FileStorage:InMemory", ServiceKeys.FileStorage.InMemory);
        Assert.Equal("FileStorage:Azure", ServiceKeys.FileStorage.Azure);
        Assert.Equal("FileStorage:AwsS3", ServiceKeys.FileStorage.AwsS3);
        Assert.Equal("FileStorage:Default", ServiceKeys.FileStorage.Default);
    }

    [Fact]
    public void ServiceKeys_Email_HasCorrectValues()
    {
        // Assert
        Assert.Equal("Email:Smtp", ServiceKeys.Email.Smtp);
        Assert.Equal("Email:SendGrid", ServiceKeys.Email.SendGrid);
        Assert.Equal("Email:Azure", ServiceKeys.Email.Azure);
        Assert.Equal("Email:InMemory", ServiceKeys.Email.InMemory);
        Assert.Equal("Email:Default", ServiceKeys.Email.Default);
    }

    [Fact]
    public void ServiceKeys_Cache_HasCorrectValues()
    {
        // Assert
        Assert.Equal("Cache:Memory", ServiceKeys.Cache.Memory);
        Assert.Equal("Cache:Distributed", ServiceKeys.Cache.Distributed);
        Assert.Equal("Cache:Hybrid", ServiceKeys.Cache.Hybrid);
        Assert.Equal("Cache:Redis", ServiceKeys.Cache.Redis);
        Assert.Equal("Cache:Default", ServiceKeys.Cache.Default);
    }

    [Fact]
    public void ServiceKeys_Database_HasCorrectValues()
    {
        // Assert
        Assert.Equal("Database:ReadOnly", ServiceKeys.Database.ReadOnly);
        Assert.Equal("Database:ReadWrite", ServiceKeys.Database.ReadWrite);
        Assert.Equal("Database:Primary", ServiceKeys.Database.Primary);
        Assert.Equal("Database:Replica", ServiceKeys.Database.Replica);
        Assert.Equal("Database:Default", ServiceKeys.Database.Default);
    }

    [Fact]
    public void ServiceKeys_Tenant_ForTenant_CreatesCorrectKey()
    {
        // Arrange
        var tenantId = "tenant-123";
        var category = "Database";

        // Act
        var key = ServiceKeys.Tenant.ForTenant(category, tenantId);

        // Assert
        Assert.Equal("Tenant:tenant-123:Database", key);
    }

    [Fact]
    public void ServiceKeys_Tenant_DatabaseForTenant_CreatesCorrectKey()
    {
        // Arrange
        var tenantId = "tenant-456";

        // Act
        var key = ServiceKeys.Tenant.DatabaseForTenant(tenantId);

        // Assert
        Assert.Equal("Tenant:tenant-456:Database", key);
    }

    [Fact]
    public void ServiceKeys_Tenant_CacheForTenant_CreatesCorrectKey()
    {
        // Arrange
        var tenantId = "tenant-789";

        // Act
        var key = ServiceKeys.Tenant.CacheForTenant(tenantId);

        // Assert
        Assert.Equal("Tenant:tenant-789:Cache", key);
    }

    #endregion

    #region AddKeyedServices Tests

    [Fact]
    public void AddKeyedServices_RegistersMultipleImplementations()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddKeyedServices<ITestService>(config =>
        {
            config.AddKeyed<ServiceA>("key:a");
            config.AddKeyed<ServiceB>("key:b");
            config.AddKeyed<ServiceC>("key:c");
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var serviceA = provider.GetRequiredKeyedService<ITestService>("key:a");
        var serviceB = provider.GetRequiredKeyedService<ITestService>("key:b");
        var serviceC = provider.GetRequiredKeyedService<ITestService>("key:c");

        Assert.Equal("ServiceA", serviceA.GetName());
        Assert.Equal("ServiceB", serviceB.GetName());
        Assert.Equal("ServiceC", serviceC.GetName());
    }

    [Fact]
    public void AddKeyedServices_WithFactory_ResolvesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ITestDependency, TestDependency>();

        // Act
        services.AddKeyedServices<ITestService>(config =>
        {
            config.AddKeyed("key:factory", (sp, _) =>
            {
                var dep = sp.GetRequiredService<ITestDependency>();
                return new ServiceWithDependency(dep);
            });
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var service = provider.GetRequiredKeyedService<ITestService>("key:factory");
        Assert.Equal("ServiceWithDependency:dependency-value", service.GetName());
    }

    [Fact]
    public void AddKeyedServices_WithDefault_ResolvesDefault()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddKeyedServices<ITestService>(config =>
        {
            config.AddKeyed<ServiceA>("key:a");
            config.AddKeyed<ServiceB>("key:b");
            config.SetDefault("key:a");
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var defaultService = provider.GetRequiredService<ITestService>();
        Assert.Equal("ServiceA", defaultService.GetName());
    }

    [Fact]
    public void AddKeyedServices_WithDifferentLifetimes_RegistersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddKeyedServices<ITestService>(config =>
        {
            config.AddKeyed<ServiceA>("key:singleton", ServiceLifetime.Singleton);
            config.AddKeyed<ServiceB>("key:scoped", ServiceLifetime.Scoped);
            config.AddKeyed<ServiceC>("key:transient", ServiceLifetime.Transient);
        });

        var provider = services.BuildServiceProvider();

        // Assert - Singleton returns same instance
        var singleton1 = provider.GetRequiredKeyedService<ITestService>("key:singleton");
        var singleton2 = provider.GetRequiredKeyedService<ITestService>("key:singleton");
        Assert.Same(singleton1, singleton2);

        // Assert - Transient returns different instances
        var transient1 = provider.GetRequiredKeyedService<ITestService>("key:transient");
        var transient2 = provider.GetRequiredKeyedService<ITestService>("key:transient");
        Assert.NotSame(transient1, transient2);
    }

    #endregion

    #region Individual Registration Tests

    [Fact]
    public void AddKeyedSingletonService_RegistersAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddKeyedSingletonService<ITestService, ServiceA>("key:singleton");
        var provider = services.BuildServiceProvider();

        // Assert
        var service1 = provider.GetRequiredKeyedService<ITestService>("key:singleton");
        var service2 = provider.GetRequiredKeyedService<ITestService>("key:singleton");
        Assert.Same(service1, service2);
    }

    [Fact]
    public void AddKeyedScopedService_RegistersAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddKeyedScopedService<ITestService, ServiceA>("key:scoped");
        var provider = services.BuildServiceProvider();

        // Assert - Same scope = same instance
        using var scope1 = provider.CreateScope();
        var service1 = scope1.ServiceProvider.GetRequiredKeyedService<ITestService>("key:scoped");
        var service2 = scope1.ServiceProvider.GetRequiredKeyedService<ITestService>("key:scoped");
        Assert.Same(service1, service2);

        // Different scope = different instance
        using var scope2 = provider.CreateScope();
        var service3 = scope2.ServiceProvider.GetRequiredKeyedService<ITestService>("key:scoped");
        Assert.NotSame(service1, service3);
    }

    [Fact]
    public void AddKeyedTransientService_RegistersAsTransient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddKeyedTransientService<ITestService, ServiceA>("key:transient");
        var provider = services.BuildServiceProvider();

        // Assert
        var service1 = provider.GetRequiredKeyedService<ITestService>("key:transient");
        var service2 = provider.GetRequiredKeyedService<ITestService>("key:transient");
        Assert.NotSame(service1, service2);
    }

    [Fact]
    public void AddKeyedSingletonService_WithFactory_Works()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddKeyedSingletonService<ITestService>("key:factory", (sp, key) => new ServiceA());
        var provider = services.BuildServiceProvider();

        // Assert
        var service = provider.GetRequiredKeyedService<ITestService>("key:factory");
        Assert.Equal("ServiceA", service.GetName());
    }

    #endregion

    #region SetDefaultKeyedService Tests

    [Fact]
    public void SetDefaultKeyedService_MakesKeyedServiceAvailableWithoutKey()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKeyedSingleton<ITestService, ServiceA>("key:a");
        services.AddKeyedSingleton<ITestService, ServiceB>("key:b");

        // Act
        services.SetDefaultKeyedService<ITestService>("key:b");
        var provider = services.BuildServiceProvider();

        // Assert
        var defaultService = provider.GetRequiredService<ITestService>();
        Assert.Equal("ServiceB", defaultService.GetName());
    }

    #endregion

    #region TryAddKeyed Tests

    [Fact]
    public void TryAddKeyedSingletonService_DoesNotOverwrite()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKeyedSingleton<ITestService, ServiceA>("key:test");

        // Act
        services.TryAddKeyedSingletonService<ITestService, ServiceB>("key:test");
        var provider = services.BuildServiceProvider();

        // Assert - Should still be ServiceA
        var service = provider.GetRequiredKeyedService<ITestService>("key:test");
        Assert.Equal("ServiceA", service.GetName());
    }

    [Fact]
    public void TryAddKeyedSingletonService_AddsIfNotExists()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.TryAddKeyedSingletonService<ITestService, ServiceA>("key:new");
        var provider = services.BuildServiceProvider();

        // Assert
        var service = provider.GetRequiredKeyedService<ITestService>("key:new");
        Assert.Equal("ServiceA", service.GetName());
    }

    #endregion

    #region Resolution Extensions Tests

    [Fact]
    public void GetKeyedServiceOrDefault_ReturnsNullWhenNotFound()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        // Act
        var service = provider.GetKeyedServiceOrDefault<ITestService>("nonexistent");

        // Assert
        Assert.Null(service);
    }

    [Fact]
    public void GetKeyedServiceOrFallback_ReturnsFallbackWhenNotFound()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        // Act
        var service = provider.GetKeyedServiceOrFallback<ITestService>(
            "nonexistent",
            _ => new ServiceA());

        // Assert
        Assert.Equal("ServiceA", service.GetName());
    }

    [Fact]
    public void GetKeyedServiceOrFallback_ReturnsServiceWhenFound()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKeyedSingleton<ITestService, ServiceB>("key:exists");
        var provider = services.BuildServiceProvider();

        // Act
        var service = provider.GetKeyedServiceOrFallback<ITestService>(
            "key:exists",
            _ => new ServiceA());

        // Assert
        Assert.Equal("ServiceB", service.GetName());
    }

    [Fact]
    public void HasKeyedService_ReturnsTrueWhenExists()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKeyedSingleton<ITestService, ServiceA>("key:exists");

        // Act & Assert
        Assert.True(services.HasKeyedService<ITestService>("key:exists"));
    }

    [Fact]
    public void HasKeyedService_ReturnsFalseWhenNotExists()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.False(services.HasKeyedService<ITestService>("key:nonexistent"));
    }

    [Fact]
    public void GetKeyedServiceKeys_ReturnsAllKeys()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKeyedSingleton<ITestService, ServiceA>("key:a");
        services.AddKeyedSingleton<ITestService, ServiceB>("key:b");
        services.AddKeyedSingleton<ITestService, ServiceC>("key:c");

        // Act
        var keys = services.GetKeyedServiceKeys<ITestService>().ToList();

        // Assert
        Assert.Contains("key:a", keys);
        Assert.Contains("key:b", keys);
        Assert.Contains("key:c", keys);
        Assert.Equal(3, keys.Count);
    }

    #endregion

    #region Multi-Tenant Tests

    [Fact]
    public void AddTenantKeyedService_RegistersTenantSpecificService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTenantKeyedService<ITestService, ServiceA>("tenant-1", "Database");
        services.AddTenantKeyedService<ITestService, ServiceB>("tenant-2", "Database");
        var provider = services.BuildServiceProvider();

        // Assert
        var tenant1Service = provider.GetTenantService<ITestService>("tenant-1", "Database");
        var tenant2Service = provider.GetTenantService<ITestService>("tenant-2", "Database");

        Assert.Equal("ServiceA", tenant1Service.GetName());
        Assert.Equal("ServiceB", tenant2Service.GetName());
    }

    [Fact]
    public void GetTenantServiceOrDefault_FallsBackToDefault()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKeyedSingleton<ITestService, ServiceA>(ServiceKeys.Database.Default);
        var provider = services.BuildServiceProvider();

        // Act - tenant-unknown doesn't have a specific service
        var service = provider.GetTenantServiceOrDefault<ITestService>(
            "tenant-unknown",
            "Database",
            ServiceKeys.Database.Default);

        // Assert - Falls back to default
        Assert.Equal("ServiceA", service.GetName());
    }

    [Fact]
    public void GetTenantServiceOrDefault_ReturnsTenantSpecificWhenExists()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKeyedSingleton<ITestService, ServiceA>(ServiceKeys.Database.Default);
        services.AddTenantKeyedService<ITestService, ServiceB>("tenant-1", "Database");
        var provider = services.BuildServiceProvider();

        // Act
        var service = provider.GetTenantServiceOrDefault<ITestService>(
            "tenant-1",
            "Database",
            ServiceKeys.Database.Default);

        // Assert - Returns tenant-specific
        Assert.Equal("ServiceB", service.GetName());
    }

    #endregion

    #region Assembly Scanning Tests

    [Fact]
    public void AddKeyedServicesFromAssembly_RegistersAttributedServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddKeyedServicesFromAssembly(typeof(KeyedServicesTest).Assembly);
        var provider = services.BuildServiceProvider();

        // Assert
        var attributedService = provider.GetRequiredKeyedService<ITestService>("test:attributed");
        Assert.Equal("AttributedService", attributedService.GetName());
    }

    [Fact]
    public void AddKeyedServicesFromAssembly_RespectsLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddKeyedServicesFromAssembly(typeof(KeyedServicesTest).Assembly);
        var provider = services.BuildServiceProvider();

        // Assert - Scoped service should have different instances in different scopes
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var service1 = scope1.ServiceProvider.GetRequiredKeyedService<ITestService>("test:scoped");
        var service2 = scope2.ServiceProvider.GetRequiredKeyedService<ITestService>("test:scoped");

        Assert.NotSame(service1, service2);
    }

    [Fact]
    public void AddKeyedServicesFromAssemblyContaining_Works()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddKeyedServicesFromAssemblyContaining<KeyedServicesTest>();
        var provider = services.BuildServiceProvider();

        // Assert
        var service = provider.GetRequiredKeyedService<ITestService>("test:attributed");
        Assert.NotNull(service);
    }

    #endregion

    #region Replace and Remove Tests

    [Fact]
    public void ReplaceKeyedService_ReplacesExistingService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKeyedSingleton<ITestService, ServiceA>("key:test");

        // Act
        services.ReplaceKeyedService<ITestService, ServiceB>("key:test");
        var provider = services.BuildServiceProvider();

        // Assert
        var service = provider.GetRequiredKeyedService<ITestService>("key:test");
        Assert.Equal("ServiceB", service.GetName());
    }

    [Fact]
    public void ReplaceKeyedService_AddsIfNotExists()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.ReplaceKeyedService<ITestService, ServiceB>("key:new");
        var provider = services.BuildServiceProvider();

        // Assert
        var service = provider.GetRequiredKeyedService<ITestService>("key:new");
        Assert.Equal("ServiceB", service.GetName());
    }

    [Fact]
    public void RemoveKeyedService_RemovesService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKeyedSingleton<ITestService, ServiceA>("key:toremove");

        // Act
        services.RemoveKeyedService<ITestService>("key:toremove");
        var provider = services.BuildServiceProvider();

        // Assert
        var service = provider.GetKeyedService<ITestService>("key:toremove");
        Assert.Null(service);
    }

    [Fact]
    public void RemoveKeyedService_DoesNothingIfNotExists()
    {
        // Arrange
        var services = new ServiceCollection();
        var initialCount = services.Count;

        // Act - Should not throw
        services.RemoveKeyedService<ITestService>("key:nonexistent");

        // Assert
        Assert.Equal(initialCount, services.Count);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void AddKeyedServices_ThrowsOnNullServices()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            services!.AddKeyedServices<ITestService>(_ => { }));
    }

    [Fact]
    public void AddKeyedServices_ThrowsOnNullConfigure()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            services.AddKeyedServices<ITestService>(null!));
    }

    [Fact]
    public void KeyedServiceAttribute_ThrowsOnNullKey()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new KeyedServiceAttribute(null!));
    }

    [Fact]
    public void KeyedServiceAttribute_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var attr = new KeyedServiceAttribute("test:key", typeof(ITestService))
        {
            Lifetime = ServiceLifetime.Scoped
        };

        // Assert
        Assert.Equal("test:key", attr.Key);
        Assert.Equal(typeof(ITestService), attr.ServiceType);
        Assert.Equal(ServiceLifetime.Scoped, attr.Lifetime);
    }

    #endregion
}

