//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.SQLServer.Test.Setup;
using Mvp24Hours.Application.SQLServer.Test.Support.Entities;
using Mvp24Hours.Application.SQLServer.Test.Support.Entities.Basics;
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
public class Test1BasicService
{
    private readonly IServiceProvider serviceProvider;

    #region [ Ctor ]
    /// <summary>
    /// Initialize
    /// </summary>
    public Test1BasicService()
    {
        serviceProvider = Startup.InitializeBasic();
    }
    #endregion

    #region [ List ]
    [Fact, Priority(1)]
    public void GetFilterCustomerList()
    {
        // arrange
        CustomerBasicService? service = serviceProvider.GetRequiredService<CustomerBasicService>();
        // act
        IBusinessResult<IList<CustomerBasic>> result = service.List();
        // assert
        Assert.True(result.HasData());
    }
    [Fact, Priority(2)]
    public void GetFilterCustomerListAny()
    {
        // arrange
        CustomerBasicService? service = serviceProvider.GetRequiredService<CustomerBasicService>();
        // act
        IBusinessResult<bool> result = service.ListAny();
        // assert
        Assert.True(result.GetDataValue());
    }
    [Fact, Priority(3)]
    public void GetFilterCustomerListCount()
    {
        // arrange
        CustomerBasicService? service = serviceProvider.GetRequiredService<CustomerBasicService>();
        // act
        IBusinessResult<int> result = service.ListCount();
        // assert
        Assert.True(result.GetDataValue() > 0);
    }
    [Fact, Priority(4)]
    public void GetFilterCustomerListPaging()
    {
        // arrange
        CustomerBasicService? service = serviceProvider.GetRequiredService<CustomerBasicService>();
        var paging = new PagingCriteria(3, 0);
        // act
        IBusinessResult<IList<CustomerBasic>> result = service.List(paging);
        // assert
        Assert.True(result.HasDataCount(3));
    }
    [Fact, Priority(5)]
    public void GetFilterCustomerListNavigation()
    {
        // arrange
        CustomerBasicService? service = serviceProvider.GetRequiredService<CustomerBasicService>();
        var paging = new PagingCriteria(3, 0, navigation: ["Contacts"]);
        // act
        IBusinessResult<IList<CustomerBasic>> result = service.List(paging);
        // assert
        Assert.True(result.HasDataCount(3));
    }
    [Fact, Priority(6)]
    public void GetFilterCustomerListOrderAsc()
    {
        // arrange
        CustomerBasicService? service = serviceProvider.GetRequiredService<CustomerBasicService>();
        var paging = new PagingCriteria(3, 0, ["Name"]);
        // act
        IBusinessResult<IList<CustomerBasic>> result = service.List(paging);
        // assert
        Assert.True(result.HasDataCount(3));
    }
    [Fact, Priority(7)]
    public void GetFilterCustomerListOrderDesc()
    {
        // arrange
        CustomerBasicService? service = serviceProvider.GetRequiredService<CustomerBasicService>();
        var paging = new PagingCriteria(3, 0, ["Name desc"]);
        // act
        IBusinessResult<IList<CustomerBasic>> result = service.List(paging);
        // assert
        Assert.True(result.HasDataCount(3));
    }
    [Fact, Priority(8)]
    public void GetFilterCustomerListOrderAscExpression()
    {
        // arrange
        CustomerBasicService? service = serviceProvider.GetRequiredService<CustomerBasicService>();
        var paging = new PagingCriteriaExpression<Customer>(3, 0);
        paging.OrderByAscendingExpr.Add(x => x.Name);
        // act
        IBusinessResult<IList<CustomerBasic>> result = service.List(paging);
        // assert
        Assert.True(result.HasDataCount(3));
    }
    [Fact, Priority(9)]
    public void GetFilterCustomerListOrderDescExpression()
    {
        // arrange
        CustomerBasicService? service = serviceProvider.GetRequiredService<CustomerBasicService>();
        var paging = new PagingCriteriaExpression<Customer>(3, 0);
        paging.OrderByDescendingExpr.Add(x => x.Name);
        // act
        IBusinessResult<IList<CustomerBasic>> result = service.List(paging);
        // assert
        Assert.True(result.HasDataCount(3));
    }
    [Fact, Priority(10)]
    public void GetFilterCustomerListPagingExpression()
    {
        // arrange
        CustomerBasicService? service = serviceProvider.GetRequiredService<CustomerBasicService>();
        var paging = new PagingCriteriaExpression<Customer>(3, 0);
        // act
        IBusinessResult<IList<CustomerBasic>> result = service.List(paging);
        // assert
        Assert.True(result.HasDataCount(3));
    }
    [Fact, Priority(11)]
    public void GetFilterCustomerListNavigationExpression()
    {
        // arrange
        CustomerBasicService? service = serviceProvider.GetRequiredService<CustomerBasicService>();
        var paging = new PagingCriteriaExpression<Customer>(3, 0);
        paging.NavigationExpr.Add(x => x.Contacts);
        // act
        IBusinessResult<IList<CustomerBasic>> result = service.List(paging);
        // assert
        Assert.True(result.HasDataCount(3));
    }
    #endregion

