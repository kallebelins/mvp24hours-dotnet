//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.PostgreSql.Test.Setup;
using Mvp24Hours.Application.PostgreSql.Test.Support.Entities;
using Mvp24Hours.Application.PostgreSql.Test.Support.Services.Async;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;
using Xunit;
using Xunit.Priority;

namespace Mvp24Hours.Application.PostgreSql.Test;

/// <summary>
/// 
/// </summary>
[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
[Trait("Category", "Unit")]
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
        CustomerPagingServiceAsync? service = serviceProvider.GetRequiredService<CustomerPagingServiceAsync>();
        // act
        IPagingResult<IList<Customer>> pagingResult = await service.ListWithPaginationAsync();
        // assert
        Assert.NotNull(pagingResult.Paging);
    }
    [Fact, Priority(5)]
    public async Task GetFilterCustomerListPaging()
    {
        // arrange
        CustomerPagingServiceAsync? service = serviceProvider.GetRequiredService<CustomerPagingServiceAsync>();
        var paging = new PagingCriteria(3, 0);
        // act
        IPagingResult<IList<Customer>> pagingResult = await service.ListWithPaginationAsync(paging);
        // assert
        Assert.NotNull(pagingResult.Paging);
    }
    [Fact, Priority(5)]
    public async Task GetFilterCustomerListNavigation()
    {
        // arrange
        CustomerPagingServiceAsync? service = serviceProvider.GetRequiredService<CustomerPagingServiceAsync>();
        var paging = new PagingCriteria(3, 0, navigation: ["Contacts"]);
        // act
        IPagingResult<IList<Customer>> pagingResult = await service.ListWithPaginationAsync(paging);
        // assert
        Assert.NotNull(pagingResult.Paging);
    }
    [Fact, Priority(6)]
    public async Task GetFilterCustomerListOrderAsc()
    {
        // arrange
        CustomerPagingServiceAsync? service = serviceProvider.GetRequiredService<CustomerPagingServiceAsync>();
        var paging = new PagingCriteria(3, 0, ["Name"]);
        // act
        IPagingResult<IList<Customer>> pagingResult = await service.ListWithPaginationAsync(paging);
        // assert
        Assert.NotNull(pagingResult.Paging);
    }
    [Fact, Priority(6)]
    public async Task GetFilterCustomerListOrderDesc()
    {
        // arrange
        CustomerPagingServiceAsync? service = serviceProvider.GetRequiredService<CustomerPagingServiceAsync>();
        var paging = new PagingCriteria(3, 0, ["Name desc"]);
        // act
        IPagingResult<IList<Customer>> pagingResult = await service.ListWithPaginationAsync(paging);
        // assert
        Assert.NotNull(pagingResult.Paging);
    }
    [Fact, Priority(7)]
    public async Task GetFilterCustomerListOrderAscExpression()
    {
        // arrange
        CustomerPagingServiceAsync? service = serviceProvider.GetRequiredService<CustomerPagingServiceAsync>();
        var paging = new PagingCriteriaExpression<Customer>(3, 0);
        paging.OrderByAscendingExpr.Add(x => x.Name);
        // act
        IPagingResult<IList<Customer>> pagingResult = await service.ListWithPaginationAsync(paging);
        // assert
        Assert.NotNull(pagingResult.Paging);
    }
    [Fact, Priority(8)]
    public async Task GetFilterCustomerListOrderDescExpression()
    {
        // arrange
        CustomerPagingServiceAsync? service = serviceProvider.GetRequiredService<CustomerPagingServiceAsync>();
        var paging = new PagingCriteriaExpression<Customer>(3, 0);
        paging.OrderByDescendingExpr.Add(x => x.Name);
        // act
        IPagingResult<IList<Customer>> pagingResult = await service.ListWithPaginationAsync(paging);
        // assert
        Assert.NotNull(pagingResult.Paging);
    }
    [Fact, Priority(9)]
    public async Task GetFilterCustomerListPagingExpression()
    {
        // arrange
        CustomerPagingServiceAsync? service = serviceProvider.GetRequiredService<CustomerPagingServiceAsync>();
        var paging = new PagingCriteriaExpression<Customer>(3, 0);
        // act
        IPagingResult<IList<Customer>> pagingResult = await service.ListWithPaginationAsync(paging);
        // assert
        Assert.NotNull(pagingResult.Paging);
    }
    [Fact, Priority(9)]
    public async Task GetFilterCustomerListNavigationExpression()
    {
        // arrange
        CustomerPagingServiceAsync? service = serviceProvider.GetRequiredService<CustomerPagingServiceAsync>();
        var paging = new PagingCriteriaExpression<Customer>(3, 0);
        paging.NavigationExpr.Add(x => x.Contacts);
        // act
        IPagingResult<IList<Customer>> pagingResult = await service.ListWithPaginationAsync(paging);
        // assert
        Assert.NotNull(pagingResult.Paging);
    }
    #endregion

    #region [ GetBy ]
    [Fact, Priority(2)]
    public async Task GetFilterCustomerGetBy()
    {
        // arrange
        CustomerPagingServiceAsync? service = serviceProvider.GetRequiredService<CustomerPagingServiceAsync>();
        // act
        IPagingResult<IList<Customer>> pagingResult = await service.GetByWithPaginationAsync(x => x.Name.Contains("Test"));
        // assert
        Assert.NotNull(pagingResult.Paging);
    }
    [Fact, Priority(5)]
    public async Task GetFilterCustomerGetByPaging()
    {
        // arrange
        CustomerPagingServiceAsync? service = serviceProvider.GetRequiredService<CustomerPagingServiceAsync>();
        var paging = new PagingCriteria(3, 0);
        // act
        IPagingResult<IList<Customer>> pagingResult = await service.GetByWithPaginationAsync(x => x.Name.Contains("Test"), paging);
        // assert
        Assert.NotNull(pagingResult.Paging);
    }
    [Fact, Priority(5)]
    public async Task GetFilterCustomerGetByNavigation()
    {
        // arrange
        CustomerPagingServiceAsync? service = serviceProvider.GetRequiredService<CustomerPagingServiceAsync>();
        var paging = new PagingCriteria(3, 0, navigation: ["Contacts"]);
        // act
        IPagingResult<IList<Customer>> pagingResult = await service.GetByWithPaginationAsync(x => x.Name.Contains("Test"), paging);
        // assert
        Assert.NotNull(pagingResult.Paging);
    }
    [Fact, Priority(6)]
    public async Task GetFilterCustomerGetByOrderAsc()
    {
        // arrange
        CustomerPagingServiceAsync? service = serviceProvider.GetRequiredService<CustomerPagingServiceAsync>();
        var paging = new PagingCriteria(3, 0, ["Name"]);
        // act
        IPagingResult<IList<Customer>> pagingResult = await service.GetByWithPaginationAsync(x => x.Name.Contains("Test"), paging);
        // assert
        Assert.NotNull(pagingResult.Paging);
    }
    [Fact, Priority(6)]
    public async Task GetFilterCustomerGetByOrderDesc()
    {
        // arrange
        CustomerPagingServiceAsync? service = serviceProvider.GetRequiredService<CustomerPagingServiceAsync>();
        var paging = new PagingCriteria(3, 0, ["Name desc"]);
        // act
        IPagingResult<IList<Customer>> pagingResult = await service.GetByWithPaginationAsync(x => x.Name.Contains("Test"), paging);
        // assert
        Assert.NotNull(pagingResult.Paging);
    }
    [Fact, Priority(7)]
    public async Task GetFilterCustomerGetByOrderAscExpression()
    {
        // arrange
        CustomerPagingServiceAsync? service = serviceProvider.GetRequiredService<CustomerPagingServiceAsync>();
        var paging = new PagingCriteriaExpression<Customer>(3, 0);
        paging.OrderByAscendingExpr.Add(x => x.Name);
        // act
        IPagingResult<IList<Customer>> pagingResult = await service.GetByWithPaginationAsync(x => x.Name.Contains("Test"), paging);
        // assert
        Assert.NotNull(pagingResult.Paging);
    }
    [Fact, Priority(8)]
    public async Task GetFilterCustomerGetByOrderDescExpression()
    {
        // arrange
        CustomerPagingServiceAsync? service = serviceProvider.GetRequiredService<CustomerPagingServiceAsync>();
        var paging = new PagingCriteriaExpression<Customer>(3, 0);
        paging.OrderByDescendingExpr.Add(x => x.Name);
        // act
        IPagingResult<IList<Customer>> pagingResult = await service.GetByWithPaginationAsync(x => x.Name.Contains("Test"), paging);
        // assert
        Assert.NotNull(pagingResult.Paging);
    }
    [Fact, Priority(9)]
    public async Task GetFilterCustomerGetByPagingExpression()
    {
        // arrange
        CustomerPagingServiceAsync? service = serviceProvider.GetRequiredService<CustomerPagingServiceAsync>();
        var paging = new PagingCriteriaExpression<Customer>(3, 0);
        // act
        IPagingResult<IList<Customer>> pagingResult = await service.GetByWithPaginationAsync(x => x.Name.Contains("Test"), paging);
        // assert
        Assert.NotNull(pagingResult.Paging);
    }
    [Fact, Priority(9)]
    public async Task GetFilterCustomerGetByNavigationExpression()
    {
        // arrange
        CustomerPagingServiceAsync? service = serviceProvider.GetRequiredService<CustomerPagingServiceAsync>();
        var paging = new PagingCriteriaExpression<Customer>(3, 0);
        paging.NavigationExpr.Add(x => x.Contacts);
        // act
        IPagingResult<IList<Customer>> pagingResult = await service.GetByWithPaginationAsync(x => x.Name.Contains("Test"), paging);
        // assert
        Assert.NotNull(pagingResult.Paging);
    }
    #endregion
}
