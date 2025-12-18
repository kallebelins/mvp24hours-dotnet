//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Mvp24Hours.Application.MongoDb.Test.Support.Entities;
using Mvp24Hours.Application.MongoDb.Test.Support.Services;
using Mvp24Hours.Extensions;
using System;
using System.Threading.Tasks;
using Testcontainers.MongoDb;
using Xunit;
using Xunit.Priority;

namespace Mvp24Hours.Application.MongoDb.Test
{
    /// <summary>
    /// 
    /// </summary>
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
    public class CommandServiceTest : IAsyncLifetime
    {
        #region [ Container ]
        private readonly MongoDbContainer _mongoDbContainer =
            new MongoDbBuilder().Build();

        public async Task InitializeAsync()
            => await _mongoDbContainer.StartAsync().ConfigureAwait(false);

        public async Task DisposeAsync()
            => await _mongoDbContainer.DisposeAsync().ConfigureAwait(false);
        #endregion

        #region [ Fields ]
        private IServiceProvider serviceProvider;
        private ObjectId oid;
        #endregion

        public CommandServiceTest() { }

        #region [ Configure ]
        private void Setup()
        {
            var services = new ServiceCollection();
            services.AddMvp24HoursDbContext(options =>
            {
                options.DatabaseName = "commandservicetest";
                options.ConnectionString = _mongoDbContainer.GetConnectionString();
            });
            services.AddMvp24HoursRepository(repositoryOptions: null);
            services.AddScoped<CustomerService, CustomerService>();
            serviceProvider = services.BuildServiceProvider();
            oid = ObjectId.GenerateNewId();
        }
        #endregion

        #region [ Facts ]
        [Fact]
        public void CreateCustomer()
        {
            Setup();
            var service = serviceProvider.GetService<CustomerService>();

            service.Add(new Customer
            {
                Oid = oid,
                Created = DateTime.Now,
                Name = "Test 1",
                Active = true
            });

            var result = service.GetById(oid);

            Assert.True(result.HasData());
        }

        [Fact]
        public void UpdateCustomer()
        {
            Setup();
            CreateCustomer();
            var service = serviceProvider.GetService<CustomerService>();

            var customer = service.GetById(oid).GetDataValue();

            customer.Name = "Test Updated";

            service.Modify(customer);

            var boCustomer = service.GetById(oid);

            Assert.True(boCustomer != null && boCustomer.Data?.Name == "Test Updated");
        }

        [Fact]
        public void DeleteCustomer()
        {
            Setup();
            UpdateCustomer();
            var service = serviceProvider.GetService<CustomerService>();

            service.RemoveById(oid);

            var result = service.GetById(oid);

            Assert.False(result.HasData());
        }
        #endregion
    }
}
