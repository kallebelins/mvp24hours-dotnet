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
    public class Test1CacheTest : IAsyncLifetime
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

        private IServiceProvider serviceProvider;
        private readonly string keyString = $"stringtest-{StringHelper.GenerateKey(5)}";
        private readonly string keyObject = $"objecttest-{StringHelper.GenerateKey(5)}";

        public Test1CacheTest()
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
        public void SetString()
        {
            Setup();
            // arrange
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
            cache.SetString(keyString, content);

            // assert
            Assert.True(cache.GetString(keyString).HasValue());
        }

        [Fact, Priority(2)]
        public void GetString()
        {
            Setup();
            // arrange
            var cache = serviceProvider.GetService<IDistributedCache>();

            // act
            cache.SetString(keyString, "Test");
            string content = cache.GetString(keyString);

            // assert
            Assert.True(content.HasValue());
        }

        [Fact, Priority(3)]
        public void RemoveString()
        {
            Setup();
            // arrange
            var cache = serviceProvider.GetService<IDistributedCache>();

            //  act
            cache.SetString(keyString, "Test");
            cache.Remove(keyString);

            // assert
            string content = cache.GetString(keyString);
            Assert.False(content.HasValue());
        }

        [Fact, Priority(4)]
        public void SetObject()
        {
            Setup();
            // arrange
            var cache = serviceProvider.GetService<IDistributedCache>();
            var customer = new Customer
            {
                Oid = Guid.NewGuid(),
                Created = DateTime.Now,
                Name = "Test 1",
                Active = true
            };

            //  act
            cache.SetObject(keyObject, customer);

            // assert
            var result = cache.GetObject<Customer>(keyObject);
            Assert.NotNull(result);
        }

        [Fact, Priority(5)]
        public void GetObject()
        {
            Setup();
            // arrange
            var cache = serviceProvider.GetService<IDistributedCache>();
            var customer = new Customer
            {
                Oid = Guid.NewGuid(),
                Created = DateTime.Now,
                Name = "Test 1",
                Active = true
            };
            cache.SetObject(keyObject, customer);

            //  act
            customer = cache.GetObject<Customer>(keyObject);

            // assert
            Assert.NotNull(customer);
        }

        [Fact, Priority(6)]
        public void RemoveObject()
        {
            Setup();
            // arrange
            var cache = serviceProvider.GetService<IDistributedCache>();
            var customer = new Customer
            {
                Oid = Guid.NewGuid(),
                Created = DateTime.Now,
                Name = "Test 1",
                Active = true
            };
            cache.SetObject(keyObject, customer);

            //  act
            cache.Remove(keyObject);

            // assert
            customer = cache.GetObject<Customer>(keyObject);
            Assert.Null(customer);
        }
    }
}
