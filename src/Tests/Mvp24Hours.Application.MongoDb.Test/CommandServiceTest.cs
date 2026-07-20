//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Mvp24Hours.Application.MongoDb.Test.Support;
using Mvp24Hours.Application.MongoDb.Test.Support.Entities;
using Mvp24Hours.Application.MongoDb.Test.Support.Services;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
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
public class CommandServiceTest(MongoDbIntegrationFixture fixture)
{
    #region [ Fields ]
    private ObjectId oid;
    #endregion

    #region [ Configure ]
    private IServiceProvider Setup()
    {
        var services = new ServiceCollection();
        services.AddMvp24HoursDbContext(options =>
        {
            options.DatabaseName = $"commandservicetest_{Guid.NewGuid():N}";
            options.ConnectionString = fixture.ConnectionString;
        });
        services.AddMvp24HoursRepository(repositoryOptions: null);
        services.AddScoped<CustomerService, CustomerService>();
        oid = ObjectId.GenerateNewId();
        return services.BuildServiceProvider();
    }
    #endregion

    #region [ Facts ]
    [DockerFact]
    public void CreateCustomer()
    {
        IServiceProvider serviceProvider = Setup();
        CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();

        service.Add(new Customer
        {
            Oid = oid,
            Created = DateTime.Now,
            Name = "Test 1",
            Active = true
        });

        IBusinessResult<Customer?> result = service.GetById(oid);

        Assert.True(result.HasData());
    }

    [DockerFact]
    public void UpdateCustomer()
    {
        IServiceProvider serviceProvider = Setup();
        CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();

        service.Add(new Customer
        {
            Oid = oid,
            Created = DateTime.Now,
            Name = "Test 1",
            Active = true
        });

        Customer? customer = service.GetById(oid).GetDataValue();
        Assert.NotNull(customer);

        customer.Name = "Test Updated";
        service.Modify(customer);

        IBusinessResult<Customer?> boCustomer = service.GetById(oid);

        Assert.True(boCustomer != null && boCustomer.Data?.Name == "Test Updated");
    }

    [DockerFact]
    public void DeleteCustomer()
    {
        IServiceProvider serviceProvider = Setup();
        CustomerService? service = serviceProvider.GetRequiredService<CustomerService>();

        service.Add(new Customer
        {
            Oid = oid,
            Created = DateTime.Now,
            Name = "Test 1",
            Active = true
        });

        service.RemoveById(oid);

        IBusinessResult<Customer?> result = service.GetById(oid);

        Assert.False(result.HasData());
    }
    #endregion
}
