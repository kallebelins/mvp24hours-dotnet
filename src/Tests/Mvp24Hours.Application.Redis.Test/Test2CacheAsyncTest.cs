//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Distributed;
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
    public class Test2CacheAsyncTest : IAsyncLifetime
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

        public Test2CacheAsyncTest()
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
        public async Task SetStringAsync()
        {
            // arrange
            Setup();
            var cache = serviceProvider.GetService<IDistributedCache>();
            var customer = new Customer
            {
                Oid = Guid.NewGuid(),
                Created = DateTime.Now,
                Name = "Test 1",
                Active = true
            };
            string content = customer.ToSerialize();

            // act
            await cache.SetStringAsync(keyString, content);

            // assert
            var result = await cache.GetStringAsync(keyString);
            Assert.True(result.HasValue());
        }

        [Fact, Priority(2)]
        public async Task GetStringAsync()
        {
            // arrange
            Setup();
            var cache = serviceProvider.GetService<IDistributedCache>();

            // act
            await cache.SetStringAsync(keyString, "Test");
            string content = await cache.GetStringAsync(keyString);

            // assert
            Assert.True(content.HasValue());
        }

        [Fact, Priority(3)]
        public async Task RemoveStringAsync()
        {
            // arrange
            Setup();
            var cache = serviceProvider.GetService<IDistributedCache>();

            //  act
            await cache.RemoveAsync(keyString);

            // assert
            string content = await cache.GetStringAsync(keyString);
            Assert.False(content.HasValue());
        }

        [Fact, Priority(4)]
        public async Task SetObjectAsync()
        {
            // arrange
            Setup();
            var cache = serviceProvider.GetService<IDistributedCache>();
            var customer = new Customer
            {
                Oid = Guid.NewGuid(),
                Created = DateTime.Now,
                Name = "Test 1",
                Active = true
            };

            //  act
            await cache.SetObjectAsync(keyObject, customer);

            // assert
            var result = await cache.GetObjectAsync<Customer>(keyObject);
            Assert.NotNull(result);
        }

        [Fact, Priority(5)]
        public async Task GetObjectAsync()
        {
            // arrange
            Setup();
            var cache = serviceProvider.GetService<IDistributedCache>();
            var customer = new Customer
            {
                Oid = Guid.NewGuid(),
                Created = DateTime.Now,
                Name = "Test 1",
                Active = true
            };
            await cache.SetObjectAsync(keyObject, customer);

            //  act
            customer = await cache.GetObjectAsync<Customer>(keyObject);

            // assert
            Assert.NotNull(customer);
        }

        [Fact, Priority(6)]
        public async Task RemoveObjectAsync()
        {
            // arrange
            Setup();
            var cache = serviceProvider.GetService<IDistributedCache>();
            var customer = new Customer
            {
                Oid = Guid.NewGuid(),
                Created = DateTime.Now,
                Name = "Test 1",
                Active = true
            };
            await cache.SetObjectAsync(keyObject, customer);

            //  act
            await cache.RemoveAsync(keyObject);

            // assert
            customer = await cache.GetObjectAsync<Customer>(keyObject);
            Assert.Null(customer);
        }
    }
}
