//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.PostgreSql.Test.Setup;
using Mvp24Hours.Application.PostgreSql.Test.Support.Entities;
using Mvp24Hours.Application.PostgreSql.Test.Support.Services;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Priority;

namespace Mvp24Hours.Application.PostgreSql.Test
{
    /// <summary>
    /// 
    /// </summary>
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
    public class Test1QueryService
    {
        private readonly IServiceProvider serviceProvider;

        #region [ Ctor ]
        /// <summary>
        /// Initialize
        /// </summary>
        public Test1QueryService()
        {
            serviceProvider = Startup.Initialize();
        }
        #endregion

        #region [ List ]
        [Fact, Priority(1)]
        public void GetFilterCustomerList()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerService>();
            // act
            var result = service.List();
            // assert
            Assert.True(result.HasData());
        }
        [Fact, Priority(2)]
        public void GetFilterCustomerListAny()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerService>();
            // act
            var result = service.ListAny();
            // assert
            Assert.True(result.GetDataValue());
        }
        [Fact, Priority(3)]
        public void GetFilterCustomerListCount()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerService>();
            // act
            var result = service.ListCount();
            // assert
            Assert.True(result.GetDataValue() > 0);
        }
        [Fact, Priority(4)]
        public void GetFilterCustomerListPaging()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerService>();
            var paging = new PagingCriteria(3, 0);
            // act
            var result = service.List(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(5)]
        public void GetFilterCustomerListNavigation()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerService>();
            var paging = new PagingCriteria(3, 0, navigation: new List<string> { "Contacts" });
            // act
            var result = service.List(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(6)]
        public void GetFilterCustomerListOrderAsc()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerService>();
            var paging = new PagingCriteria(3, 0, new List<string> { "Name" });
            // act
            var result = service.List(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(7)]
        public void GetFilterCustomerListOrderDesc()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerService>();
            var paging = new PagingCriteria(3, 0, new List<string> { "Name desc" });
            // act
            var result = service.List(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(8)]
        public void GetFilterCustomerListOrderAscExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.OrderByAscendingExpr.Add(x => x.Name);
            // act
            var result = service.List(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(9)]
        public void GetFilterCustomerListOrderDescExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.OrderByDescendingExpr.Add(x => x.Name);
            // act
            var result = service.List(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(10)]
        public void GetFilterCustomerListPagingExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            // act
            var result = service.List(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(11)]
        public void GetFilterCustomerListNavigationExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.NavigationExpr.Add(x => x.Contacts);
            // act
            var result = service.List(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        #endregion

        #region [ GetBy ]
        [Fact, Priority(12)]
        public void GetFilterCustomerGetById()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerService>();
            // act
            var result = service.GetById(1);
            // assert
            Assert.NotNull(result.GetDataValue());
        }
        [Fact, Priority(13)]
        public void GetFilterCustomerGetByIdNavigation()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerService>();
            var paging = new PagingCriteria(1, 0, navigation: new List<string> { "Contacts" });
            // act
            var result = service.GetById(1, paging);
            // assert
            Assert.True(result.GetDataValue().Contacts.AnyOrNotNull());
        }
        [Fact, Priority(14)]
        public void GetFilterCustomerGetBy()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerService>();
            // act
            var result = service.GetBy(x => x.Name.Contains("Test"));
            // assert
            Assert.True(result.HasData());
        }
        [Fact, Priority(15)]
        public void GetFilterCustomerGetByAny()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerService>();
            // act
            var result = service.GetByAny(x => x.Name.Contains("Test"));
            // assert
            Assert.True(result.GetDataValue());
        }
        [Fact, Priority(16)]
        public void GetFilterCustomerGetByCount()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerService>();
            // act
            var result = service.GetByCount(x => x.Name.Contains("Test"));
            // assert
            Assert.True(result.GetDataValue() > 0);
        }
        [Fact, Priority(17)]
        public void GetFilterCustomerGetByPaging()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerService>();
            var paging = new PagingCriteria(3, 0);
            // act
            var result = service.GetBy(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(18)]
        public void GetFilterCustomerGetByNavigation()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerService>();
            var paging = new PagingCriteria(3, 0, navigation: new List<string> { "Contacts" });
            // act
            var result = service.GetBy(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(19)]
        public void GetFilterCustomerGetByOrderAsc()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerService>();
            var paging = new PagingCriteria(3, 0, orderBy: new List<string> { "Name" });
            // act
            var result = service.GetBy(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(20)]
        public void GetFilterCustomerGetByOrderDesc()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerService>();
            var paging = new PagingCriteria(3, 0, orderBy: new List<string> { "Name desc" });
            // act
            var result = service.GetBy(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(21)]
        public void GetFilterCustomerGetByOrderAscExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.OrderByAscendingExpr.Add(x => x.Name);
            // act
            var result = service.GetBy(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(22)]
        public void GetFilterCustomerGetByOrderDescExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.OrderByDescendingExpr.Add(x => x.Name);
            // act
            var result = service.GetBy(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(23)]
        public void GetFilterCustomerGetByPagingExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            // act
            var result = service.GetBy(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(24)]
        public void GetFilterCustomerGetByNavigationExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.NavigationExpr.Add(x => x.Contacts);
            // act
            var result = service.GetBy(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }

        #endregion
    }
}
