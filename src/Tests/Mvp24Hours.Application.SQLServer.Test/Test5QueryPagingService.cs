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
using Mvp24Hours.Application.SQLServer.Test.Support.Services;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;
using Xunit;
using Xunit.Priority;

namespace Mvp24Hours.Application.SQLServer.Test
{
    /// <summary>
    /// 
    /// </summary>
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
    [Trait("Category", "Unit")]
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
            CustomerPagingService? service = serviceProvider.GetRequiredService<CustomerPagingService>();
            // act
            IPagingResult<IList<Customer>> pagingResult = service.ListWithPagination();
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(5)]
        public void GetFilterCustomerListPaging()
        {
            // arrange
            CustomerPagingService? service = serviceProvider.GetRequiredService<CustomerPagingService>();
            var paging = new PagingCriteria(3, 0);
            // act
            IPagingResult<IList<Customer>> pagingResult = service.ListWithPagination(paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(5)]
        public void GetFilterCustomerListNavigation()
        {
            // arrange
            CustomerPagingService? service = serviceProvider.GetRequiredService<CustomerPagingService>();
            var paging = new PagingCriteria(3, 0, navigation: new List<string> { "Contacts" });
            // act
            IPagingResult<IList<Customer>> pagingResult = service.ListWithPagination(paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(6)]
        public void GetFilterCustomerListOrderAsc()
        {
            // arrange
            CustomerPagingService? service = serviceProvider.GetRequiredService<CustomerPagingService>();
            var paging = new PagingCriteria(3, 0, new List<string> { "Name" });
            // act
            IPagingResult<IList<Customer>> pagingResult = service.ListWithPagination(paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(6)]
        public void GetFilterCustomerListOrderDesc()
        {
            // arrange
            CustomerPagingService? service = serviceProvider.GetRequiredService<CustomerPagingService>();
            var paging = new PagingCriteria(3, 0, new List<string> { "Name desc" });
            // act
            IPagingResult<IList<Customer>> pagingResult = service.ListWithPagination(paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(7)]
        public void GetFilterCustomerListOrderAscExpression()
        {
            // arrange
            CustomerPagingService? service = serviceProvider.GetRequiredService<CustomerPagingService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.OrderByAscendingExpr.Add(x => x.Name);
            // act
            IPagingResult<IList<Customer>> pagingResult = service.ListWithPagination(paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(8)]
        public void GetFilterCustomerListOrderDescExpression()
        {
            // arrange
            CustomerPagingService? service = serviceProvider.GetRequiredService<CustomerPagingService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.OrderByDescendingExpr.Add(x => x.Name);
            // act
            IPagingResult<IList<Customer>> pagingResult = service.ListWithPagination(paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(9)]
        public void GetFilterCustomerListPagingExpression()
        {
            // arrange
            CustomerPagingService? service = serviceProvider.GetRequiredService<CustomerPagingService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            // act
            IPagingResult<IList<Customer>> pagingResult = service.ListWithPagination(paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(9)]
        public void GetFilterCustomerListNavigationExpression()
        {
            // arrange
            CustomerPagingService? service = serviceProvider.GetRequiredService<CustomerPagingService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.NavigationExpr.Add(x => x.Contacts);
            // act
            IPagingResult<IList<Customer>> pagingResult = service.ListWithPagination(paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        #endregion

        #region [ GetBy ]
        [Fact, Priority(2)]
        public void GetFilterCustomerGetBy()
        {
            // arrange
            CustomerPagingService? service = serviceProvider.GetRequiredService<CustomerPagingService>();
            // act
            IPagingResult<IList<Customer>> pagingResult = service.GetByWithPagination(x => x.Name.Contains("Test"));
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(5)]
        public void GetFilterCustomerGetByPaging()
        {
            // arrange
            CustomerPagingService? service = serviceProvider.GetRequiredService<CustomerPagingService>();
            var paging = new PagingCriteria(3, 0);
            // act
            IPagingResult<IList<Customer>> pagingResult = service.GetByWithPagination(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(5)]
        public void GetFilterCustomerGetByNavigation()
        {
            // arrange
            CustomerPagingService? service = serviceProvider.GetRequiredService<CustomerPagingService>();
            var paging = new PagingCriteria(3, 0, navigation: new List<string> { "Contacts" });
            // act
            IPagingResult<IList<Customer>> pagingResult = service.GetByWithPagination(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(6)]
        public void GetFilterCustomerGetByOrderAsc()
        {
            // arrange
            CustomerPagingService? service = serviceProvider.GetRequiredService<CustomerPagingService>();
            var paging = new PagingCriteria(3, 0, new List<string> { "Name" });
            // act
            IPagingResult<IList<Customer>> pagingResult = service.GetByWithPagination(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(6)]
        public void GetFilterCustomerGetByOrderDesc()
        {
            // arrange
            CustomerPagingService? service = serviceProvider.GetRequiredService<CustomerPagingService>();
            var paging = new PagingCriteria(3, 0, new List<string> { "Name desc" });
            // act
            IPagingResult<IList<Customer>> pagingResult = service.GetByWithPagination(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(7)]
        public void GetFilterCustomerGetByOrderAscExpression()
        {
            // arrange
            CustomerPagingService? service = serviceProvider.GetRequiredService<CustomerPagingService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.OrderByAscendingExpr.Add(x => x.Name);
            // act
            IPagingResult<IList<Customer>> pagingResult = service.GetByWithPagination(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(8)]
        public void GetFilterCustomerGetByOrderDescExpression()
        {
            // arrange
            CustomerPagingService? service = serviceProvider.GetRequiredService<CustomerPagingService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.OrderByDescendingExpr.Add(x => x.Name);
            // act
            IPagingResult<IList<Customer>> pagingResult = service.GetByWithPagination(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(9)]
        public void GetFilterCustomerGetByPagingExpression()
        {
            // arrange
            CustomerPagingService? service = serviceProvider.GetRequiredService<CustomerPagingService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            // act
            IPagingResult<IList<Customer>> pagingResult = service.GetByWithPagination(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(9)]
        public void GetFilterCustomerGetByNavigationExpression()
        {
            // arrange
            CustomerPagingService? service = serviceProvider.GetRequiredService<CustomerPagingService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.NavigationExpr.Add(x => x.Contacts);
            // act
            IPagingResult<IList<Customer>> pagingResult = service.GetByWithPagination(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        #endregion
    }
}

