//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
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

namespace Mvp24Hours.Application.SQLServer.Test;

/// <summary>
/// 
/// </summary>
[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
[Trait("Category", "Unit")]
public class Test1BasicLogService
{
    private readonly IServiceProvider serviceProvider;

    #region [ Ctor ]
    /// <summary>
    /// Initialize
    /// </summary>
    public Test1BasicLogService()
    {
        serviceProvider = Startup.InitializeBasicLog();
    }
    #endregion

    #region [ Log ]

    [Fact, Priority(1)]
    public void CreateDateLog()
    {
        // arrange
        CustomerBasicLogService? service = serviceProvider.GetRequiredService<CustomerBasicLogService>();
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
        CustomerBasicLogService? service = serviceProvider.GetRequiredService<CustomerBasicLogService>();
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
        CustomerBasicLogService? service = serviceProvider.GetRequiredService<CustomerBasicLogService>();
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
        CustomerBasicLogService? service = serviceProvider.GetRequiredService<CustomerBasicLogService>();
        // act
        IBusinessResult<IList<CustomerBasicLog>> result = service.List();
        // assert
        Assert.True(result.HasData());
    }
    [Fact, Priority(2)]
    public void GetFilterCustomerListAny()
    {
        // arrange
        CustomerBasicLogService? service = serviceProvider.GetRequiredService<CustomerBasicLogService>();
        // act
        IBusinessResult<bool> result = service.ListAny();
        // assert
        Assert.True(result.GetDataValue());
    }
    [Fact, Priority(3)]
    public void GetFilterCustomerListCount()
    {
        // arrange
        CustomerBasicLogService? service = serviceProvider.GetRequiredService<CustomerBasicLogService>();
        // act
        IBusinessResult<int> result = service.ListCount();
        // assert
        Assert.True(result.GetDataValue() > 0);
    }
    [Fact, Priority(4)]
    public void GetFilterCustomerListPaging()
    {
        // arrange
        CustomerBasicLogService? service = serviceProvider.GetRequiredService<CustomerBasicLogService>();
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
        CustomerBasicLogService? service = serviceProvider.GetRequiredService<CustomerBasicLogService>();
        var paging = new PagingCriteria(3, 0, navigation: ["Contacts"]);
        // act
        IBusinessResult<IList<CustomerBasicLog>> result = service.List(paging);
        // assert
        Assert.True(result.HasDataCount(3));
    }
    [Fact, Priority(6)]
    public void GetFilterCustomerListOrderAsc()
    {
        // arrange
        CustomerBasicLogService? service = serviceProvider.GetRequiredService<CustomerBasicLogService>();
        var paging = new PagingCriteria(3, 0, ["Name"]);
        // act
        IBusinessResult<IList<CustomerBasicLog>> result = service.List(paging);
        // assert
        Assert.True(result.HasDataCount(3));
    }
    [Fact, Priority(7)]
    public void GetFilterCustomerListOrderDesc()
    {
        // arrange
        CustomerBasicLogService? service = serviceProvider.GetRequiredService<CustomerBasicLogService>();
        var paging = new PagingCriteria(3, 0, ["Name desc"]);
        // act
        IBusinessResult<IList<CustomerBasicLog>> result = service.List(paging);
        // assert
        Assert.True(result.HasDataCount(3));
    }
    [Fact, Priority(8)]
    public void GetFilterCustomerListOrderAscExpression()
    {
        // arrange
        CustomerBasicLogService? service = serviceProvider.GetRequiredService<CustomerBasicLogService>();
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
        CustomerBasicLogService? service = serviceProvider.GetRequiredService<CustomerBasicLogService>();
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
        CustomerBasicLogService? service = serviceProvider.GetRequiredService<CustomerBasicLogService>();
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
        CustomerBasicLogService? service = serviceProvider.GetRequiredService<CustomerBasicLogService>();
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
        CustomerBasicLogService? service = serviceProvider.GetRequiredService<CustomerBasicLogService>();
        // act
        IBusinessResult<CustomerBasicLog?> result = service.GetById(1);
        // assert
        Assert.NotNull(result.GetDataValue());
    }
    [Fact, Priority(13)]
    public void GetFilterCustomerGetByIdNavigation()
    {
        // arrange
        CustomerBasicLogService? service = serviceProvider.GetRequiredService<CustomerBasicLogService>();
        var paging = new PagingCriteria(1, 0, navigation: ["Contacts"]);
        // act
        IBusinessResult<CustomerBasicLog?> result = service.GetById(1, paging);
        // assert
        CustomerBasicLog? data = result.GetDataValue();
        Assert.NotNull(data);
        Assert.True(data.Contacts.AnyOrNotNull());
    }
    [Fact, Priority(14)]
    public void GetFilterCustomerGetBy()
    {
        // arrange
        CustomerBasicLogService? service = serviceProvider.GetRequiredService<CustomerBasicLogService>();
        // act
        IBusinessResult<IList<CustomerBasicLog>> result = service.GetBy(x => x.Name.Contains("Test"));
        // assert
        Assert.True(result.HasData());
    }
    [Fact, Priority(15)]
    public void GetFilterCustomerGetByAny()
    {
        // arrange
        CustomerBasicLogService? service = serviceProvider.GetRequiredService<CustomerBasicLogService>();
        // act
        IBusinessResult<bool> result = service.GetByAny(x => x.Name.Contains("Test"));
        // assert
        Assert.True(result.GetDataValue());
    }
    [Fact, Priority(16)]
    public void GetFilterCustomerGetByCount()
    {
        // arrange
        CustomerBasicLogService? service = serviceProvider.GetRequiredService<CustomerBasicLogService>();
        // act
        IBusinessResult<int> result = service.GetByCount(x => x.Name.Contains("Test"));
        // assert
        Assert.True(result.GetDataValue() > 0);
    }
    [Fact, Priority(17)]
    public void GetFilterCustomerGetByPaging()
    {
        // arrange
        CustomerBasicLogService? service = serviceProvider.GetRequiredService<CustomerBasicLogService>();
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
        CustomerBasicLogService? service = serviceProvider.GetRequiredService<CustomerBasicLogService>();
        var paging = new PagingCriteria(3, 0, navigation: ["Contacts"]);
        // act
        IBusinessResult<IList<CustomerBasicLog>> result = service.GetBy(x => x.Name.Contains("Test"), paging);
        // assert
        Assert.True(result.HasDataCount(3));
    }
    [Fact, Priority(19)]
    public void GetFilterCustomerGetByOrderAsc()
    {
        // arrange
        CustomerBasicLogService? service = serviceProvider.GetRequiredService<CustomerBasicLogService>();
        var paging = new PagingCriteria(3, 0, orderBy: ["Name"]);
        // act
        IBusinessResult<IList<CustomerBasicLog>> result = service.GetBy(x => x.Name.Contains("Test"), paging);
        // assert
        Assert.True(result.HasDataCount(3));
    }
    [Fact, Priority(20)]
    public void GetFilterCustomerGetByOrderDesc()
    {
        // arrange
        CustomerBasicLogService? service = serviceProvider.GetRequiredService<CustomerBasicLogService>();
        var paging = new PagingCriteria(3, 0, orderBy: ["Name desc"]);
        // act
        IBusinessResult<IList<CustomerBasicLog>> result = service.GetBy(x => x.Name.Contains("Test"), paging);
        // assert
        Assert.True(result.HasDataCount(3));
    }
    [Fact, Priority(21)]
    public void GetFilterCustomerGetByOrderAscExpression()
    {
        // arrange
        CustomerBasicLogService? service = serviceProvider.GetRequiredService<CustomerBasicLogService>();
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
        CustomerBasicLogService? service = serviceProvider.GetRequiredService<CustomerBasicLogService>();
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
        CustomerBasicLogService? service = serviceProvider.GetRequiredService<CustomerBasicLogService>();
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
        CustomerBasicLogService? service = serviceProvider.GetRequiredService<CustomerBasicLogService>();
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
        CustomerBasicLogService? service = serviceProvider.GetRequiredService<CustomerBasicLogService>();
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
        CustomerBasicLogService? service = serviceProvider.GetRequiredService<CustomerBasicLogService>();
        IPagingCriteria paging = new PagingCriteria(3, 0)
            .NewCriteria(navigation: ["Contacts"]);
        // act
        IBusinessResult<IList<CustomerBasicLog>> result = service.GetBy(x => x.Name.Contains("Test"), paging);
        // assert
        Assert.True(result.HasDataCount(3));
    }

    #endregion
}
