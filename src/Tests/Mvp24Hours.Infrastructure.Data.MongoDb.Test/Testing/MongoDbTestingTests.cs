//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Bson;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Data.MongoDb.Testing;
using Xunit;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Testing;

/// <summary>
/// Tests for MongoDB Testing utilities.
/// </summary>
public class MongoDbTestingTests
{
    #region Test Entity

    public class TestCustomer : IEntityBase
    {
        public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
        public string Name { get; set; } = string.Empty;
        public bool Active { get; set; } = true;

        public object EntityKey => Id;
        public IReadOnlyCollection<MessageResult> GetNotifications() => new List<MessageResult>();
        public bool HasNotifications() => false;
    }

    #endregion

    #region MongoDbInMemoryProvider Tests

    [Fact]
    public void MongoDbInMemoryProvider_ShouldCreateProvider()
    {
        // Arrange & Act
        using var provider = new MongoDbInMemoryProvider();

        // Assert
        Assert.NotNull(provider);
        Assert.NotNull(provider.DatabaseName);
    }

    [Fact]
    public void MongoDbInMemoryProvider_ShouldCreateProviderWithOptions()
    {
        // Arrange
        var options = new MongoDbInMemoryOptions
        {
            DatabaseNamePrefix = "TestDb",
            UseUniqueDatabaseName = false
        };

        // Act
        using var provider = new MongoDbInMemoryProvider(options);

        // Assert
        Assert.NotNull(provider);
        Assert.Equal("TestDb", provider.DatabaseName);
    }

    [Fact]
    public void MongoDbInMemoryProvider_ShouldGetCollection()
    {
        // Arrange
        using var provider = new MongoDbInMemoryProvider();

        // Act
        var collection = provider.GetCollection<TestCustomer>();

        // Assert
        Assert.NotNull(collection);
        Assert.Equal("TestCustomer", collection.CollectionName);
    }

    [Fact]
    public void InMemoryMongoCollection_ShouldInsertAndFindDocument()
    {
        // Arrange
        using var provider = new MongoDbInMemoryProvider();
        var collection = provider.GetCollection<TestCustomer>();
        var customer = new TestCustomer { Name = "Test Customer" };

        // Act
        collection.InsertOne(customer);
        var found = collection.FindById(customer.Id);

        // Assert
        Assert.NotNull(found);
        Assert.Equal("Test Customer", found.Name);
    }

    [Fact]
    public void InMemoryMongoCollection_ShouldInsertManyDocuments()
    {
        // Arrange
        using var provider = new MongoDbInMemoryProvider();
        var collection = provider.GetCollection<TestCustomer>();
        var customers = new[]
        {
            new TestCustomer { Name = "Customer 1" },
            new TestCustomer { Name = "Customer 2" },
            new TestCustomer { Name = "Customer 3" }
        };

        // Act
        collection.InsertMany(customers);

        // Assert
        Assert.Equal(3, collection.Count);
    }

    [Fact]
    public void InMemoryMongoCollection_ShouldReplaceDocument()
    {
        // Arrange
        using var provider = new MongoDbInMemoryProvider();
        var collection = provider.GetCollection<TestCustomer>();
        var customer = new TestCustomer { Name = "Original Name" };
        collection.InsertOne(customer);

        // Act
        customer.Name = "Updated Name";
        var replaced = collection.ReplaceOne(customer);
        var found = collection.FindById(customer.Id);

        // Assert
        Assert.True(replaced);
        Assert.NotNull(found);
        Assert.Equal("Updated Name", found.Name);
    }

    [Fact]
    public void InMemoryMongoCollection_ShouldDeleteDocument()
    {
        // Arrange
        using var provider = new MongoDbInMemoryProvider();
        var collection = provider.GetCollection<TestCustomer>();
        var customer = new TestCustomer { Name = "To Delete" };
        collection.InsertOne(customer);

        // Act
        var deleted = collection.DeleteOne(customer);
        var found = collection.FindById(customer.Id);

        // Assert
        Assert.True(deleted);
        Assert.Null(found);
    }

    [Fact]
    public void InMemoryMongoCollection_ShouldFindByPredicate()
    {
        // Arrange
        using var provider = new MongoDbInMemoryProvider();
        var collection = provider.GetCollection<TestCustomer>();
        collection.InsertMany(new[]
        {
            new TestCustomer { Name = "Active 1", Active = true },
            new TestCustomer { Name = "Active 2", Active = true },
            new TestCustomer { Name = "Inactive", Active = false }
        });

        // Act
        var activeCustomers = collection.Find(c => c.Active);

        // Assert
        Assert.Equal(2, activeCustomers.Count);
    }

