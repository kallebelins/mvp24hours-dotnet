//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.SQLServer.Test.Setup;
using Mvp24Hours.Application.SQLServer.Test.Support.Entities;
using Mvp24Hours.Application.SQLServer.Test.Support.Services.Async;
using Mvp24Hours.Core.ValueObjects.Logic;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Priority;

namespace Mvp24Hours.Application.SQLServer.Test
{
    /// <summary>
    /// 
    /// </summary>
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
    public class Test6QueryPagingServiceAsync
    {
        private readonly IServiceProvider serviceProvider;

        #region [ Ctor ]
        /// <summary>
        /// Initialize
        /// </summary>
        public Test6QueryPagingServiceAsync()
        {
            serviceProvider = StartupAsync.Initialize();
        }
        #endregion

        #region [ List ]
        [Fact, Priority(2)]
        public async Task GetFilterCustomerList()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingServiceAsync>();
            // act
            var pagingResult = await service.ListWithPaginationAsync();
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(5)]
        public async Task GetFilterCustomerListPaging()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingServiceAsync>();
            var paging = new PagingCriteria(3, 0);
            // act
            var pagingResult = await service.ListWithPaginationAsync(paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(5)]
        public async Task GetFilterCustomerListNavigation()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingServiceAsync>();
            var paging = new PagingCriteria(3, 0, navigation: new List<string> { "Contacts" });
            // act
            var pagingResult = await service.ListWithPaginationAsync(paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(6)]
        public async Task GetFilterCustomerListOrderAsc()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingServiceAsync>();
            var paging = new PagingCriteria(3, 0, new List<string> { "Name" });
            // act
            var pagingResult = await service.ListWithPaginationAsync(paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(6)]
        public async Task GetFilterCustomerListOrderDesc()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingServiceAsync>();
            var paging = new PagingCriteria(3, 0, new List<string> { "Name desc" });
            // act
            var pagingResult = await service.ListWithPaginationAsync(paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(7)]
        public async Task GetFilterCustomerListOrderAscExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingServiceAsync>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.OrderByAscendingExpr.Add(x => x.Name);
            // act
            var pagingResult = await service.ListWithPaginationAsync(paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(8)]
        public async Task GetFilterCustomerListOrderDescExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingServiceAsync>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.OrderByDescendingExpr.Add(x => x.Name);
            // act
            var pagingResult = await service.ListWithPaginationAsync(paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(9)]
        public async Task GetFilterCustomerListPagingExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingServiceAsync>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            // act
            var pagingResult = await service.ListWithPaginationAsync(paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(9)]
        public async Task GetFilterCustomerListNavigationExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingServiceAsync>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.NavigationExpr.Add(x => x.Contacts);
            // act
            var pagingResult = await service.ListWithPaginationAsync(paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        #endregion

        #region [ GetBy ]
        [Fact, Priority(2)]
        public async Task GetFilterCustomerGetBy()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingServiceAsync>();
            // act
            var pagingResult = await service.GetByWithPaginationAsync(x => x.Name.Contains("Test"));
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(5)]
        public async Task GetFilterCustomerGetByPaging()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingServiceAsync>();
            var paging = new PagingCriteria(3, 0);
            // act
            var pagingResult = await service.GetByWithPaginationAsync(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(5)]
        public async Task GetFilterCustomerGetByNavigation()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingServiceAsync>();
            var paging = new PagingCriteria(3, 0, navigation: new List<string> { "Contacts" });
            // act
            var pagingResult = await service.GetByWithPaginationAsync(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(6)]
        public async Task GetFilterCustomerGetByOrderAsc()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingServiceAsync>();
            var paging = new PagingCriteria(3, 0, new List<string> { "Name" });
            // act
            var pagingResult = await service.GetByWithPaginationAsync(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(6)]
        public async Task GetFilterCustomerGetByOrderDesc()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingServiceAsync>();
            var paging = new PagingCriteria(3, 0, new List<string> { "Name desc" });
            // act
            var pagingResult = await service.GetByWithPaginationAsync(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(7)]
        public async Task GetFilterCustomerGetByOrderAscExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingServiceAsync>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.OrderByAscendingExpr.Add(x => x.Name);
            // act
            var pagingResult = await service.GetByWithPaginationAsync(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(8)]
        public async Task GetFilterCustomerGetByOrderDescExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingServiceAsync>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.OrderByDescendingExpr.Add(x => x.Name);
            // act
            var pagingResult = await service.GetByWithPaginationAsync(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(9)]
        public async Task GetFilterCustomerGetByPagingExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingServiceAsync>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            // act
            var pagingResult = await service.GetByWithPaginationAsync(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        [Fact, Priority(9)]
        public async Task GetFilterCustomerGetByNavigationExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerPagingServiceAsync>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.NavigationExpr.Add(x => x.Contacts);
            // act
            var pagingResult = await service.GetByWithPaginationAsync(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.NotNull(pagingResult.Paging);
        }
        #endregion
    }
}
