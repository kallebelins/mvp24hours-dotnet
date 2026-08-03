using System.Linq.Expressions;
using Mvp24Hours.Application.Contract.Observability;
using Mvp24Hours.Application.Logic.Observability;
using Mvp24Hours.Application.Test.Support;
using Mvp24Hours.Core.ValueObjects.Logic;

namespace Mvp24Hours.Application.Test.Logic.Observability;

[Trait("Category", "Unit")]
public class ObservableApplicationServiceBaseAsyncTest
{
    private static TestObservableApplicationService CreateService(
        Mock<IUnitOfWorkAsync>? unitOfWorkMock = null,
        Mock<IRepositoryAsync<AppTestEntity>>? repositoryMock = null,
        IOperationMetrics? metrics = null,
        IApplicationAuditStore? auditStore = null,
        IValidator<AppTestEntity>? validator = null)
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        unitOfWorkMock ??= uow;
        repositoryMock ??= repo;
        unitOfWorkMock.Setup(u => u.GetRepository<AppTestEntity>()).Returns(repositoryMock.Object);

        var correlationId = new CorrelationIdAccessor();
        correlationId.SetCorrelationId("test-correlation");

        return new TestObservableApplicationService(
            unitOfWorkMock.Object,
            NullLogger<TestObservableApplicationService>.Instance,
            correlationId,
            metrics ?? new ApplicationOperationMetrics(),
            auditStore,
            validator);
    }

    [Fact]
    public async Task ListAnyAsync_ShouldReturnRepositoryResultAndRecordMetrics()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupListAny(repo, true);
        var metrics = new Mock<IOperationMetrics>();
        TestObservableApplicationService service = CreateService(uow, repo, metrics.Object);

        IBusinessResult<bool> result = await service.ListAnyAsync();

        result.Data.Should().BeTrue();
        metrics.Verify(m => m.RecordOperationStart("TestObservableApplicationService", "ListAny", "Query"), Times.Once);
        metrics.Verify(m => m.RecordOperationSuccess(
            "TestObservableApplicationService",
            "ListAny",
            "Query",
            It.IsAny<long>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnEntity()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var entity = new AppTestEntity { Id = 7, Name = "Found" };
        ApplicationTestHelpers.SetupGetById(repo, 7, entity);
        TestObservableApplicationService service = CreateService(uow, repo);

        IBusinessResult<AppTestEntity?> result = await service.GetByIdAsync(7);

        result.Data.Should().BeEquivalentTo(entity);
    }

    [Fact]
    public async Task AddAsync_ValidEntity_ShouldPersistAndAudit()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var auditStore = new InMemoryApplicationAuditStore();
        TestObservableApplicationService service = CreateService(
            uow,
            repo,
            auditStore: auditStore,
            validator: new AppTestEntityValidator());

        IBusinessResult<int> result = await service.AddAsync(new AppTestEntity { Name = "New" });

        result.Data.Should().Be(1);
        repo.Verify(r => r.AddAsync(It.IsAny<AppTestEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        auditStore.GetAll().Should().ContainSingle(e =>
            e.OperationName == "Add" &&
            e.IsSuccess == true &&
            e.CorrelationId == "test-correlation");
    }

    [Fact]
    public async Task AddAsync_InvalidEntity_ShouldReturnValidationErrors()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        TestObservableApplicationService service = CreateService(
            uow,
            repo,
            validator: new AppTestEntityValidator());

        IBusinessResult<int> result = await service.AddAsync(new AppTestEntity { Name = "" });

        result.HasErrors.Should().BeTrue();
        repo.Verify(r => r.AddAsync(It.IsAny<AppTestEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_EmptyBatch_ShouldReturnZeroWithoutRepositoryCall()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        TestObservableApplicationService service = CreateService(uow, repo);

        IBusinessResult<int> result = await service.AddAsync([]);

        result.Data.Should().Be(0);
        repo.Verify(r => r.AddAsync(It.IsAny<AppTestEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveByIdAsync_ShouldRemoveAndSaveChanges()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        TestObservableApplicationService service = CreateService(uow, repo);

        IBusinessResult<int> result = await service.RemoveByIdAsync(42);

        result.Data.Should().Be(1);
        repo.Verify(r => r.RemoveByIdAsync(42, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnyBySpecificationAsync_WithNullSpec_ShouldReturnFalse()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        TestObservableApplicationService service = CreateService(uow, repo);

        IBusinessResult<bool> result = await service.AnyBySpecificationAsync<ActiveAppTestEntitySpec>(null!);

        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task AnyBySpecificationAsync_WithReadOnlyRepository_ShouldUseSpecificationMethod()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupReadOnlySpecification<AppTestEntity, ActiveAppTestEntitySpec>(repo, anyResult: true);
        TestObservableApplicationService service = CreateService(uow, repo);
        var spec = new ActiveAppTestEntitySpec();

        IBusinessResult<bool> result = await service.AnyBySpecificationAsync(spec);

        result.Data.Should().BeTrue();
        repo.As<IReadOnlyRepositoryAsync<AppTestEntity>>()
            .Verify(r => r.AnyBySpecificationAsync(spec, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListAsync_WhenRepositoryThrows_ShouldRecordFailureAndRethrow()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        repo.Setup(r => r.ListAsync(It.IsAny<IPagingCriteria?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db failure"));
        var metrics = new Mock<IOperationMetrics>();
        TestObservableApplicationService service = CreateService(uow, repo, metrics.Object);

        Func<Task> act = () => service.ListAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
        metrics.Verify(m => m.RecordOperationFailure(
            "TestObservableApplicationService",
            "List",
            "Query",
            It.IsAny<long>(),
            nameof(InvalidOperationException)), Times.Once);
    }

    [Fact]
    public async Task AddAsync_WhenAuditStoreFails_ShouldStillCompleteCommand()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var auditStore = new Mock<IApplicationAuditStore>();
        auditStore.Setup(s => s.SaveAsync(It.IsAny<ApplicationAuditEntry>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("audit unavailable"));
        TestObservableApplicationService service = CreateService(
            uow,
            repo,
            auditStore: auditStore.Object,
            validator: new AppTestEntityValidator());

        IBusinessResult<int> result = await service.AddAsync(new AppTestEntity { Name = "Audited" });

        result.Data.Should().Be(1);
    }

    [Fact]
    public async Task ListCountAsync_ShouldReturnRepositoryCountAndRecordMetrics()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupListCount(repo, 5);
        var metrics = new Mock<IOperationMetrics>();
        TestObservableApplicationService service = CreateService(uow, repo, metrics.Object);

        IBusinessResult<int> result = await service.ListCountAsync();

        result.Data.Should().Be(5);
        metrics.Verify(m => m.RecordOperationSuccess(
            "TestObservableApplicationService",
            "ListCount",
            "Query",
            It.IsAny<long>()), Times.Once);
    }

    [Fact]
    public async Task ModifyAsync_ValidEntity_ShouldPersistAndAudit()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var auditStore = new InMemoryApplicationAuditStore();
        TestObservableApplicationService service = CreateService(
            uow,
            repo,
            auditStore: auditStore,
            validator: new AppTestEntityValidator());

        IBusinessResult<int> result = await service.ModifyAsync(new AppTestEntity { Id = 1, Name = "Updated" });

        result.Data.Should().Be(1);
        repo.Verify(r => r.ModifyAsync(It.IsAny<AppTestEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        auditStore.GetAll().Should().ContainSingle(e => e.OperationName == "Modify");
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveEntityAndSaveChanges()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        TestObservableApplicationService service = CreateService(uow, repo);
        var entity = new AppTestEntity { Id = 3, Name = "RemoveMe" };

        IBusinessResult<int> result = await service.RemoveAsync(entity);

        result.Data.Should().Be(1);
        repo.Verify(r => r.RemoveAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetBySpecificationAsync_WithNullSpec_ShouldReturnEmptyList()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        TestObservableApplicationService service = CreateService(uow, repo);

        IBusinessResult<IList<AppTestEntity>> result =
            await service.GetBySpecificationAsync<ActiveAppTestEntitySpec>(null!);

        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task CountBySpecificationAsync_WithReadOnlyRepository_ShouldUseSpecificationMethod()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        Mock<IReadOnlyRepositoryAsync<AppTestEntity>> readOnly = repo.As<IReadOnlyRepositoryAsync<AppTestEntity>>();
        readOnly.Setup(r => r.CountBySpecificationAsync(It.IsAny<ActiveAppTestEntitySpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        TestObservableApplicationService service = CreateService(uow, repo);
        var spec = new ActiveAppTestEntitySpec();

        IBusinessResult<int> result = await service.CountBySpecificationAsync(spec);

        result.Data.Should().Be(3);
    }

    [Fact]
    public async Task AnyBySpecificationAsync_WithoutReadOnlyRepository_ShouldFallbackToExpression()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        repo.Setup(r => r.GetByAnyAsync(It.IsAny<Expression<Func<AppTestEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        TestObservableApplicationService service = CreateService(uow, repo);

        IBusinessResult<bool> result = await service.AnyBySpecificationAsync(new ActiveAppTestEntitySpec());

        result.Data.Should().BeTrue();
        repo.Verify(r => r.GetByAnyAsync(It.IsAny<Expression<Func<AppTestEntity, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_WhenRepositoryThrows_ShouldRecordFailureAuditAndRethrow()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        repo.Setup(r => r.AddAsync(It.IsAny<AppTestEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db failure"));
        var auditStore = new InMemoryApplicationAuditStore();
        var metrics = new Mock<IOperationMetrics>();
        TestObservableApplicationService service = CreateService(
            uow,
            repo,
            metrics.Object,
            auditStore,
            new AppTestEntityValidator());

        Func<Task> act = () => service.AddAsync(new AppTestEntity { Name = "Fail" });

        await act.Should().ThrowAsync<InvalidOperationException>();
        auditStore.GetAll().Should().ContainSingle(e => e.IsSuccess == false);
        metrics.Verify(m => m.RecordOperationFailure(
            "TestObservableApplicationService",
            "Add",
            "Command",
            It.IsAny<long>(),
            nameof(InvalidOperationException)), Times.Once);
    }

    [Fact]
    public async Task AddAsync_BatchValidEntities_ShouldPersistAll()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        TestObservableApplicationService service = CreateService(
            uow,
            repo,
            validator: new AppTestEntityValidator());
        var entities = new List<AppTestEntity>
        {
            new() { Name = "One" },
            new() { Name = "Two" }
        };

        IBusinessResult<int> result = await service.AddAsync(entities);

        result.Data.Should().Be(1);
        repo.Verify(r => r.AddAsync(It.IsAny<AppTestEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task AddAsync_BatchWithInvalidEntity_ShouldReturnValidationErrors()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        TestObservableApplicationService service = CreateService(
            uow,
            repo,
            validator: new AppTestEntityValidator());

        IBusinessResult<int> result = await service.AddAsync([
            new AppTestEntity { Name = "Valid" },
            new AppTestEntity { Name = "" }
        ]);

        result.HasErrors.Should().BeTrue();
        repo.Verify(r => r.AddAsync(It.IsAny<AppTestEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ModifyAsync_InvalidEntity_ShouldReturnValidationErrors()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        TestObservableApplicationService service = CreateService(
            uow,
            repo,
            validator: new AppTestEntityValidator());

        IBusinessResult<int> result = await service.ModifyAsync(new AppTestEntity { Id = 1, Name = "" });

        result.HasErrors.Should().BeTrue();
        repo.Verify(r => r.ModifyAsync(It.IsAny<AppTestEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ModifyAsync_BatchValidEntities_ShouldPersistAll()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        TestObservableApplicationService service = CreateService(
            uow,
            repo,
            validator: new AppTestEntityValidator());

        IBusinessResult<int> result = await service.ModifyAsync([
            new AppTestEntity { Id = 1, Name = "A" },
            new AppTestEntity { Id = 2, Name = "B" }
        ]);

        result.Data.Should().Be(1);
        repo.Verify(r => r.ModifyAsync(It.IsAny<AppTestEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task RemoveAsync_Batch_ShouldRemoveAllEntities()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        TestObservableApplicationService service = CreateService(uow, repo);
        var entities = new List<AppTestEntity>
        {
            new() { Id = 1, Name = "A" },
            new() { Id = 2, Name = "B" }
        };

        IBusinessResult<int> result = await service.RemoveAsync(entities);

        result.Data.Should().Be(1);
        repo.Verify(r => r.RemoveAsync(It.IsAny<AppTestEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task RemoveAsync_EmptyBatch_ShouldReturnZero()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        TestObservableApplicationService service = CreateService(uow, repo);

        IBusinessResult<int> result = await service.RemoveAsync([]);

        result.Data.Should().Be(0);
        repo.Verify(r => r.RemoveAsync(It.IsAny<AppTestEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveByIdAsync_Batch_ShouldRemoveAllIds()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        TestObservableApplicationService service = CreateService(uow, repo);

        IBusinessResult<int> result = await service.RemoveByIdAsync([1, 2]);

        result.Data.Should().Be(1);
        repo.Verify(r => r.RemoveByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetByAnyAsync_ShouldReturnRepositoryResult()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        repo.Setup(r => r.GetByAnyAsync(It.IsAny<Expression<Func<AppTestEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        TestObservableApplicationService service = CreateService(uow, repo);

        IBusinessResult<bool> result = await service.GetByAnyAsync(e => e.Active);

        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task GetByCountAsync_ShouldReturnRepositoryCount()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        repo.Setup(r => r.GetByCountAsync(It.IsAny<Expression<Func<AppTestEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(6);
        TestObservableApplicationService service = CreateService(uow, repo);

        IBusinessResult<int> result = await service.GetByCountAsync(e => e.Active);

        result.Data.Should().Be(6);
    }

    [Fact]
    public async Task GetByAsync_ShouldReturnFilteredEntities()
    {
        var items = new List<AppTestEntity> { new() { Id = 1, Active = true } };
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetByAnyExpression(repo, items);
        TestObservableApplicationService service = CreateService(uow, repo);

        IBusinessResult<IList<AppTestEntity>> result = await service.GetByAsync(e => e.Active);

        result.Data.Should().ContainSingle();
    }

    [Fact]
    public async Task ListAsync_WithPagingCriteria_ShouldReturnEntities()
    {
        var items = new List<AppTestEntity> { new() { Id = 1, Name = "Paged" } };
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupList(repo, items);
        TestObservableApplicationService service = CreateService(uow, repo);
        var criteria = new PagingCriteria(limit: 10, offset: 0);

        IBusinessResult<IList<AppTestEntity>> result = await service.ListAsync(criteria);

        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task CountBySpecificationAsync_WithNullSpec_ShouldReturnZero()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        TestObservableApplicationService service = CreateService(uow, repo);

        IBusinessResult<int> result = await service.CountBySpecificationAsync<ActiveAppTestEntitySpec>(null!);

        result.Data.Should().Be(0);
    }

    [Fact]
    public async Task GetBySpecificationAsync_WithReadOnlyRepository_ShouldUseSpecificationMethod()
    {
        var items = new List<AppTestEntity> { new() { Id = 1, Active = true } };
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        Mock<IReadOnlyRepositoryAsync<AppTestEntity>> readOnly = repo.As<IReadOnlyRepositoryAsync<AppTestEntity>>();
        readOnly.Setup(r => r.GetBySpecificationAsync(It.IsAny<ActiveAppTestEntitySpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);
        TestObservableApplicationService service = CreateService(uow, repo);

        IBusinessResult<IList<AppTestEntity>> result = await service.GetBySpecificationAsync(new ActiveAppTestEntitySpec());

        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetSingleBySpecificationAsync_WithNullSpec_ShouldReturnNull()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        TestObservableApplicationService service = CreateService(uow, repo);

        IBusinessResult<AppTestEntity?> result =
            await service.GetSingleBySpecificationAsync<ActiveAppTestEntitySpec>(null!);

        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task GetSingleBySpecificationAsync_WithoutReadOnlyRepository_ShouldFallbackToExpression()
    {
        var items = new List<AppTestEntity>
        {
            new() { Id = 1, Name = "Only", Active = true }
        };
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetByAnyExpression(repo, items);
        TestObservableApplicationService service = CreateService(uow, repo);

        IBusinessResult<AppTestEntity?> result = await service.GetSingleBySpecificationAsync(new ActiveAppTestEntitySpec());

        result.Data!.Name.Should().Be("Only");
    }

    [Fact]
    public async Task GetFirstBySpecificationAsync_WithReadOnlyRepository_ShouldUseSpecificationMethod()
    {
        var entity = new AppTestEntity { Id = 2, Name = "First", Active = true };
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupReadOnlySpecification<AppTestEntity, ActiveAppTestEntitySpec>(repo, firstResult: entity);
        TestObservableApplicationService service = CreateService(uow, repo);

        IBusinessResult<AppTestEntity?> result = await service.GetFirstBySpecificationAsync(new ActiveAppTestEntitySpec());

        result.Data!.Name.Should().Be("First");
    }

    [Fact]
    public async Task GetFirstBySpecificationAsync_WithNullSpec_ShouldReturnNull()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        TestObservableApplicationService service = CreateService(uow, repo);

        IBusinessResult<AppTestEntity?> result =
            await service.GetFirstBySpecificationAsync<ActiveAppTestEntitySpec>(null!);

        result.Data.Should().BeNull();
    }
}
