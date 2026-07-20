//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.Redis.Test.Support;
using Mvp24Hours.Application.Redis.Test.Support.Entities;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Helpers;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Caching;
using Xunit;
using Xunit.Priority;

namespace Mvp24Hours.Application.Redis.Test;

[Collection(RedisIntegrationCollection.Name)]
[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
[Trait("Category", "Integration")]
public class Test1CacheTest(RedisIntegrationFixture fixture)
{
    private readonly string keyString = $"stringtest-{StringHelper.GenerateKey(5)}";
    private readonly string keyObject = $"objecttest-{StringHelper.GenerateKey(5)}";

    private IServiceProvider Setup()
    {
        var services = new ServiceCollection();
        services.AddScoped<IRepositoryCache<Customer>, RepositoryCache<Customer>>();
        services.AddScoped<IRepositoryCacheAsync<Customer>, RepositoryCacheAsync<Customer>>();
        services.AddMvp24HoursCaching();
        services.AddMvp24HoursCachingRedis(fixture.ConnectionString);
        return services.BuildServiceProvider();
    }

    [DockerFact, Priority(1)]
    public void SetString()
    {
        IServiceProvider serviceProvider = Setup();
        IDistributedCache cache = serviceProvider.GetRequiredService<IDistributedCache>();
        var customer = new Customer
        {
            Oid = Guid.NewGuid(),
            Created = DateTime.Now,
            Name = "Test 1",
            Active = true
        };
        string content = customer.ToSerialize();

        cache.SetString(keyString, content);

        Assert.True(cache.GetString(keyString).HasValue());
    }

    [DockerFact, Priority(2)]
    public void GetString()
    {
        IServiceProvider serviceProvider = Setup();
        IDistributedCache cache = serviceProvider.GetRequiredService<IDistributedCache>();

        cache.SetString(keyString, "Test");
        string? content = cache.GetString(keyString);

        Assert.True(content.HasValue());
    }

    [DockerFact, Priority(3)]
    public void RemoveString()
    {
        IServiceProvider serviceProvider = Setup();
        IDistributedCache cache = serviceProvider.GetRequiredService<IDistributedCache>();

        cache.SetString(keyString, "Test");
        cache.Remove(keyString);

        string? content = cache.GetString(keyString);
        Assert.False(content.HasValue());
    }

    [DockerFact, Priority(4)]
    public void SetObject()
    {
        IServiceProvider serviceProvider = Setup();
        IDistributedCache cache = serviceProvider.GetRequiredService<IDistributedCache>();
        var customer = new Customer
        {
            Oid = Guid.NewGuid(),
            Created = DateTime.Now,
            Name = "Test 1",
            Active = true
        };

        cache.SetObject(keyObject, customer);

        Customer? result = cache.GetObject<Customer>(keyObject);
        Assert.NotNull(result);
    }

    [DockerFact, Priority(5)]
    public void GetObject()
    {
        IServiceProvider serviceProvider = Setup();
        IDistributedCache cache = serviceProvider.GetRequiredService<IDistributedCache>();
        var customer = new Customer
        {
            Oid = Guid.NewGuid(),
            Created = DateTime.Now,
            Name = "Test 1",
            Active = true
        };
        cache.SetObject(keyObject, customer);

        Customer? result = cache.GetObject<Customer>(keyObject);

        Assert.NotNull(result);
    }

    [DockerFact, Priority(6)]
    public void RemoveObject()
    {
        IServiceProvider serviceProvider = Setup();
        IDistributedCache cache = serviceProvider.GetRequiredService<IDistributedCache>();
        var customer = new Customer
        {
            Oid = Guid.NewGuid(),
            Created = DateTime.Now,
            Name = "Test 1",
            Active = true
        };
        cache.SetObject(keyObject, customer);

        cache.Remove(keyObject);

        Customer? result = cache.GetObject<Customer>(keyObject);
        Assert.Null(result);
    }
}
