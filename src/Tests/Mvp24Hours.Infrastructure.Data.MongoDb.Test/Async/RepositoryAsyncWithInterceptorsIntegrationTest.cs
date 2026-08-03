using MongoDB.Bson;
using MongoDB.Driver;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Core.Infrastructure.Clock;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Data.MongoDb;
using Mvp24Hours.Infrastructure.Data.MongoDb.Interceptors;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Async;

[Trait("Category", "Integration")]
[Collection(MongoDbIntegrationCollection.Name)]
public class RepositoryAsyncWithInterceptorsIntegrationTest(MongoDbIntegrationFixture fixture)
{
    private const string TenantId = "tenant-integration";
    private static readonly DateTime FixedTime = new(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);

    private RepositoryAsyncWithInterceptors<AuditableTenantProduct> CreateRepository()
    {
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture);
        var clock = new TestClock(FixedTime);
        ICurrentUserProvider userProvider = new SystemUserProvider("integration-user", "Integration User");

        IMongoDbInterceptor[] interceptors =
        [
            new TenantInterceptor(new FakeTenantProvider(TenantId)),
            new AuditInterceptor(userProvider, clock, defaultUser: "System"),
            new SoftDeleteInterceptor(userProvider, clock, defaultUser: "System")
        ];
        var pipeline = new MongoDbInterceptorPipeline(interceptors);

