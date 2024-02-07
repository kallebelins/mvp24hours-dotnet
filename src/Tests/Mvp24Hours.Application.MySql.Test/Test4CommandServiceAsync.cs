//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.MySql.Test.Setup;
using Mvp24Hours.Application.MySql.Test.Support.Entities;
using Mvp24Hours.Application.MySql.Test.Support.Services.Async;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Priority;

namespace Mvp24Hours.Application.MySql.Test
{
    /// <summary>
    /// 
    /// </summary>
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
    public class Test4CommandServiceAsync
    {
        #region [ Actions ]
        [Fact, Priority(1)]
        public async Task CreateCustomer()
        {
            // arrange
            var serviceProvider = StartupAsync.Initialize(false);
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            // act
            var customer = new Customer
            {
                Name = "Test 1",
                Active = true
            };
            await service.AddAsync(customer);
            // assert
            Assert.True(customer.Id > 0);
            // dispose
            StartupAsync.Cleanup(serviceProvider);
        }
        [Fact, Priority(2)]
        public async Task CreateManyCustomers()
        {
            // arrange
            var serviceProvider = StartupAsync.Initialize(false);
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            // act
            List<Customer> customers = [];
            for (int i = 2; i <= 10; i++)
            {
                customers.Add(new Customer
                {
                    Name = $"Test {i}",
                    Active = true
                });
            }
            await service.AddAsync(customers);
            // assert
            Assert.False(customers.AnySafe(x => x.Id == 0));
            // dispose
            StartupAsync.Cleanup(serviceProvider);
        }
        [Fact, Priority(3)]
        public async Task UpdateCustomer()
        {
            // arrange
            var serviceProvider = StartupAsync.Initialize();
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            // act
            var customer = await service.GetByIdAsync(1).GetDataValueAsync();
            customer.Name = "Test Updated";
            await service.ModifyAsync(customer);
            customer = await service.GetByIdAsync(1).GetDataValueAsync();
            // assert
            Assert.Equal("Test Updated", customer?.Name);
            // dispose
            StartupAsync.Cleanup(serviceProvider);
        }
        [Fact, Priority(4)]
        public async Task UpdateManyCustomers()
        {
            // arrange
            var serviceProvider = StartupAsync.Initialize();
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            var paging = new PagingCriteria(1, 0);
            var customers = await service.ListAsync(paging)
                .GetDataValueAsync();
            foreach (var item in customers)
                item.Active = false;
            await service.ModifyAsync(customers);
            var result = await service.GetByCountAsync(x => !x.Active);
            // assert
            Assert.True(result.GetDataValue() > 0);
            // dispose
            StartupAsync.Cleanup(serviceProvider);
        }
        [Fact, Priority(5)]
        public async Task DeleteCustomer()
        {
            // arrange
            var serviceProvider = StartupAsync.Initialize();
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            // act
            var customer = await service.GetByIdAsync(1).GetDataValueAsync();
            await service.RemoveByIdAsync(customer.Id);
            var result = await service.GetByIdAsync(customer.Id);
            // assert
            Assert.Null(result.GetDataValue());
            // dispose
            StartupAsync.Cleanup(serviceProvider);
        }
        [Fact, Priority(6)]
        public async Task DeleteManyCustomers()
        {
            // arrange
            var serviceProvider = StartupAsync.Initialize();
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            // act
            var customers = await service.ListAsync().GetDataValueAsync();
            await service.RemoveAsync(customers);
            var result = await service.ListCountAsync();
            // assert
            Assert.Equal(0, result.GetDataValue());
            // dispose
            StartupAsync.Cleanup(serviceProvider);
        }
        #endregion
    }
}
