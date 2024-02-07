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
    public class Test5QueryPagingService
    {
        private readonly IServiceProvider serviceProvider;

        #region [ Ctor ]
        /// <summary>
        /// Initialize
        /// </summary>
        public Test5QueryPagingService()
        {
            serviceProvider = Startup.Initialize();
        }
        #endregion

        #region [ List ]
        [Fact, Priority(2)]
        public void GetFilterCustomerList()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingService>();
            // act
            var pagingResult = service.ListWithPagination();
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(5)]
        public void GetFilterCustomerListPaging()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingService>();
            var paging = new PagingCriteria(3, 0);
            // act
            var pagingResult = service.ListWithPagination(paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(5)]
        public void GetFilterCustomerListNavigation()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingService>();
            var paging = new PagingCriteria(3, 0, navigation: new List<string> { "Contacts" });
            // act
            var pagingResult = service.ListWithPagination(paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(6)]
        public void GetFilterCustomerListOrderAsc()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingService>();
            var paging = new PagingCriteria(3, 0, new List<string> { "Name" });
            // act
            var pagingResult = service.ListWithPagination(paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(6)]
        public void GetFilterCustomerListOrderDesc()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingService>();
            var paging = new PagingCriteria(3, 0, new List<string> { "Name desc" });
            // act
            var pagingResult = service.ListWithPagination(paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(7)]
        public void GetFilterCustomerListOrderAscExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.OrderByAscendingExpr.Add(x => x.Name);
            // act
            var pagingResult = service.ListWithPagination(paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(8)]
        public void GetFilterCustomerListOrderDescExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.OrderByDescendingExpr.Add(x => x.Name);
            // act
            var pagingResult = service.ListWithPagination(paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(9)]
        public void GetFilterCustomerListPagingExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            // act
            var pagingResult = service.ListWithPagination(paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(9)]
        public void GetFilterCustomerListNavigationExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.NavigationExpr.Add(x => x.Contacts);
            // act
            var pagingResult = service.ListWithPagination(paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        #endregion

        #region [ GetBy ]
        [Fact, Priority(2)]
        public void GetFilterCustomerGetBy()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingService>();
            // act
            var pagingResult = service.GetByWithPagination(x => x.Name.Contains("Test"));
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(5)]
        public void GetFilterCustomerGetByPaging()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingService>();
            var paging = new PagingCriteria(3, 0);
            // act
            var pagingResult = service.GetByWithPagination(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(5)]
        public void GetFilterCustomerGetByNavigation()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingService>();
            var paging = new PagingCriteria(3, 0, navigation: new List<string> { "Contacts" });
            // act
            var pagingResult = service.GetByWithPagination(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(6)]
        public void GetFilterCustomerGetByOrderAsc()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingService>();
            var paging = new PagingCriteria(3, 0, new List<string> { "Name" });
            // act
            var pagingResult = service.GetByWithPagination(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(6)]
        public void GetFilterCustomerGetByOrderDesc()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingService>();
            var paging = new PagingCriteria(3, 0, new List<string> { "Name desc" });
            // act
            var pagingResult = service.GetByWithPagination(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(7)]
        public void GetFilterCustomerGetByOrderAscExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.OrderByAscendingExpr.Add(x => x.Name);
            // act
            var pagingResult = service.GetByWithPagination(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(8)]
        public void GetFilterCustomerGetByOrderDescExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.OrderByDescendingExpr.Add(x => x.Name);
            // act
            var pagingResult = service.GetByWithPagination(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(9)]
        public void GetFilterCustomerGetByPagingExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            // act
            var pagingResult = service.GetByWithPagination(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(9)]
        public void GetFilterCustomerGetByNavigationExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.NavigationExpr.Add(x => x.Contacts);
            // act
            var pagingResult = service.GetByWithPagination(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        #endregion
    }
}

