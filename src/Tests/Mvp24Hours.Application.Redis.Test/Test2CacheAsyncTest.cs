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
public class Test2CacheAsyncTest(RedisIntegrationFixture fixture)
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
    public async Task SetStringAsync()
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

        await cache.SetStringAsync(keyString, content);

        string? result = await cache.GetStringAsync(keyString);
        Assert.True(result.HasValue());
    }

    [DockerFact, Priority(2)]
    public async Task GetStringAsync()
    {
        IServiceProvider serviceProvider = Setup();
        IDistributedCache cache = serviceProvider.GetRequiredService<IDistributedCache>();

        await cache.SetStringAsync(keyString, "Test");
        string? content = await cache.GetStringAsync(keyString);

        Assert.True(content.HasValue());
    }

    [DockerFact, Priority(3)]
    public async Task RemoveStringAsync()
    {
        IServiceProvider serviceProvider = Setup();
        IDistributedCache cache = serviceProvider.GetRequiredService<IDistributedCache>();

        await cache.SetStringAsync(keyString, "Test");
        await cache.RemoveAsync(keyString);

        string? content = await cache.GetStringAsync(keyString);
        Assert.False(content.HasValue());
    }

    [DockerFact, Priority(4)]
    public async Task SetObjectAsync()
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

        await cache.SetObjectAsync(keyObject, customer);

        Customer? result = await cache.GetObjectAsync<Customer>(keyObject);
        Assert.NotNull(result);
    }

    [DockerFact, Priority(5)]
    public async Task GetObjectAsync()
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
        await cache.SetObjectAsync(keyObject, customer);

        Customer? result = await cache.GetObjectAsync<Customer>(keyObject);

        Assert.NotNull(result);
    }

    [DockerFact, Priority(6)]
    public async Task RemoveObjectAsync()
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
        await cache.SetObjectAsync(keyObject, customer);

        await cache.RemoveAsync(keyObject);

        Customer? result = await cache.GetObjectAsync<Customer>(keyObject);
        Assert.Null(result);
    }
}
