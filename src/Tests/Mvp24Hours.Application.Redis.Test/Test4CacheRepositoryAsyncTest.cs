//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
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
public class Test4CacheRepositoryAsyncTest(RedisIntegrationFixture fixture)
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
    public async Task AddStringCacheAsync()
    {
        IServiceProvider serviceProvider = Setup();
        var customer = new Customer
        {
            Oid = Guid.NewGuid(),
            Created = DateTime.Now,
            Name = "Test 1",
            Active = true
        };
        string content = customer.ToSerialize();

        IRepositoryCacheAsync<Customer> repo = serviceProvider.GetRequiredService<IRepositoryCacheAsync<Customer>>();
        await repo.SetStringAsync(keyString, content);
        Assert.False(string.IsNullOrEmpty(await repo.GetStringAsync(keyString)));
    }

    [DockerFact, Priority(2)]
    public async Task GetStringAsync()
    {
        IServiceProvider serviceProvider = Setup();
        IRepositoryCacheAsync<Customer> repo = serviceProvider.GetRequiredService<IRepositoryCacheAsync<Customer>>();
        await repo.SetStringAsync(keyString, "Test");
        string? content = await repo.GetStringAsync(keyString);
        Assert.False(string.IsNullOrEmpty(content));
    }

    [DockerFact, Priority(3)]
    public async Task RemoveStringAsync()
    {
        IServiceProvider serviceProvider = Setup();
        IRepositoryCacheAsync<Customer> repo = serviceProvider.GetRequiredService<IRepositoryCacheAsync<Customer>>();
        await repo.SetStringAsync(keyString, "Test");
        await repo.RemoveAsync(keyString);
        string? content = await repo.GetStringAsync(keyString);
        Assert.True(string.IsNullOrEmpty(content));
    }

    [DockerFact, Priority(4)]
    public async Task AddObjectCacheAsync()
    {
        IServiceProvider serviceProvider = Setup();
        var customer = new Customer
        {
            Oid = Guid.NewGuid(),
            Created = DateTime.Now,
            Name = "Test 1",
            Active = true
        };
        IRepositoryCacheAsync<Customer> repo = serviceProvider.GetRequiredService<IRepositoryCacheAsync<Customer>>();
        await repo.SetAsync(keyObject, customer);
        Assert.NotNull(await repo.GetAsync(keyObject));
    }

    [DockerFact, Priority(5)]
    public async Task GetObjectAsync()
    {
        IServiceProvider serviceProvider = Setup();
        IRepositoryCacheAsync<Customer> repo = serviceProvider.GetRequiredService<IRepositoryCacheAsync<Customer>>();
        await repo.SetAsync(keyObject, new Customer { Name = "GetObject" });
        Customer? customer = await repo.GetAsync(keyObject);
        Assert.NotNull(customer);
    }

    [DockerFact, Priority(6)]
    public async Task RemoveObjectAsync()
    {
        IServiceProvider serviceProvider = Setup();
        IRepositoryCacheAsync<Customer> repo = serviceProvider.GetRequiredService<IRepositoryCacheAsync<Customer>>();
        await repo.SetAsync(keyObject, new Customer { Name = "RemoveObject" });
        await repo.RemoveAsync(keyObject);
        Customer? customer = await repo.GetAsync(keyObject);
        Assert.Null(customer);
    }
}