    #region [ GetBy ]
    [Fact, Priority(12)]
    public void GetFilterCustomerGetById()
    {
        // arrange
        CustomerBasicService? service = serviceProvider.GetRequiredService<CustomerBasicService>();
        // act
        IBusinessResult<CustomerBasic?> result = service.GetById(1);
        // assert
        Assert.NotNull(result.GetDataValue());
    }
    [Fact, Priority(13)]
    public void GetFilterCustomerGetByIdNavigation()
    {
        // arrange
        CustomerBasicService? service = serviceProvider.GetRequiredService<CustomerBasicService>();
        var paging = new PagingCriteria(1, 0, navigation: ["Contacts"]);
        // act
        IBusinessResult<CustomerBasic?> result = service.GetById(1, paging);
        // assert
        CustomerBasic? data = result.GetDataValue();
        Assert.NotNull(data);
        Assert.True(data.Contacts.AnyOrNotNull());
    }
    [Fact, Priority(14)]
    public void GetFilterCustomerGetBy()
    {
        // arrange
        CustomerBasicService? service = serviceProvider.GetRequiredService<CustomerBasicService>();
        // act
        IBusinessResult<IList<CustomerBasic>> result = service.GetBy(x => x.Name.Contains("Test"));
        // assert
        Assert.True(result.HasData());
    }
    [Fact, Priority(15)]
    public void GetFilterCustomerGetByAny()
    {
        // arrange
        CustomerBasicService? service = serviceProvider.GetRequiredService<CustomerBasicService>();
        // act
        IBusinessResult<bool> result = service.GetByAny(x => x.Name.Contains("Test"));
        // assert
        Assert.True(result.GetDataValue());
    }
    [Fact, Priority(16)]
    public void GetFilterCustomerGetByCount()
    {
        // arrange
        CustomerBasicService? service = serviceProvider.GetRequiredService<CustomerBasicService>();
        // act
        IBusinessResult<int> result = service.GetByCount(x => x.Name.Contains("Test"));
        // assert
        Assert.True(result.GetDataValue() > 0);
    }
    [Fact, Priority(17)]
    public void GetFilterCustomerGetByPaging()
    {
        // arrange
        CustomerBasicService? service = serviceProvider.GetRequiredService<CustomerBasicService>();
        var paging = new PagingCriteria(3, 0);
        // act
        IBusinessResult<IList<CustomerBasic>> result = service.GetBy(x => x.Name.Contains("Test"), paging);
        // assert
        Assert.True(result.HasDataCount(3));
    }
    [Fact, Priority(18)]
    public void GetFilterCustomerGetByNavigation()
    {
        // arrange
        CustomerBasicService? service = serviceProvider.GetRequiredService<CustomerBasicService>();
        var paging = new PagingCriteria(3, 0, navigation: ["Contacts"]);
        // act
        IBusinessResult<IList<CustomerBasic>> result = service.GetBy(x => x.Name.Contains("Test"), paging);
        // assert
        Assert.True(result.HasDataCount(3));
    }
    [Fact, Priority(19)]
    public void GetFilterCustomerGetByOrderAsc()
    {
        // arrange
        CustomerBasicService? service = serviceProvider.GetRequiredService<CustomerBasicService>();
        var paging = new PagingCriteria(3, 0, orderBy: ["Name"]);
        // act
        IBusinessResult<IList<CustomerBasic>> result = service.GetBy(x => x.Name.Contains("Test"), paging);
        // assert
        Assert.True(result.HasDataCount(3));
    }
    [Fact, Priority(20)]
    public void GetFilterCustomerGetByOrderDesc()
    {
        // arrange
        CustomerBasicService? service = serviceProvider.GetRequiredService<CustomerBasicService>();
        var paging = new PagingCriteria(3, 0, orderBy: ["Name desc"]);
        // act
        IBusinessResult<IList<CustomerBasic>> result = service.GetBy(x => x.Name.Contains("Test"), paging);
        // assert
        Assert.True(result.HasDataCount(3));
    }
    [Fact, Priority(21)]
    public void GetFilterCustomerGetByOrderAscExpression()
    {
        // arrange
        CustomerBasicService? service = serviceProvider.GetRequiredService<CustomerBasicService>();
        var paging = new PagingCriteriaExpression<Customer>(3, 0);
        paging.OrderByAscendingExpr.Add(x => x.Name);
        // act
        IBusinessResult<IList<CustomerBasic>> result = service.GetBy(x => x.Name.Contains("Test"), paging);
        // assert
        Assert.True(result.HasDataCount(3));
    }
    [Fact, Priority(22)]
    public void GetFilterCustomerGetByOrderDescExpression()
    {
        // arrange
        CustomerBasicService? service = serviceProvider.GetRequiredService<CustomerBasicService>();
        var paging = new PagingCriteriaExpression<Customer>(3, 0);
        paging.OrderByDescendingExpr.Add(x => x.Name);
        // act
        IBusinessResult<IList<CustomerBasic>> result = service.GetBy(x => x.Name.Contains("Test"), paging);
        // assert
        Assert.True(result.HasDataCount(3));
    }
    [Fact, Priority(23)]
    public void GetFilterCustomerGetByPagingExpression()
    {
        // arrange
        CustomerBasicService? service = serviceProvider.GetRequiredService<CustomerBasicService>();
        var paging = new PagingCriteriaExpression<Customer>(3, 0);
        // act
        IBusinessResult<IList<CustomerBasic>> result = service.GetBy(x => x.Name.Contains("Test"), paging);
        // assert
        Assert.True(result.HasDataCount(3));
    }
    [Fact, Priority(24)]
    public void GetFilterCustomerGetByNavigationExpression()
    {
        // arrange
        CustomerBasicService? service = serviceProvider.GetRequiredService<CustomerBasicService>();
        var paging = new PagingCriteriaExpression<Customer>(3, 0);
        paging.NavigationExpr.Add(x => x.Contacts);
        // act
        IBusinessResult<IList<CustomerBasic>> result = service.GetBy(x => x.Name.Contains("Test"), paging);
        // assert
        Assert.True(result.HasDataCount(3));
    }
    [Fact, Priority(25)]
    public void GetFilterCustomerGetByNavigationExpressionNewCriteria()
    {
        // arrange
        CustomerBasicService? service = serviceProvider.GetRequiredService<CustomerBasicService>();
        IPagingCriteriaExpression<Customer> paging = new PagingCriteria(3, 0)
            .NewCriteriaExpression<Customer>();
        paging.NavigationExpr.Add(x => x.Contacts);
        // act
        IBusinessResult<IList<CustomerBasic>> result = service.GetBy(x => x.Name.Contains("Test"), paging);
        // assert
        Assert.True(result.HasDataCount(3));
    }
    [Fact, Priority(26)]
    public void GetFilterCustomerGetByNavigationNewCriteria()
    {
        // arrange
        CustomerBasicService? service = serviceProvider.GetRequiredService<CustomerBasicService>();
        IPagingCriteria paging = new PagingCriteria(3, 0)
            .NewCriteria(navigation: ["Contacts"]);
        // act
        IBusinessResult<IList<CustomerBasic>> result = service.GetBy(x => x.Name.Contains("Test"), paging);
        // assert
        Assert.True(result.HasDataCount(3));
    }

    #endregion
}