    [Fact]
    public void InMemoryMongoCollection_ShouldCountDocuments()
    {
        // Arrange
        using var provider = new MongoDbInMemoryProvider();
        var collection = provider.GetCollection<TestCustomer>();
        collection.InsertMany(new[]
        {
            new TestCustomer { Active = true },
            new TestCustomer { Active = true },
            new TestCustomer { Active = false }
        });

        // Act
        var totalCount = collection.CountDocuments();
        var activeCount = collection.CountDocuments(c => c.Active);

        // Assert
        Assert.Equal(3, totalCount);
        Assert.Equal(2, activeCount);
    }

    [Fact]
    public void InMemoryMongoCollection_ShouldClearDocuments()
    {
        // Arrange
        using var provider = new MongoDbInMemoryProvider();
        var collection = provider.GetCollection<TestCustomer>();
        collection.InsertMany(new[]
        {
            new TestCustomer(),
            new TestCustomer(),
            new TestCustomer()
        });

        // Act
        collection.Clear();

        // Assert
        Assert.Equal(0, collection.Count);
    }

    #endregion

    #region MongoRepositoryFake Tests

    [Fact]
    public void MongoRepositoryFake_ShouldCreateRepository()
    {
        // Arrange & Act
        using var repository = new MongoRepositoryFake<TestCustomer>();

        // Assert
        Assert.NotNull(repository);
        Assert.Empty(repository.AllEntities);
    }

    [Fact]
    public void MongoRepositoryFake_ShouldAddAndCommit()
    {
        // Arrange
        using var repository = new MongoRepositoryFake<TestCustomer>();
        var customer = new TestCustomer { Name = "Test" };

        // Act
        repository.Add(customer);
        var changes = repository.CommitChanges();

        // Assert
        Assert.Equal(1, changes);
        Assert.Single(repository.AllEntities);
    }

    [Fact]
    public void MongoRepositoryFake_ShouldModifyAndCommit()
    {
        // Arrange
        using var repository = new MongoRepositoryFake<TestCustomer>();
        var customer = new TestCustomer { Name = "Original" };
        repository.Add(customer);
        repository.CommitChanges();

        // Act
        customer.Name = "Modified";
        repository.Modify(customer);
        var changes = repository.CommitChanges();
        var found = repository.GetById(customer.Id);

        // Assert
        Assert.Equal(1, changes);
        Assert.NotNull(found);
        Assert.Equal("Modified", found.Name);
    }

    [Fact]
    public void MongoRepositoryFake_ShouldRemoveAndCommit()
    {
        // Arrange
        using var repository = new MongoRepositoryFake<TestCustomer>();
        var customer = new TestCustomer { Name = "To Delete" };
        repository.Add(customer);
        repository.CommitChanges();

        // Act
        repository.Remove(customer);
        var changes = repository.CommitChanges();

        // Assert
        Assert.Equal(1, changes);
        Assert.Empty(repository.AllEntities);
    }

    [Fact]
    public void MongoRepositoryFake_ShouldGetByClause()
    {
        // Arrange
        using var repository = new MongoRepositoryFake<TestCustomer>();
        repository.SeedData(new[]
        {
            new TestCustomer { Name = "Active 1", Active = true },
            new TestCustomer { Name = "Active 2", Active = true },
            new TestCustomer { Name = "Inactive", Active = false }
        });

        // Act
        var activeCustomers = repository.GetBy(c => c.Active);

        // Assert
        Assert.Equal(2, activeCustomers.Count);
    }

    [Fact]
    public void MongoRepositoryFake_ShouldListAny()
    {
        // Arrange
        using var repository = new MongoRepositoryFake<TestCustomer>();

        // Act & Assert
        Assert.False(repository.ListAny());

        repository.SeedData(new[] { new TestCustomer() });
        Assert.True(repository.ListAny());
    }

