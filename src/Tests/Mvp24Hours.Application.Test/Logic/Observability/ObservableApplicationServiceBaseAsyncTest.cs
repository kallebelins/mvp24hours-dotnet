using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Application.Contract.Observability;
using Mvp24Hours.Application.Logic.Observability;
using Mvp24Hours.Application.Test.Support;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;

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
}
