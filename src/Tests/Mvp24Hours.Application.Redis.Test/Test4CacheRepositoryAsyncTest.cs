//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.Redis.Test.Support.Entities;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Helpers;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Caching;
using System;
using System.Threading.Tasks;
using Testcontainers.Redis;
using Xunit;
using Xunit.Priority;

namespace Mvp24Hours.Application.Redis.Test
{
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
    public class Test4CacheRepositoryAsyncTest : IAsyncLifetime
    {
        #region [ Container ]
        private readonly RedisContainer _redisContainer = new RedisBuilder()
            .WithImage("redis:3.2.5-alpine")
            .WithExposedPort(6379)
            .WithCleanUp(true)
            .Build();

        public async Task InitializeAsync()
            => await _redisContainer.StartAsync().ConfigureAwait(false);

        public async Task DisposeAsync()
            => await _redisContainer.DisposeAsync().ConfigureAwait(false);
        #endregion

        private readonly string keyString = $"stringtest-{StringHelper.GenerateKey(5)}";
        private readonly string keyObject = $"objecttest-{StringHelper.GenerateKey(5)}";
        private IServiceProvider serviceProvider;

        public Test4CacheRepositoryAsyncTest()
        {
        }

        private void Setup()
        {
            var services = new ServiceCollection();
            // caching
            services.AddScoped<IRepositoryCache<Customer>, RepositoryCache<Customer>>();
            services.AddScoped<IRepositoryCacheAsync<Customer>, RepositoryCacheAsync<Customer>>();

            // caching.redis
            services.AddMvp24HoursCaching();
            services.AddMvp24HoursCachingRedis(_redisContainer.GetConnectionString());
            serviceProvider = services.BuildServiceProvider();
        }

        [Fact, Priority(1)]
        public async Task AddStringCacheAsync()
        {
            Setup();
            var customer = new Customer
            {
                Oid = Guid.NewGuid(),
                Created = DateTime.Now,
                Name = "Test 1",
                Active = true
            };
            string content = customer.ToSerialize();

            var repo = serviceProvider.GetService<IRepositoryCacheAsync<Customer>>();
            await repo.SetStringAsync(keyString, content);
            Assert.True(true);
        }

        [Fact, Priority(2)]
        public async Task GetStringAsync()
        {
            Setup();
            var repo = serviceProvider.GetService<IRepositoryCacheAsync<Customer>>();
            await repo.SetStringAsync(keyString, "Test");
            string content = await repo.GetStringAsync(keyString);
            Assert.False(string.IsNullOrEmpty(content));
        }

        [Fact, Priority(3)]
        public async Task RemoveStringAsync()
        {
            Setup();
            var repo = serviceProvider.GetService<IRepositoryCacheAsync<Customer>>();
            await repo.RemoveAsync(keyString);
            string content = await repo.GetStringAsync(keyString);
            Assert.True(string.IsNullOrEmpty(content));
        }

        [Fact, Priority(4)]
        public async Task AddObjectCacheAsync()
        {
            Setup();
            var customer = new Customer
            {
                Oid = Guid.NewGuid(),
                Created = DateTime.Now,
                Name = "Test 1",
                Active = true
            };
            var repo = serviceProvider.GetService<IRepositoryCacheAsync<Customer>>();
            await repo.SetAsync(keyObject, customer);
            Assert.True(true);
        }

        [Fact, Priority(5)]
        public async Task GetObjectAsync()
        {
            Setup();
            var repo = serviceProvider.GetService<IRepositoryCacheAsync<Customer>>();
            await repo.SetAsync(keyObject, new Customer { });
            var customer = await repo.GetAsync(keyObject);
            Assert.NotNull(customer);
        }

        [Fact, Priority(6)]
        public async Task RemoveObjectAsync()
        {
            Setup();
            var repo = serviceProvider.GetService<IRepositoryCacheAsync<Customer>>();
            await repo.RemoveAsync(keyObject);
            var customer = await repo.GetAsync(keyObject);
            Assert.Null(customer);
        }
    }
}
