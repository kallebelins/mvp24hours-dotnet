//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.SQLServer.Test.Setup;
using Mvp24Hours.Application.SQLServer.Test.Support.Entities;
using Mvp24Hours.Application.SQLServer.Test.Support.Entities.BasicLogs;
using Mvp24Hours.Application.SQLServer.Test.Support.Services;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using Xunit;
using Xunit.Priority;

namespace Mvp24Hours.Application.SQLServer.Test
{
    /// <summary>
    /// 
    /// </summary>
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
    [Trait("Category", "Unit")]
    public class Test1LogService
    {
        private readonly IServiceProvider serviceProvider;

        #region [ Ctor ]
        /// <summary>
        /// Initialize
        /// </summary>
        public Test1LogService()
        {
            serviceProvider = Startup.InitializeLog();
        }
        #endregion

        #region [ Log ]

        [Fact, Priority(1)]
        public void CreateDateLog()
        {
            // arrange
            CustomerLogService? service = serviceProvider.GetRequiredService<CustomerLogService>();
            // act
            var customer = new CustomerBasicLog
            {
                Name = "Test 1",
                Active = true
            };
            service.Add(customer);
            // assert
            Assert.True(customer.Created > DateTime.MinValue);
        }

        [Fact, Priority(1)]
        public void UpdateDateLog()
        {
            // arrange
            CustomerLogService? service = serviceProvider.GetRequiredService<CustomerLogService>();
            // act
            var customer = new CustomerBasicLog
            {
                Name = "Test 1",
                Active = true
            };
            service.Add(customer);

            customer.Name = "Test T";
            service.Modify(customer);

            // assert
            Assert.True(customer.Modified > DateTime.MinValue);
        }

        [Fact, Priority(1)]
        public void RemoveDateLog()
        {
            // arrange
            CustomerLogService? service = serviceProvider.GetRequiredService<CustomerLogService>();
            // act
            var customer = new CustomerBasicLog
            {
                Name = "Test 1",
                Active = true
            };
            service.Add(customer);
            service.Remove(customer);

            // assert
            Assert.True(customer.Removed > DateTime.MinValue);
        }

        #endregion

