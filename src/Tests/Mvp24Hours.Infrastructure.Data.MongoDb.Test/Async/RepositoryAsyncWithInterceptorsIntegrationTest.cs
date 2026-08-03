using MongoDB.Bson;
using MongoDB.Driver;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Core.Infrastructure.Clock;
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
}
