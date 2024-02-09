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
using System;
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
    public class Test2QueryServiceAsync
    {
        private readonly IServiceProvider serviceProvider;

        #region [ Ctor ]
        /// <summary>
        /// Initialize
        /// </summary>
        public Test2QueryServiceAsync()
        {
            serviceProvider = StartupAsync.Initialize();
        }
        #endregion

        #region [ List ]
        [Fact, Priority(1)]
        public async Task GetFilterCustomerList()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            // act
            var result = await service.ListAsync();
            // assert
            Assert.True(result.HasData());
        }
        [Fact, Priority(2)]
        public async Task GetFilterCustomerListAny()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            // act
            var result = await service.ListAnyAsync();
            // assert
            Assert.True(result.GetDataValue());
        }
        [Fact, Priority(3)]
        public async Task GetFilterCustomerListCount()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            // act
            var result = await service.ListCountAsync();
            // assert
            Assert.True(result.GetDataValue() > 0);
        }
        [Fact, Priority(4)]
        public async Task GetFilterCustomerListPaging()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            var paging = new PagingCriteria(3, 0);
            // act
            var result = await service.ListAsync(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(5)]
        public async Task GetFilterCustomerListNavigation()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            var paging = new PagingCriteria(3, 0, navigation: new List<string> { "Contacts" });
            // act
            var result = await service.ListAsync(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(6)]
        public async Task GetFilterCustomerListOrderAsc()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            var paging = new PagingCriteria(3, 0, new List<string> { "Name" });
            // act
            var result = await service.ListAsync(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(7)]
        public async Task GetFilterCustomerListOrderDesc()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            var paging = new PagingCriteria(3, 0, new List<string> { "Name desc" });
            // act
            var result = await service.ListAsync(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(8)]
        public async Task GetFilterCustomerListOrderAscExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.OrderByAscendingExpr.Add(x => x.Name);
            // act
            var result = await service.ListAsync(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(9)]
        public async Task GetFilterCustomerListOrderDescExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.OrderByDescendingExpr.Add(x => x.Name);
            // act
            var result = await service.ListAsync(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(10)]
        public async Task GetFilterCustomerListPagingExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            // act
            var result = await service.ListAsync(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(11)]
        public async Task GetFilterCustomerListNavigationExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.NavigationExpr.Add(x => x.Contacts);
            // act
            var result = await service.ListAsync(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        #endregion

        #region [ GetBy ]
        [Fact, Priority(12)]
        public async Task GetFilterCustomerGetById()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            // act
            var result = await service.GetByIdAsync(1);
            // assert
            Assert.NotNull(result.GetDataValue());
        }
        [Fact, Priority(13)]
        public async Task GetFilterCustomerGetByIdNavigation()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            var paging = new PagingCriteria(1, 0, navigation: new List<string> { "Contacts" });
            // act
            var result = await service.GetByIdAsync(1, paging);
            // assert
            Assert.True(result.GetDataValue().Contacts.AnyOrNotNull());
        }
        [Fact, Priority(14)]
        public async Task GetFilterCustomerGetBy()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            // act
            var result = await service.GetByAsync(x => x.Name.Contains("Test"));
            // assert
            Assert.True(result.HasData());
        }
        [Fact, Priority(15)]
        public async Task GetFilterCustomerGetByAny()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            // act
            var result = await service.GetByAnyAsync(x => x.Name.Contains("Test"));
            // assert
            Assert.True(result.GetDataValue());
        }
        [Fact, Priority(16)]
        public async Task GetFilterCustomerGetByCount()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            // act
            var result = await service.GetByCountAsync(x => x.Name.Contains("Test"));
            // assert
            Assert.True(result.GetDataValue() > 0);
        }
        [Fact, Priority(17)]
        public async Task GetFilterCustomerGetByPaging()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            var paging = new PagingCriteria(3, 0);
            // act
            var result = await service.GetByAsync(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(18)]
        public async Task GetFilterCustomerGetByNavigation()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            var paging = new PagingCriteria(3, 0, navigation: new List<string> { "Contacts" });
            // act
            var result = await service.GetByAsync(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(19)]
        public async Task GetFilterCustomerGetByOrderAsc()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            var paging = new PagingCriteria(3, 0, new List<string> { "Name" });
            // act
            var result = await service.GetByAsync(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(20)]
        public async Task GetFilterCustomerGetByOrderDesc()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            var paging = new PagingCriteria(3, 0, new List<string> { "Name desc" });
            // act
            var result = await service.GetByAsync(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(21)]
        public async Task GetFilterCustomerGetByOrderAscExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.OrderByAscendingExpr.Add(x => x.Name);
            // act
            var result = await service.GetByAsync(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(22)]
        public async Task GetFilterCustomerGetByOrderDescExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.OrderByDescendingExpr.Add(x => x.Name);
            // act
            var result = await service.GetByAsync(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(23)]
        public async Task GetFilterCustomerGetByPagingExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            // act
            var result = await service.GetByAsync(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(24)]
        public async Task GetFilterCustomerGetByNavigationExpression()
        {
            // arrange
            var service = serviceProvider.GetService<CustomerServiceAsync>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.NavigationExpr.Add(x => x.Contacts);
            // act
            var result = await service.GetByAsync(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        #endregion
    }
}
