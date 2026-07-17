//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.MySql.Test.Setup;
using Mvp24Hours.Application.MySql.Test.Support.Entities;
using Mvp24Hours.Application.MySql.Test.Support.Services;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using Xunit;
using Xunit.Priority;

namespace Mvp24Hours.Application.MySql.Test
{
    /// <summary>
    /// 
    /// </summary>
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
    [Trait("Category", "Unit")]
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
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
            // act
            IBusinessResult<IList<Customer>>? result = service?.List();
            // assert
            Assert.True(result?.HasData());
        }
        [Fact, Priority(2)]
        public void GetFilterCustomerListAny()
        {
            // arrange
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
            // act
            IBusinessResult<bool>? result = service?.ListAny();
            // assert
            Assert.True(result?.GetDataValue());
        }
        [Fact, Priority(3)]
        public void GetFilterCustomerListCount()
        {
            // arrange
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
            // act
            IBusinessResult<int>? result = service?.ListCount();
            // assert
            Assert.True(result?.GetDataValue() > 0);
        }
        [Fact, Priority(4)]
        public void GetFilterCustomerListPaging()
        {
            // arrange
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
            var paging = new PagingCriteria(3, 0);
            // act
            IBusinessResult<IList<Customer>> result = service.List(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(5)]
        public void GetFilterCustomerListNavigation()
        {
            // arrange
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
            var paging = new PagingCriteria(3, 0, navigation: new List<string> { "Contacts" });
            // act
            IBusinessResult<IList<Customer>> result = service.List(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(6)]
        public void GetFilterCustomerListOrderAsc()
        {
            // arrange
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
            var paging = new PagingCriteria(3, 0, new List<string> { "Name" });
            // act
            IBusinessResult<IList<Customer>> result = service.List(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(7)]
        public void GetFilterCustomerListOrderDesc()
        {
            // arrange
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
            var paging = new PagingCriteria(3, 0, new List<string> { "Name desc" });
            // act
            IBusinessResult<IList<Customer>> result = service.List(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(8)]
        public void GetFilterCustomerListOrderAscExpression()
        {
            // arrange
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.OrderByAscendingExpr.Add(x => x.Name);
            // act
            IBusinessResult<IList<Customer>> result = service.List(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(9)]
        public void GetFilterCustomerListOrderDescExpression()
        {
            // arrange
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.OrderByDescendingExpr.Add(x => x.Name);
            // act
            IBusinessResult<IList<Customer>> result = service.List(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(10)]
        public void GetFilterCustomerListPagingExpression()
        {
            // arrange
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            // act
            IBusinessResult<IList<Customer>> result = service.List(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(11)]
        public void GetFilterCustomerListNavigationExpression()
        {
            // arrange
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.NavigationExpr.Add(x => x.Contacts);
            // act
            IBusinessResult<IList<Customer>> result = service.List(paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        #endregion

        #region [ GetBy ]
        [Fact, Priority(12)]
        public void GetFilterCustomerGetById()
        {
            // arrange
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
            // act
            IBusinessResult<Customer> result = service.GetById(1);
            // assert
            Assert.NotNull(result.GetDataValue());
        }
        [Fact, Priority(13)]
        public void GetFilterCustomerGetByIdNavigation()
        {
            // arrange
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
            var paging = new PagingCriteria(1, 0, navigation: new List<string> { "Contacts" });
            // act
            IBusinessResult<Customer> result = service.GetById(1, paging);
            // assert
            Assert.True(result.GetDataValue().Contacts.AnyOrNotNull());
        }
        [Fact, Priority(14)]
        public void GetFilterCustomerGetBy()
        {
            // arrange
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
            // act
            IBusinessResult<IList<Customer>> result = service.GetBy(x => x.Name.Contains("Test"));
            // assert
            Assert.True(result.HasData());
        }
        [Fact, Priority(15)]
        public void GetFilterCustomerGetByAny()
        {
            // arrange
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
            // act
            IBusinessResult<bool> result = service.GetByAny(x => x.Name.Contains("Test"));
            // assert
            Assert.True(result.GetDataValue());
        }
        [Fact, Priority(16)]
        public void GetFilterCustomerGetByCount()
        {
            // arrange
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
            // act
            IBusinessResult<int> result = service.GetByCount(x => x.Name.Contains("Test"));
            // assert
            Assert.True(result.GetDataValue() > 0);
        }
        [Fact, Priority(17)]
        public void GetFilterCustomerGetByPaging()
        {
            // arrange
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
            var paging = new PagingCriteria(3, 0);
            // act
            IBusinessResult<IList<Customer>> result = service.GetBy(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(18)]
        public void GetFilterCustomerGetByNavigation()
        {
            // arrange
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
            var paging = new PagingCriteria(3, 0, navigation: new List<string> { "Contacts" });
            // act
            IBusinessResult<IList<Customer>> result = service.GetBy(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result.HasDataCount(3));
        }
        [Fact, Priority(19)]
        public void GetFilterCustomerGetByOrderAsc()
        {
            // arrange
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
            var paging = new PagingCriteria(3, 0, orderBy: ["Name"]);
            // act
            IBusinessResult<IList<Customer>>? result = service?.GetBy(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result?.HasDataCount(3));
        }
        [Fact, Priority(20)]
        public void GetFilterCustomerGetByOrderDesc()
        {
            // arrange
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
            var paging = new PagingCriteria(3, 0, orderBy: ["Name desc"]);
            // act
            IBusinessResult<IList<Customer>>? result = service?.GetBy(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result?.HasDataCount(3));
        }
        [Fact, Priority(21)]
        public void GetFilterCustomerGetByOrderAscExpression()
        {
            // arrange
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.OrderByAscendingExpr.Add(x => x.Name);
            // act
            IBusinessResult<IList<Customer>>? result = service?.GetBy(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result?.HasDataCount(3));
        }
        [Fact, Priority(22)]
        public void GetFilterCustomerGetByOrderDescExpression()
        {
            // arrange
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.OrderByDescendingExpr.Add(x => x.Name);
            // act
            IBusinessResult<IList<Customer>>? result = service?.GetBy(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result?.HasDataCount(3));
        }
        [Fact, Priority(23)]
        public void GetFilterCustomerGetByPagingExpression()
        {
            // arrange
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            // act
            IBusinessResult<IList<Customer>>? result = service?.GetBy(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result?.HasDataCount(3));
        }
        [Fact, Priority(24)]
        public void GetFilterCustomerGetByNavigationExpression()
        {
            // arrange
            CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.NavigationExpr.Add(x => x.Contacts);
            // act
            IBusinessResult<IList<Customer>>? result = service?.GetBy(x => x.Name.Contains("Test"), paging);
            // assert
            Assert.True(result?.HasDataCount(3));
        }

        #endregion
    }
}
