//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.SQLServer.Test.Setup;
using Mvp24Hours.Application.SQLServer.Test.Support.Entities;
using Mvp24Hours.Application.SQLServer.Test.Support.Services;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using System.Collections.Generic;
using Xunit;
using Xunit.Priority;

namespace Mvp24Hours.Application.SQLServer.Test
{
    /// <summary>
    /// 
    /// </summary>
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
    public class Test3CommandService
    {
        #region [ Actions ]
        [Fact, Priority(1)]
        public void CreateCustomer()
        {
            // arrange
            var serviceProvider = Startup.Initialize(false);
            var service = serviceProvider.GetService<CustomerService>();
            // act
            var customer = new Customer
            {
                Name = "Test 1",
                Active = true
            };
            service.Add(customer);
            // assert
            Assert.True(customer.Id > 0);
            // dispose
            Startup.Cleanup(serviceProvider);
        }
        [Fact, Priority(2)]
        public void CreateManyCustomers()
        {
            // arrange
            var serviceProvider = Startup.Initialize(false);
            var service = serviceProvider.GetService<CustomerService>();
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
            service.Add(customers);
            // assert
            Assert.False(customers.AnySafe(x => x.Id == 0));
            // dispose
            Startup.Cleanup(serviceProvider);
        }
        [Fact, Priority(3)]
        public void UpdateCustomer()
        {
            // arrange
            var serviceProvider = Startup.Initialize();
            var service = serviceProvider.GetService<CustomerService>();
            // act
            var customer = service.GetById(1).GetDataValue();
            customer.Name = "Test Updated";
            service.Modify(customer);
            customer = service.GetById(1).GetDataValue();
            // assert
            Assert.Equal("Test Updated", customer?.Name);
            // dispose
            Startup.Cleanup(serviceProvider);
        }
        [Fact, Priority(4)]
        public void UpdateManyCustomers()
        {
            // arrange
            var serviceProvider = Startup.Initialize();
            var service = serviceProvider.GetService<CustomerService>();
            // act
            var paging = new PagingCriteria(1, 0);
            var customers = service.List(paging)
                .GetDataValue();
            foreach (var item in customers)
                item.Active = false;
            service.Modify(customers);
            var result = service.GetByCount(x => !x.Active);
            // assert
            Assert.True(result.GetDataValue() > 0);
            // dispose
            Startup.Cleanup(serviceProvider);
        }
        [Fact, Priority(5)]
        public void DeleteCustomer()
        {
            // arrange
            var serviceProvider = Startup.Initialize();
            var service = serviceProvider.GetService<CustomerService>();
            // act
            var customer = service.GetById(1).GetDataValue();
            service.RemoveById(customer.Id);
            var result = service.GetById(customer.Id);
            // assert
            Assert.Null(result.GetDataValue());
            // dispose
            Startup.Cleanup(serviceProvider);
        }
        [Fact, Priority(6)]
        public void DeleteManyCustomers()
        {
            // arrange
            var serviceProvider = Startup.Initialize();
            var service = serviceProvider.GetService<CustomerService>();
            // act
            var customers = service.List().Data;
            service.Remove(customers);
            var result = service.ListCount();
            // assert
            Assert.Equal(0, result.GetDataValue());
            // dispose
            Startup.Cleanup(serviceProvider);
        }
        #endregion
    }
}
