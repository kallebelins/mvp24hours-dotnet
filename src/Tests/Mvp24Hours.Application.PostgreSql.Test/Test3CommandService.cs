//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.PostgreSql.Test.Setup;
using Mvp24Hours.Application.PostgreSql.Test.Support.Entities;
using Mvp24Hours.Application.PostgreSql.Test.Support.Services;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using Xunit;
using Xunit.Priority;

namespace Mvp24Hours.Application.PostgreSql.Test
{
    /// <summary>
    /// 
    /// </summary>
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
    [Trait("Category", "Unit")]
    public class Test3CommandService
    {
        #region [ Actions ]
        [Fact, Priority(1)]
        public void CreateCustomer()
        {
            // arrange
            IServiceProvider serviceProvider = Startup.Initialize(false);
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
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
            IServiceProvider serviceProvider = Startup.Initialize(false);
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
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
            Assert.DoesNotContain(customers, x => x.Id == 0);
            // dispose
            Startup.Cleanup(serviceProvider);
        }
        [Fact, Priority(3)]
        public void UpdateCustomer()
        {
            // arrange
            IServiceProvider serviceProvider = Startup.Initialize();
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
            // act
            Customer? customer = service.GetById(1).GetDataValue();
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
            IServiceProvider serviceProvider = Startup.Initialize();
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
            // act
            var paging = new PagingCriteria(1, 0);
            IList<Customer>? customers = service.List(paging)
                .GetDataValue();
            foreach (Customer? item in customers)
                item.Active = false;
            service.Modify(customers);
            IBusinessResult<int> result = service.GetByCount(x => !x.Active);
            // assert
            Assert.True(result.GetDataValue() > 0);
            // dispose
            Startup.Cleanup(serviceProvider);
        }
        [Fact, Priority(5)]
        public void DeleteCustomer()
        {
            // arrange
            IServiceProvider serviceProvider = Startup.Initialize();
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
            // act
            Customer? customer = service.GetById(1).GetDataValue();
            service.RemoveById(customer.Id);
            IBusinessResult<Customer> result = service.GetById(customer.Id);
            // assert
            Assert.Null(result.GetDataValue());
            // dispose
            Startup.Cleanup(serviceProvider);
        }
        [Fact, Priority(6)]
        public void DeleteManyCustomers()
        {
            // arrange
            IServiceProvider serviceProvider = Startup.Initialize();
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
            // act
            IList<Customer>? customers = service.List().Data;
            service.Remove(customers);
            IBusinessResult<int> result = service.ListCount();
            // assert
            Assert.Equal(0, result.GetDataValue());
            // dispose
            Startup.Cleanup(serviceProvider);
        }
        #endregion
    }
}