        #region [ List ]
        [Fact, Priority(1)]
        public void GetFilterCustomerList()
        {
            // arrange
            CustomerLogService? service = serviceProvider.GetRequiredService<CustomerLogService>();
            // act
            IBusinessResult<IList<CustomerBasicLog>> result = service.List();
            // assert
            Assert.True(result.HasData());
        }
        [Fact, Priority(2)]
        public void GetFilterCustomerListAny()
        {
            // arrange
            CustomerLogService? service = serviceProvider.GetRequiredService<CustomerLogService>();
            // act
            IBusinessResult<bool> result = service.ListAny();
            // assert
            Assert.True(result.GetDataValue());
        }
        [Fact, Priority(3)]
        public void GetFilterCustomerListCount()
        {
            // arrange
            CustomerLogService? service = serviceProvider.GetRequiredService<CustomerLogService>();
            // act
            IBusinessResult<int> result = service.ListCount();
            // assert
            Assert.True(result.GetDataValue() > 0);
        }
        [Fact, Priority(4)]
        public void GetFilterCustomerListPaging()
        {
            // arrange
            CustomerLogService? service = serviceProvider.GetRequiredService<CustomerLogService>();
            var paging = new PagingCriteria(3, 0);
            // act
            IBusinessResult<IList<CustomerBasicLog>> result = service.List(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(5)]
        public void GetFilterCustomerListNavigation()
        {
            // arrange
            CustomerLogService? service = serviceProvider.GetRequiredService<CustomerLogService>();
            var paging = new PagingCriteria(3, 0, navigation: new List<string> { "Contacts" });
            // act
            IBusinessResult<IList<CustomerBasicLog>> result = service.List(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(6)]
        public void GetFilterCustomerListOrderAsc()
        {
            // arrange
            CustomerLogService? service = serviceProvider.GetRequiredService<CustomerLogService>();
            var paging = new PagingCriteria(3, 0, new List<string> { "Name" });
            // act
            IBusinessResult<IList<CustomerBasicLog>> result = service.List(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(7)]
        public void GetFilterCustomerListOrderDesc()
        {
            // arrange
            CustomerLogService? service = serviceProvider.GetRequiredService<CustomerLogService>();
            var paging = new PagingCriteria(3, 0, new List<string> { "Name desc" });
            // act
            IBusinessResult<IList<CustomerBasicLog>> result = service.List(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(8)]
        public void GetFilterCustomerListOrderAscExpression()
        {
            // arrange
            CustomerLogService? service = serviceProvider.GetRequiredService<CustomerLogService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.OrderByAscendingExpr.Add(x => x.Name);
            // act
            IBusinessResult<IList<CustomerBasicLog>> result = service.List(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(9)]
        public void GetFilterCustomerListOrderDescExpression()
        {
            // arrange
            CustomerLogService? service = serviceProvider.GetRequiredService<CustomerLogService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.OrderByDescendingExpr.Add(x => x.Name);
            // act
            IBusinessResult<IList<CustomerBasicLog>> result = service.List(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(10)]
        public void GetFilterCustomerListPagingExpression()
        {
            // arrange
            CustomerLogService? service = serviceProvider.GetRequiredService<CustomerLogService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            // act
            IBusinessResult<IList<CustomerBasicLog>> result = service.List(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(11)]
        public void GetFilterCustomerListNavigationExpression()
        {
            // arrange
            CustomerLogService? service = serviceProvider.GetRequiredService<CustomerLogService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.NavigationExpr.Add(x => x.Contacts);
            // act
            IBusinessResult<IList<CustomerBasicLog>> result = service.List(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        #endregion

        #region [ GetBy ]
        [Fact, Priority(12)]
        public void GetFilterCustomerGetById()
        {
            // arrange
            CustomerLogService? service = serviceProvider.GetRequiredService<CustomerLogService>();
            // act
            IBusinessResult<CustomerBasicLog> result = service.GetById(1);
            // assert
            Assert.NotNull(result.GetDataValue());
        }
        [Fact, Priority(13)]
        public void GetFilterCustomerGetByIdNavigation()
        {
            // arrange
            CustomerLogService? service = serviceProvider.GetRequiredService<CustomerLogService>();
            var paging = new PagingCriteria(1, 0, navigation: new List<string> { "Contacts" });
            // act
            IBusinessResult<CustomerBasicLog> result = service.GetById(1, paging);
            // assert
            Assert.True(result.GetDataValue().Contacts.AnyOrNotNull());
        }
        [Fact, Priority(14)]
        public void GetFilterCustomerGetBy()
        {
            // arrange
            CustomerLogService? service = serviceProvider.GetRequiredService<CustomerLogService>();
            // act
            IBusinessResult<IList<CustomerBasicLog>> result = service.GetBy(x => x.Name.Contains("Test"));
            // assert
            Assert.True(result.HasData());
        }
        [Fact, Priority(15)]
        public void GetFilterCustomerGetByAny()
        {
            // arrange
            CustomerLogService? service = serviceProvider.GetRequiredService<CustomerLogService>();
            // act
            IBusinessResult<bool> result = service.GetByAny(x => x.Name.Contains("Test"));
            // assert
            Assert.True(result.GetDataValue());
        }
        [Fact, Priority(16)]
        public void GetFilterCustomerGetByCount()
        {
            // arrange
            CustomerLogService? service = serviceProvider.GetRequiredService<CustomerLogService>();
            // act
            IBusinessResult<int> result = service.GetByCount(x => x.Name.Contains("Test"));
            // assert
            Assert.True(result.GetDataValue() > 0);
        }
        [Fact, Priority(17)]
        public void GetFilterCustomerGetByPaging()
        {
            // arrange
            CustomerLogService? service = serviceProvider.GetRequiredService<CustomerLogService>();
            var paging = new PagingCriteria(3, 0);
            // act
            IBusinessResult<IList<CustomerBasicLog>> result = service.GetBy(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(18)]
        public void GetFilterCustomerGetByNavigation()
        {
            // arrange
            CustomerLogService? service = serviceProvider.GetRequiredService<CustomerLogService>();
            var paging = new PagingCriteria(3, 0, navigation: new List<string> { "Contacts" });
            // act
            IBusinessResult<IList<CustomerBasicLog>> result = service.GetBy(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(19)]
        public void GetFilterCustomerGetByOrderAsc()
        {
            // arrange
            CustomerLogService? service = serviceProvider.GetRequiredService<CustomerLogService>();
            var paging = new PagingCriteria(3, 0, orderBy: new List<string> { "Name" });
            // act
            IBusinessResult<IList<CustomerBasicLog>> result = service.GetBy(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(20)]
        public void GetFilterCustomerGetByOrderDesc()
        {
            // arrange
            CustomerLogService? service = serviceProvider.GetRequiredService<CustomerLogService>();
            var paging = new PagingCriteria(3, 0, orderBy: new List<string> { "Name desc" });
            // act
            IBusinessResult<IList<CustomerBasicLog>> result = service.GetBy(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(21)]
        public void GetFilterCustomerGetByOrderAscExpression()
        {
            // arrange
            CustomerLogService? service = serviceProvider.GetRequiredService<CustomerLogService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.OrderByAscendingExpr.Add(x => x.Name);
            // act
            IBusinessResult<IList<CustomerBasicLog>> result = service.GetBy(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(22)]
        public void GetFilterCustomerGetByOrderDescExpression()
        {
            // arrange
            CustomerLogService? service = serviceProvider.GetRequiredService<CustomerLogService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.OrderByDescendingExpr.Add(x => x.Name);
            // act
            IBusinessResult<IList<CustomerBasicLog>> result = service.GetBy(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(23)]
        public void GetFilterCustomerGetByPagingExpression()
        {
            // arrange
            CustomerLogService? service = serviceProvider.GetRequiredService<CustomerLogService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            // act
            IBusinessResult<IList<CustomerBasicLog>> result = service.GetBy(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(24)]
        public void GetFilterCustomerGetByNavigationExpression()
        {
            // arrange
            CustomerLogService? service = serviceProvider.GetRequiredService<CustomerLogService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.NavigationExpr.Add(x => x.Contacts);
            // act
            IBusinessResult<IList<CustomerBasicLog>> result = service.GetBy(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(25)]
        public void GetFilterCustomerGetByNavigationExpressionNewCriteria()
        {
            // arrange
            CustomerLogService? service = serviceProvider.GetRequiredService<CustomerLogService>();
            IPagingCriteriaExpression<Customer> paging = new PagingCriteria(3, 0)
                .NewCriteriaExpression<Customer>();
            paging.NavigationExpr.Add(x => x.Contacts);
            // act
            IBusinessResult<IList<CustomerBasicLog>> result = service.GetBy(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(26)]
        public void GetFilterCustomerGetByNavigationNewCriteria()
        {
            // arrange
            CustomerLogService? service = serviceProvider.GetRequiredService<CustomerLogService>();
            IPagingCriteria paging = new PagingCriteria(3, 0)
                .NewCriteria(navigation: ["Contacts"]);
            // act
            IBusinessResult<IList<CustomerBasicLog>> result = service.GetBy(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }

        #endregion
    }
}
