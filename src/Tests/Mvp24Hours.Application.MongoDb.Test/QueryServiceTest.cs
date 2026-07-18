//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.MongoDb.Test.Support.Entities;
using Mvp24Hours.Application.MongoDb.Test.Support.Services;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using Testcontainers.MongoDb;
using Xunit;
using Xunit.Priority;

namespace Mvp24Hours.Application.MongoDb.Test;

/// <summary>
/// 
/// </summary>
[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
[Trait("Category", "Integration")]
public class QueryServiceTest : IAsyncLifetime
{
    #region [ Container ]
    private readonly MongoDbContainer _mongoDbContainer =
        new MongoDbBuilder("mongo:6.0").Build();

    public async Task InitializeAsync()
    {
        await _mongoDbContainer.StartAsync().ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        await _mongoDbContainer.DisposeAsync().ConfigureAwait(false);
    }
    #endregion

    #region [ Configure ]
    public QueryServiceTest() { }

    private IServiceProvider Setup()
    {
        var services = new ServiceCollection();
        services.AddMvp24HoursDbContext(options =>
        {
            options.DatabaseName = "queryservicetest";
            options.ConnectionString = _mongoDbContainer.GetConnectionString();
        });
        services.AddMvp24HoursRepository(repositoryOptions: null);
        services.AddScoped<CustomerService, CustomerService>();
        IServiceProvider serviceProvider = services.BuildServiceProvider();

        CreateManyCustomers(serviceProvider);
        return serviceProvider;
    }

    private static void CreateManyCustomers(IServiceProvider serviceProvider)
    {
        CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
        for (int i = 0; i < 3; i++)
        {
            service.Add(new Customer
            {
                Created = DateTime.Now,
                Name = $"Test {i}",
                Active = true
            });
        }
    }
    #endregion

    #region [ Facts ]
    [Fact]
    public void GetFilterCustomerList()
    {
        IServiceProvider serviceProvider = Setup();
        CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
        IBusinessResult<IList<Customer>> result = service.List();
        Assert.True(result.GetDataCount() > 0);
    }

    [Fact]
    public void GetFilterCustomerListAny()
    {
        IServiceProvider serviceProvider = Setup();
        CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
        IBusinessResult<bool> result = service.ListAny();
        Assert.True(result.GetDataValue());
    }

    [Fact]
    public void GetFilterCustomerListCount()
    {
        IServiceProvider serviceProvider = Setup();
        CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
        IBusinessResult<int> result = service.ListCount();
        Assert.True(result.GetDataValue() > 0);
    }

    [Fact]
    public void GetFilterCustomerListPaging()
    {
        IServiceProvider serviceProvider = Setup();
        CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
        var paging = new PagingCriteria(3, 0);
        IBusinessResult<IList<Customer>> result = service.List(paging);
        Assert.True(result.HasDataCount(3));
    }

    [Fact]
    public void GetFilterCustomerListOrder()
    {
        IServiceProvider serviceProvider = Setup();
        CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
        var paging = new PagingCriteria(3, 0, ["Name desc"]);
        IBusinessResult<IList<Customer>> result = service.List(paging);
        Assert.True(result.HasDataCount(3));
    }

    [Fact]
    public void GetFilterCustomerListOrderExpression()
    {
        IServiceProvider serviceProvider = Setup();
        CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
        var paging = new PagingCriteriaExpression<Customer>(3, 0);
        paging.OrderByDescendingExpr.Add(x => x.Name);
        IBusinessResult<IList<Customer>> result = service.List(paging);
        Assert.True(result.HasDataCount(3));
    }

    [Fact]
    public void GetFilterCustomerListPagingExpression()
    {
        IServiceProvider serviceProvider = Setup();
        CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
        var paging = new PagingCriteriaExpression<Customer>(3, 0);
        IBusinessResult<IList<Customer>> result = service.List(paging);
        Assert.True(result.HasDataCount(3));
    }

    [Fact]
    public void GetFilterCustomerByName()
    {
        IServiceProvider serviceProvider = Setup();
        CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
        IBusinessResult<IList<Customer>> result = service.GetBy(x => x.Name == "Test 2");
        Assert.True(result.HasData());
    }
    #endregion
}
