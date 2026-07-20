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
public class Test3CacheRepositoryTest(RedisIntegrationFixture fixture)
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
    public void SetContentCache()
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

        IRepositoryCache<Customer> repo = serviceProvider.GetRequiredService<IRepositoryCache<Customer>>();
        repo.SetString(keyString, content);
        Assert.True(repo.GetString(keyString).HasValue());
    }

    [DockerFact, Priority(2)]
    public void GetString()
    {
        IServiceProvider serviceProvider = Setup();
        IRepositoryCache<Customer> repo = serviceProvider.GetRequiredService<IRepositoryCache<Customer>>();
        repo.SetString(keyString, "Test");
        string? content = repo.GetString(keyString);
        Assert.True(content.HasValue());
    }

    [DockerFact, Priority(3)]
    public void RemoveString()
    {
        IServiceProvider serviceProvider = Setup();
        IRepositoryCache<Customer> repo = serviceProvider.GetRequiredService<IRepositoryCache<Customer>>();
        repo.SetString(keyString, "Test");
        repo.Remove(keyString);
        string? content = repo.GetString(keyString);
        Assert.True(string.IsNullOrEmpty(content));
    }

    [DockerFact, Priority(4)]
    public void SetObjectContentCache()
    {
        IServiceProvider serviceProvider = Setup();
        var customer = new Customer
        {
            Oid = Guid.NewGuid(),
            Created = DateTime.Now,
            Name = "Test 1",
            Active = true
        };
        IRepositoryCache<Customer> repo = serviceProvider.GetRequiredService<IRepositoryCache<Customer>>();
        repo.Set(keyObject, customer);
        Assert.NotNull(repo.Get(keyObject));
    }

    [DockerFact, Priority(5)]
    public void GetObject()
    {
        IServiceProvider serviceProvider = Setup();
        IRepositoryCache<Customer> repo = serviceProvider.GetRequiredService<IRepositoryCache<Customer>>();
        repo.Set(keyObject, new Customer { Name = "GetObject" });
        Customer? customer = repo.Get(keyObject);
        Assert.NotNull(customer);
    }

    [DockerFact, Priority(6)]
    public void RemoveObject()
    {
        IServiceProvider serviceProvider = Setup();
        IRepositoryCache<Customer> repo = serviceProvider.GetRequiredService<IRepositoryCache<Customer>>();
        repo.Set(keyObject, new Customer { Name = "RemoveObject" });
        repo.Remove(keyObject);
        Customer? customer = repo.Get(keyObject);
        Assert.Null(customer);
    }
}