    [Fact]
    public void MongoRepositoryFake_ShouldListCount()
    {
        // Arrange
        using var repository = new MongoRepositoryFake<TestCustomer>();
        repository.SeedData(new[]
        {
            new TestCustomer(),
            new TestCustomer(),
            new TestCustomer()
        });

        // Act
        var count = repository.ListCount();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public void MongoRepositoryFake_ShouldResetPendingChanges()
    {
        // Arrange
        using var repository = new MongoRepositoryFake<TestCustomer>();
        repository.Add(new TestCustomer { Name = "Test" });

        // Act
        repository.ResetPendingChanges();
        repository.CommitChanges();

        // Assert
        Assert.Empty(repository.AllEntities);
    }

    [Fact]
    public void MongoRepositoryFake_ShouldSeedDataWithAction()
    {
        // Arrange
        using var repository = new MongoRepositoryFake<TestCustomer>();

        // Act
        repository.SeedData(list =>
        {
            for (int i = 1; i <= 5; i++)
            {
                list.Add(new TestCustomer { Name = $"Customer {i}" });
            }
        });

        // Assert
        Assert.Equal(5, repository.ListCount());
    }

    #endregion

    #region MongoRepositoryFakeAsync Tests

    [Fact]
    public async Task MongoRepositoryFakeAsync_ShouldAddAndCommitAsync()
    {
        // Arrange
        using var repository = new MongoRepositoryFakeAsync<TestCustomer>();
        var customer = new TestCustomer { Name = "Test" };

        // Act
        await repository.AddAsync(customer);
        var changes = await repository.CommitChangesAsync();

        // Assert
        Assert.Equal(1, changes);
        Assert.Single(repository.AllEntities);
    }

    [Fact]
    public async Task MongoRepositoryFakeAsync_ShouldGetByIdAsync()
    {
        // Arrange
        using var repository = new MongoRepositoryFakeAsync<TestCustomer>();
        var customer = new TestCustomer { Name = "Test" };
        repository.SeedData(new[] { customer });

        // Act
        var found = await repository.GetByIdAsync(customer.Id);

        // Assert
        Assert.NotNull(found);
        Assert.Equal("Test", found.Name);
    }

    [Fact]
    public async Task MongoRepositoryFakeAsync_ShouldListAsync()
    {
        // Arrange
        using var repository = new MongoRepositoryFakeAsync<TestCustomer>();
        repository.SeedData(new[]
        {
            new TestCustomer { Name = "Customer 1" },
            new TestCustomer { Name = "Customer 2" }
        });

        // Act
        var list = await repository.ListAsync();

        // Assert
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task MongoRepositoryFakeAsync_ShouldGetByAsync()
    {
        // Arrange
        using var repository = new MongoRepositoryFakeAsync<TestCustomer>();
        repository.SeedData(new[]
        {
            new TestCustomer { Name = "Active", Active = true },
            new TestCustomer { Name = "Inactive", Active = false }
        });

        // Act
        var activeCustomers = await repository.GetByAsync(c => c.Active);

        // Assert
        Assert.Single(activeCustomers);
        Assert.Equal("Active", activeCustomers[0].Name);
    }

    #endregion

    #region MongoUnitOfWorkFake Tests

    [Fact]
    public void MongoUnitOfWorkFake_ShouldGetRepository()
    {
        // Arrange
        using var unitOfWork = new MongoUnitOfWorkFake();

        // Act
        var repository = unitOfWork.GetRepository<TestCustomer>();

        // Assert
        Assert.NotNull(repository);
    }

    [Fact]
    public void MongoUnitOfWorkFake_ShouldSaveChanges()
    {
        // Arrange
        using var unitOfWork = new MongoUnitOfWorkFake();
        var repository = unitOfWork.GetRepository<TestCustomer>();
        repository.Add(new TestCustomer { Name = "Test" });

        // Act
        var changes = unitOfWork.SaveChanges();

        // Assert
        Assert.Equal(1, changes);
    }

    [Fact]
    public void MongoUnitOfWorkFake_ShouldRollback()
    {
        // Arrange
        using var unitOfWork = new MongoUnitOfWorkFake();
        var repository = unitOfWork.GetRepository<TestCustomer>();
        repository.Add(new TestCustomer { Name = "Test" });

        // Act
        unitOfWork.Rollback();
        unitOfWork.SaveChanges();

        // Assert - No entities should have been committed
        Assert.Empty(((MongoRepositoryFake<TestCustomer>)repository).AllEntities);
    }

    #endregion

    #region MongoUnitOfWorkFakeAsync Tests

    [Fact]
    public async Task MongoUnitOfWorkFakeAsync_ShouldGetRepository()
    {
        // Arrange
        using var unitOfWork = new MongoUnitOfWorkFakeAsync();

        // Act
        var repository = unitOfWork.GetRepository<TestCustomer>();

        // Assert
        Assert.NotNull(repository);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task MongoUnitOfWorkFakeAsync_ShouldSaveChangesAsync()
    {
        // Arrange
        using var unitOfWork = new MongoUnitOfWorkFakeAsync();
        var repository = unitOfWork.GetRepository<TestCustomer>();
        await repository.AddAsync(new TestCustomer { Name = "Test" });

        // Act
        var changes = await unitOfWork.SaveChangesAsync();

        // Assert
        Assert.Equal(1, changes);
    }

    #endregion

    #region IMongoDataSeeder Tests

    public class TestCustomerSeeder : IMongoDataSeeder
    {
        public void Seed(Mvp24HoursContext context)
        {
            // In real scenario, would insert to context.Set<TestCustomer>()
            // For this test, we just verify the interface works
        }
    }

    [Fact]
    public void CompositeMongoDataSeeder_ShouldCallAllSeeders()
    {
        // Arrange
        var callCount = 0;
        var seeder1 = new ActionMongoDataSeeder(_ => callCount++);
        var seeder2 = new ActionMongoDataSeeder(_ => callCount++);
        var seeder3 = new ActionMongoDataSeeder(_ => callCount++);
        var composite = new CompositeMongoDataSeeder(seeder1, seeder2, seeder3);

        // Act
        composite.Seed(null!); // Context not used in test seeders

        // Assert
        Assert.Equal(3, callCount);
    }

    [Fact]
    public void ActionMongoDataSeeder_ShouldExecuteAction()
    {
        // Arrange
        var executed = false;
        var seeder = new ActionMongoDataSeeder(_ => executed = true);

        // Act
        seeder.Seed(null!); // Context not used in test

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public async Task ActionMongoDataSeederAsync_ShouldExecuteActionAsync()
    {
        // Arrange
        var executed = false;
        var seeder = new ActionMongoDataSeederAsync(async (_, _) =>
        {
            await Task.Delay(1);
            executed = true;
        });

        // Act
        await seeder.SeedAsync(null!); // Context not used in test

        // Assert
        Assert.True(executed);
    }

    #endregion

    #region MongoDbInMemoryOptions Tests

    [Fact]
    public void MongoDbInMemoryOptions_ShouldGetEffectiveDatabaseName_WithUniqueName()
    {
        // Arrange
        var options = new MongoDbInMemoryOptions
        {
            DatabaseNamePrefix = "TestDb",
            UseUniqueDatabaseName = true
        };

        // Act
        var name1 = options.GetEffectiveDatabaseName();
        var name2 = options.GetEffectiveDatabaseName();

        // Assert
        Assert.StartsWith("TestDb_", name1);
        Assert.NotEqual(name1, name2); // Each call generates unique name
    }

    [Fact]
    public void MongoDbInMemoryOptions_ShouldGetEffectiveDatabaseName_WithFixedName()
    {
        // Arrange
        var options = new MongoDbInMemoryOptions
        {
            DatabaseName = "FixedDb",
            UseUniqueDatabaseName = false
        };

        // Act
        var name1 = options.GetEffectiveDatabaseName();
        var name2 = options.GetEffectiveDatabaseName();

        // Assert
        Assert.Equal("FixedDb", name1);
        Assert.Equal(name1, name2);
    }

    [Fact]
    public void MongoDbInMemoryOptions_ForUnitTesting_ShouldReturnCorrectOptions()
    {
        // Act
        var options = MongoDbInMemoryOptions.ForUnitTesting();

        // Assert
        Assert.True(options.UseUniqueDatabaseName);
        Assert.False(options.EnableLogging);
        Assert.False(options.EnableTransaction);
        Assert.Equal(5, options.TimeoutSeconds);
    }

    [Fact]
    public void MongoDbInMemoryOptions_ForIntegrationTesting_ShouldReturnCorrectOptions()
    {
        // Act
        var options = MongoDbInMemoryOptions.ForIntegrationTesting();

        // Assert
        Assert.True(options.UseUniqueDatabaseName);
        Assert.True(options.EnableLogging);
        Assert.Equal(60, options.TimeoutSeconds);
    }

    #endregion

    #region MongoDbTestcontainersOptions Tests

    [Fact]
    public void MongoDbTestcontainersOptions_ShouldGetImageName()
    {
        // Arrange
        var options = new MongoDbTestcontainersOptions
        {
            ImageTag = "6.0"
        };

        // Act
        var imageName = options.GetImageName();

        // Assert
        Assert.Equal("mongo:6.0", imageName);
    }

    [Fact]
    public void MongoDbTestcontainersOptions_ForBasicTesting_ShouldReturnCorrectOptions()
    {
        // Act
        var options = MongoDbTestcontainersOptions.ForBasicTesting();

        // Assert
        Assert.Equal("6.0", options.ImageTag);
        Assert.True(options.UseUniqueDatabaseName);
        Assert.True(options.AutoRemove);
        Assert.Null(options.Username);
        Assert.Null(options.Password);
    }

    [Fact]
    public void MongoDbTestcontainersOptions_ForAuthenticatedTesting_ShouldReturnCorrectOptions()
    {
        // Act
        var options = MongoDbTestcontainersOptions.ForAuthenticatedTesting("admin", "password123");

        // Assert
        Assert.Equal("admin", options.Username);
        Assert.Equal("password123", options.Password);
    }

    [Fact]
    public void MongoDbTestcontainersOptions_ForReplicaSetTesting_ShouldReturnCorrectOptions()
    {
        // Act
        var options = MongoDbTestcontainersOptions.ForReplicaSetTesting();

        // Assert
        Assert.True(options.EnableReplicaSet);
    }

    #endregion
}

