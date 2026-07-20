//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.MongoDb.Test.Support;
using Mvp24Hours.Application.MongoDb.Test.Support.Entities;
using Mvp24Hours.Application.MongoDb.Test.Support.Services;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using Xunit;
using Xunit.Priority;

namespace Mvp24Hours.Application.MongoDb.Test;

/// <summary>
/// 
/// </summary>
[Collection(MongoDbIntegrationCollection.Name)]
[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
[Trait("Category", "Integration")]
public class QueryServiceTest(MongoDbIntegrationFixture fixture)
{
    #region [ Configure ]
    private IServiceProvider Setup()
    {
        var services = new ServiceCollection();
        services.AddMvp24HoursDbContext(options =>
        {
            options.DatabaseName = $"queryservicetest_{Guid.NewGuid():N}";
            options.ConnectionString = fixture.ConnectionString;
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
    [DockerFact]
    public void GetFilterCustomerList()
    {
        IServiceProvider serviceProvider = Setup();
        CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
        IBusinessResult<IList<Customer>> result = service.List();
        Assert.True(result.GetDataCount() > 0);
    }

    [DockerFact]
    public void GetFilterCustomerListAny()
    {
        IServiceProvider serviceProvider = Setup();
        CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
        IBusinessResult<bool> result = service.ListAny();
        Assert.True(result.GetDataValue());
    }

    [DockerFact]
    public void GetFilterCustomerListCount()
    {
        IServiceProvider serviceProvider = Setup();
        CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
        IBusinessResult<int> result = service.ListCount();
        Assert.True(result.GetDataValue() > 0);
    }

    [DockerFact]
    public void GetFilterCustomerListPaging()
    {
        IServiceProvider serviceProvider = Setup();
        CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
        var paging = new PagingCriteria(3, 0);
        IBusinessResult<IList<Customer>> result = service.List(paging);
        Assert.True(result.HasDataCount(3));
    }

    [DockerFact]
    public void GetFilterCustomerListOrder()
    {
        IServiceProvider serviceProvider = Setup();
        CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
        var paging = new PagingCriteria(3, 0, ["Name desc"]);
        IBusinessResult<IList<Customer>> result = service.List(paging);
        Assert.True(result.HasDataCount(3));
    }

    [DockerFact]
    public void GetFilterCustomerListOrderExpression()
    {
        IServiceProvider serviceProvider = Setup();
        CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
        var paging = new PagingCriteriaExpression<Customer>(3, 0);
        paging.OrderByDescendingExpr.Add(x => x.Name);
        IBusinessResult<IList<Customer>> result = service.List(paging);
        Assert.True(result.HasDataCount(3));
    }

    [DockerFact]
    public void GetFilterCustomerListPagingExpression()
    {
        IServiceProvider serviceProvider = Setup();
        CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
        var paging = new PagingCriteriaExpression<Customer>(3, 0);
        IBusinessResult<IList<Customer>> result = service.List(paging);
        Assert.True(result.HasDataCount(3));
    }

    [DockerFact]
    public void GetFilterCustomerByName()
    {
        IServiceProvider serviceProvider = Setup();
        CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();
        IBusinessResult<IList<Customer>> result = service.GetBy(x => x.Name == "Test 2");
        Assert.True(result.HasData());
    }
    #endregion
}
