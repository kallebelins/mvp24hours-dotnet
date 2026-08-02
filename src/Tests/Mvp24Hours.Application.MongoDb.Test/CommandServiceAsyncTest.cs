//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Mvp24Hours.Application.Logic;
using Mvp24Hours.Application.MongoDb.Test.Support;
using Mvp24Hours.Application.MongoDb.Test.Support.Entities;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using Xunit;
using Xunit.Priority;

namespace Mvp24Hours.Application.MongoDb.Test;

public class CustomerServiceAsync(IUnitOfWorkAsync unitOfWork) : RepositoryServiceAsync<Customer, IUnitOfWorkAsync>(unitOfWork)
{
    // custom async methods here
}

[Collection(MongoDbIntegrationCollection.Name)]
[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
[Trait("Category", "Integration")]
public class CommandServiceAsyncTest(MongoDbIntegrationFixture fixture)
{
    #region [ Configure ]

    private IServiceProvider Setup()
    {
        var services = new ServiceCollection();
        services.AddMvp24HoursDbContext(options =>
        {
            options.DatabaseName = $"asynctest_{Guid.NewGuid():N}";
            options.ConnectionString = fixture.ConnectionString;
        });
        services.AddMvp24HoursRepositoryAsync(repositoryOptions: null);
        services.AddScoped<CustomerServiceAsync, CustomerServiceAsync>();
        return services.BuildServiceProvider();
    }

    private static async Task<ObjectId> SeedCustomerAsync(CustomerServiceAsync service)
    {
        var oid = ObjectId.GenerateNewId();
        await service.AddAsync(new Customer
        {
            Oid = oid,
            Created = DateTime.Now,
            Name = "Test Customer",
            Active = true
        });
        return oid;
    }

    #endregion

    #region [ Facts ]

    [DockerFact]
    public async Task AddAsync_CustomerIsAdded()
    {
        IServiceProvider sp = Setup();
        CustomerServiceAsync service = sp.GetRequiredService<CustomerServiceAsync>();

        ObjectId oid = await SeedCustomerAsync(service);
        IBusinessResult<Customer?> result = await service.GetByIdAsync(oid);

        Assert.True(result.HasData());
    }

    [DockerFact]
    public async Task ListAnyAsync_ReturnsTrueWhenDataExists()
    {
        IServiceProvider sp = Setup();
        CustomerServiceAsync service = sp.GetRequiredService<CustomerServiceAsync>();
        await SeedCustomerAsync(service);

        IBusinessResult<bool> result = await service.ListAnyAsync();

        Assert.True(result.GetDataValue());
    }

    [DockerFact]
    public async Task ListCountAsync_ReturnsCountGreaterThanZero()
    {
        IServiceProvider sp = Setup();
        CustomerServiceAsync service = sp.GetRequiredService<CustomerServiceAsync>();
        await SeedCustomerAsync(service);
        await SeedCustomerAsync(service);

        IBusinessResult<int> result = await service.ListCountAsync();

        Assert.True(result.GetDataValue() >= 2);
    }

    [DockerFact]
    public async Task ListAsync_ReturnsAllCustomers()
    {
        IServiceProvider sp = Setup();
        CustomerServiceAsync service = sp.GetRequiredService<CustomerServiceAsync>();
        await SeedCustomerAsync(service);
        await SeedCustomerAsync(service);
        await SeedCustomerAsync(service);

        IBusinessResult<IList<Customer>> result = await service.ListAsync();

        Assert.True(result.GetDataCount() >= 3);
    }

    [DockerFact]
    public async Task ListAsync_WithPaging_ReturnsLimitedResults()
    {
        IServiceProvider sp = Setup();
        CustomerServiceAsync service = sp.GetRequiredService<CustomerServiceAsync>();
        for (int i = 0; i < 5; i++)
        {
            await SeedCustomerAsync(service);
        }

        var paging = new PagingCriteria(3, 0);
        IBusinessResult<IList<Customer>> result = await service.ListAsync(paging);

        Assert.True(result.HasDataCount(3));
    }

    [DockerFact]
    public async Task ModifyAsync_UpdatesCustomerName()
    {
        IServiceProvider sp = Setup();
        CustomerServiceAsync service = sp.GetRequiredService<CustomerServiceAsync>();

        ObjectId oid = await SeedCustomerAsync(service);
        Customer? customer = (await service.GetByIdAsync(oid)).GetDataValue();
        Assert.NotNull(customer);

        customer.Name = "Updated Name";
        await service.ModifyAsync(customer);

        IBusinessResult<Customer?> updated = await service.GetByIdAsync(oid);
        Assert.Equal("Updated Name", updated.Data?.Name);
    }

    [DockerFact]
    public async Task RemoveByIdAsync_DeletesCustomer()
    {
        IServiceProvider sp = Setup();
        CustomerServiceAsync service = sp.GetRequiredService<CustomerServiceAsync>();

        ObjectId oid = await SeedCustomerAsync(service);
        await service.RemoveByIdAsync(oid);

        IBusinessResult<Customer?> result = await service.GetByIdAsync(oid);
        Assert.False(result.HasData());
    }

    [DockerFact]
    public async Task GetByAsync_FiltersByName()
    {
        IServiceProvider sp = Setup();
        CustomerServiceAsync service = sp.GetRequiredService<CustomerServiceAsync>();
        var oid = ObjectId.GenerateNewId();

        await service.AddAsync(new Customer
        {
            Oid = oid,
            Created = DateTime.Now,
            Name = "UniqueSearchName",
            Active = true
        });

        IBusinessResult<IList<Customer>> result = await service.GetByAsync(x => x.Name == "UniqueSearchName");

        Assert.True(result.HasData());
        Assert.Equal("UniqueSearchName", result.Data?.FirstOrDefault()?.Name);
    }

    [DockerFact]
    public async Task ListAsync_WithOrderBy_ReturnsOrderedResults()
    {
        IServiceProvider sp = Setup();
        CustomerServiceAsync service = sp.GetRequiredService<CustomerServiceAsync>();

        await service.AddAsync(new Customer { Created = DateTime.Now, Name = "Zebra", Active = true });
        await service.AddAsync(new Customer { Created = DateTime.Now, Name = "Alpha", Active = true });
        await service.AddAsync(new Customer { Created = DateTime.Now, Name = "Mango", Active = true });

        var paging = new PagingCriteria(10, 0, ["Name asc"]);
        IBusinessResult<IList<Customer>> result = await service.ListAsync(paging);

        Assert.True(result.HasData());
    }

    [DockerFact]
    public async Task ListAsync_WithExpressionOrder_ReturnsOrderedResults()
    {
        IServiceProvider sp = Setup();
        CustomerServiceAsync service = sp.GetRequiredService<CustomerServiceAsync>();

        await service.AddAsync(new Customer { Created = DateTime.Now, Name = "Z-Customer", Active = true });
        await service.AddAsync(new Customer { Created = DateTime.Now, Name = "A-Customer", Active = true });

        var paging = new PagingCriteriaExpression<Customer>(10, 0);
        paging.OrderByAscendingExpr.Add(x => x.Name);
        IBusinessResult<IList<Customer>> result = await service.ListAsync(paging);

        Assert.True(result.HasData());
    }

    #endregion
}
