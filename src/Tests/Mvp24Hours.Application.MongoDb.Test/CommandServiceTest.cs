//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Mvp24Hours.Application.MongoDb.Test.Support.Entities;
using Mvp24Hours.Application.MongoDb.Test.Support.Services;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using Testcontainers.MongoDb;
using Xunit;
using Xunit.Priority;

namespace Mvp24Hours.Application.MongoDb.Test
{
    /// <summary>
    /// 
    /// </summary>
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
    [Trait("Category", "Integration")]
    public class CommandServiceTest : IAsyncLifetime
    {
        #region [ Container ]
        private readonly MongoDbContainer _mongoDbContainer =
            new MongoDbBuilder("mongo:6.0").Build();

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
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();

            service.Add(new Customer
            {
                Oid = oid,
                Created = DateTime.Now,
                Name = "Test 1",
                Active = true
            });

            IBusinessResult<Customer> result = service.GetById(oid);

            Assert.True(result.HasData());
        }

        [Fact]
        public void UpdateCustomer()
        {
            Setup();
            CreateCustomer();
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();

            Customer? customer = service.GetById(oid).GetDataValue();

            customer.Name = "Test Updated";

            service.Modify(customer);

            IBusinessResult<Customer> boCustomer = service.GetById(oid);

            Assert.True(boCustomer != null && boCustomer.Data?.Name == "Test Updated");
        }

        [Fact]
        public void DeleteCustomer()
        {
            Setup();
            UpdateCustomer();
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();

            service.RemoveById(oid);

            IBusinessResult<Customer> result = service.GetById(oid);

            Assert.False(result.HasData());
        }
        #endregion
    }
}
