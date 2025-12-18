//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.MongoDb.Test.Support.Entities;
using Mvp24Hours.Application.MongoDb.Test.Support.Services;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using System;
using System.Collections.Generic;
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
    public class QueryServiceTest : IAsyncLifetime
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
        #endregion

        #region [ Configure ]
        public QueryServiceTest() { }

        private void Setup()
        {
            var services = new ServiceCollection();
            services.AddMvp24HoursDbContext(options =>
            {
                options.DatabaseName = "queryservicetest";
                options.ConnectionString = _mongoDbContainer.GetConnectionString();
            });
            services.AddMvp24HoursRepository(repositoryOptions: null);
            services.AddScoped<CustomerService, CustomerService>();
            serviceProvider = services.BuildServiceProvider();

            CreateManyCustomers();
        }

        private void CreateManyCustomers()
        {
            var service = serviceProvider.GetService<CustomerService>();
            for (int i = 0; i < 3; i++)
            {
                service.Add(new Customer
                {
                    Created = DateTime.Now,
                    Name = $"Test {i}",
                    Active = true
                });
            }
        }
        #endregion

        #region [ Facts ]
        [Fact]
        public void GetFilterCustomerList()
        {
            Setup();
            var service = serviceProvider.GetService<CustomerService>();
            var result = service.List();
            Assert.True(result.GetDataCount() > 0);
        }

        [Fact]
        public void GetFilterCustomerListAny()
        {
            Setup();
            var service = serviceProvider.GetService<CustomerService>();
            var result = service.ListAny();
            Assert.True(result.GetDataValue());
        }

        [Fact]
        public void GetFilterCustomerListCount()
        {
            Setup();
            var service = serviceProvider.GetService<CustomerService>();
            var result = service.ListCount();
            Assert.True(result.GetDataValue() > 0);
        }

        [Fact]
        public void GetFilterCustomerListPaging()
        {
            Setup();
            var service = serviceProvider.GetService<CustomerService>();
            var paging = new PagingCriteria(3, 0);
            var result = service.List(paging);
            Assert.True(result.HasDataCount(3));
        }

        [Fact]
        public void GetFilterCustomerListOrder()
        {
            Setup();
            var service = serviceProvider.GetService<CustomerService>();
            var paging = new PagingCriteria(3, 0, new List<string> { "Name desc" });
            var result = service.List(paging);
            Assert.True(result.HasDataCount(3));
        }

        [Fact]
        public void GetFilterCustomerListOrderExpression()
        {
            Setup();
            var service = serviceProvider.GetService<CustomerService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.OrderByDescendingExpr.Add(x => x.Name);
            var result = service.List(paging);
            Assert.True(result.HasDataCount(3));
        }

        [Fact]
        public void GetFilterCustomerListPagingExpression()
        {
            Setup();
            var service = serviceProvider.GetService<CustomerService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            var result = service.List(paging);
            Assert.True(result.HasDataCount(3));
        }

        [Fact]
        public void GetFilterCustomerByName()
        {
            Setup();
            var service = serviceProvider.GetService<CustomerService>();
            var result = service.GetBy(x => x.Name == "Test 2");
            Assert.True(result.HasData());
        }
        #endregion
    }
}