        return new RepositoryAsyncWithInterceptors<AuditableTenantProduct>(
            context,
            MongoDbIntegrationTestHelper.CreateRepositoryOptions(),
            pipeline);
    }

    private async Task CleanupAsync()
    {
        IMongoCollection<AuditableTenantProduct> collection = fixture.GetCollection<AuditableTenantProduct>();
        await collection.DeleteManyAsync(FilterDefinition<AuditableTenantProduct>.Empty);
    }

    [DockerFact]
    public async Task AddAsync_ShouldApplyAuditTenantAndPersistEntity()
    {
        await CleanupAsync();
        RepositoryAsyncWithInterceptors<AuditableTenantProduct> repository = CreateRepository();
        var entity = new AuditableTenantProduct { Name = "Widget" };

        await repository.AddAsync(entity);

        AuditableTenantProduct? stored = await repository.GetByIdAsync(entity.Id);
        stored.Should().NotBeNull();
        stored!.TenantId.Should().Be(TenantId);
        stored.Name.Should().Be("Widget");
        stored.CreatedAt.Should().Be(FixedTime);
        stored.CreatedBy.Should().Be("integration-user");
        stored.IsDeleted.Should().BeFalse();
    }

    [DockerFact]
    public async Task ModifyAsync_ShouldApplyAuditFieldsOnUpdate()
    {
        await CleanupAsync();
        RepositoryAsyncWithInterceptors<AuditableTenantProduct> repository = CreateRepository();
        var entity = new AuditableTenantProduct { Name = "Before" };
        await repository.AddAsync(entity);

        entity.Name = "After";
        await repository.ModifyAsync(entity);

        AuditableTenantProduct? updated = await repository.GetByIdAsync(entity.Id);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("After");
        updated.ModifiedAt.Should().Be(FixedTime);
        updated.ModifiedBy.Should().Be("integration-user");
        updated.CreatedAt.Should().Be(FixedTime);
        updated.CreatedBy.Should().Be("integration-user");
    }

    [DockerFact]
    public async Task RemoveAsync_ShouldSoftDeleteInsteadOfPhysicalDelete()
    {
        await CleanupAsync();
        RepositoryAsyncWithInterceptors<AuditableTenantProduct> repository = CreateRepository();
        var entity = new AuditableTenantProduct { Name = "ToDelete" };
        await repository.AddAsync(entity);

        await repository.RemoveAsync(entity);

        IMongoCollection<AuditableTenantProduct> collection = fixture.GetCollection<AuditableTenantProduct>();
        AuditableTenantProduct stored = (await collection.Find(FilterDefinition<AuditableTenantProduct>.Empty).ToListAsync()).Single();
        stored.IsDeleted.Should().BeTrue();
        stored.DeletedAt.Should().Be(FixedTime);
        stored.DeletedBy.Should().Be("integration-user");

        (await repository.ListCountAsync()).Should().Be(1);
    }

    [DockerFact]
    public async Task AddAsync_WithWrongTenantOnUpdate_ShouldThrowUnauthorizedAccessException()
    {
        await CleanupAsync();
        RepositoryAsyncWithInterceptors<AuditableTenantProduct> repository = CreateRepository();
        var entity = new AuditableTenantProduct { Name = "CrossTenant" };
        await repository.AddAsync(entity);

        entity.TenantId = "other-tenant";

        Func<Task> act = () => repository.ModifyAsync(entity);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private async Task CleanupTestEntitiesAsync()
    {
        IMongoCollection<TestEntity> collection = fixture.GetCollection<TestEntity>();
        await collection.DeleteManyAsync(FilterDefinition<TestEntity>.Empty);
    }

    [DockerFact]
    public async Task ModifyAsync_WhenEntityNotFound_ShouldThrowInvalidOperationException()
    {
        await CleanupTestEntitiesAsync();
        RepositoryAsyncWithInterceptors<TestEntity> repository = CreateRepositoryWithoutSoftDelete();
        var entity = new TestEntity { Name = "Missing" };

        Func<Task> act = () => repository.ModifyAsync(entity);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Key value not found*");
    }

    [DockerFact]
    public async Task RemoveByIdAsync_ShouldRemoveExistingEntity()
    {
        await CleanupTestEntitiesAsync();
        RepositoryAsyncWithInterceptors<TestEntity> repository = CreateRepositoryWithoutSoftDelete();
        var entity = new TestEntity { Name = "RemoveById" };
        await repository.AddAsync(entity);

        await repository.RemoveByIdAsync(entity.Id);

        (await repository.ListCountAsync()).Should().Be(0);
    }

    [DockerFact]
    public async Task ListAsync_WithPagingCriteria_ShouldReturnSubset()
    {
        await CleanupTestEntitiesAsync();
        RepositoryAsyncWithInterceptors<TestEntity> repository = CreateRepositoryWithoutSoftDelete();
        await repository.AddAsync(new TestEntity { Name = "A" });
        await repository.AddAsync(new TestEntity { Name = "B" });
        await repository.AddAsync(new TestEntity { Name = "C" });
        var paging = new PagingCriteria(limit: 2, offset: 0);

        IList<TestEntity> page = await repository.ListAsync(paging);

        page.Should().HaveCount(2);
    }

    [DockerFact]
    public async Task GetByAsync_WithNullClause_ShouldReturnAllEntities()
    {
        await CleanupTestEntitiesAsync();
        RepositoryAsyncWithInterceptors<TestEntity> repository = CreateRepositoryWithoutSoftDelete();
        await repository.AddAsync(new TestEntity { Name = "One" });
        await repository.AddAsync(new TestEntity { Name = "Two" });

        IList<TestEntity> results = await repository.GetByAsync(null!);

        results.Should().HaveCount(2);
    }

    [DockerFact]
    public async Task RemoveAsync_WithHardDeletePipeline_ShouldPhysicallyDelete()
    {
        await CleanupTestEntitiesAsync();
        RepositoryAsyncWithInterceptors<TestEntity> repository = CreateRepositoryWithoutSoftDelete();
        var entity = new TestEntity { Name = "HardDelete" };
        await repository.AddAsync(entity);

        await repository.RemoveAsync(entity);

        (await repository.ListCountAsync()).Should().Be(0);
        IMongoCollection<TestEntity> collection = fixture.GetCollection<TestEntity>();
        long count = await collection.CountDocumentsAsync(FilterDefinition<TestEntity>.Empty);
        count.Should().Be(0);
    }

    [DockerFact]
    public async Task ListAnyAsync_ShouldReturnTrueWhenEntitiesExist()
    {
        await CleanupTestEntitiesAsync();
        RepositoryAsyncWithInterceptors<TestEntity> repository = CreateRepositoryWithoutSoftDelete();
        await repository.AddAsync(new TestEntity { Name = "Any" });

        bool any = await repository.ListAnyAsync();

        any.Should().BeTrue();
    }

    [DockerFact]
    public async Task GetByAnyAsync_WithMatchingClause_ShouldReturnTrue()
    {
        await CleanupTestEntitiesAsync();
        RepositoryAsyncWithInterceptors<TestEntity> repository = CreateRepositoryWithoutSoftDelete();
        await repository.AddAsync(new TestEntity { Name = "Match" });
        await repository.AddAsync(new TestEntity { Name = "Other" });

        bool any = await repository.GetByAnyAsync(e => e.Name == "Match");

        any.Should().BeTrue();
    }

    [DockerFact]
    public async Task GetByCountAsync_WithMatchingClause_ShouldReturnCount()
    {
        await CleanupTestEntitiesAsync();
        RepositoryAsyncWithInterceptors<TestEntity> repository = CreateRepositoryWithoutSoftDelete();
        await repository.AddAsync(new TestEntity { Name = "Counted" });
        await repository.AddAsync(new TestEntity { Name = "Counted" });
        await repository.AddAsync(new TestEntity { Name = "Other" });

        int count = await repository.GetByCountAsync(e => e.Name == "Counted");

        count.Should().Be(2);
    }

    [DockerFact]
    public async Task AddAsync_WithMultipleEntities_ShouldPersistAll()
    {
        await CleanupTestEntitiesAsync();
        RepositoryAsyncWithInterceptors<TestEntity> repository = CreateRepositoryWithoutSoftDelete();
        var entities = new List<TestEntity>
        {
            new() { Name = "Batch-1" },
            new() { Name = "Batch-2" }
        };

        await repository.AddAsync(entities);

        (await repository.ListCountAsync()).Should().Be(2);
    }

    [DockerFact]
    public async Task ModifyAsync_WithMultipleEntities_ShouldUpdateAll()
    {
        await CleanupTestEntitiesAsync();
        RepositoryAsyncWithInterceptors<TestEntity> repository = CreateRepositoryWithoutSoftDelete();
        var first = new TestEntity { Name = "Modify-1" };
        var second = new TestEntity { Name = "Modify-2" };
        await repository.AddAsync(first);
        await repository.AddAsync(second);
        first.Name = "Updated-1";
        second.Name = "Updated-2";

        await repository.ModifyAsync([first, second]);

        IList<TestEntity> stored = await repository.GetByAsync(e => e.Name == "Updated-1" || e.Name == "Updated-2");
        stored.Should().HaveCount(2);
    }

    [DockerFact]
    public async Task RemoveAsync_WithMultipleEntities_ShouldRemoveAll()
    {
        await CleanupTestEntitiesAsync();
        RepositoryAsyncWithInterceptors<TestEntity> repository = CreateRepositoryWithoutSoftDelete();
        var first = new TestEntity { Name = "Remove-1" };
        var second = new TestEntity { Name = "Remove-2" };
        await repository.AddAsync(first);
        await repository.AddAsync(second);

        await repository.RemoveAsync([first, second]);

        (await repository.ListCountAsync()).Should().Be(0);
    }

    [DockerFact]
    public async Task RemoveByIdAsync_WithMultipleIds_ShouldRemoveAll()
    {
        await CleanupTestEntitiesAsync();
        RepositoryAsyncWithInterceptors<TestEntity> repository = CreateRepositoryWithoutSoftDelete();
        var first = new TestEntity { Name = "Id-1" };
        var second = new TestEntity { Name = "Id-2" };
        await repository.AddAsync(first);
        await repository.AddAsync(second);

        await repository.RemoveByIdAsync([first.Id, second.Id]);

        (await repository.ListCountAsync()).Should().Be(0);
    }

    [DockerFact]
    public async Task RemoveByIdAsync_WhenEntityMissing_ShouldNotThrow()
    {
        await CleanupTestEntitiesAsync();
        RepositoryAsyncWithInterceptors<TestEntity> repository = CreateRepositoryWithoutSoftDelete();

        Func<Task> act = () => repository.RemoveByIdAsync(ObjectId.GenerateNewId());

        await act.Should().NotThrowAsync();
    }

    private RepositoryAsyncWithInterceptors<TestEntity> CreateRepositoryWithoutSoftDelete()
    {
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture);
        return new RepositoryAsyncWithInterceptors<TestEntity>(
            context,
            MongoDbIntegrationTestHelper.CreateRepositoryOptions(),
            new MongoDbInterceptorPipeline([]));
    }
}
