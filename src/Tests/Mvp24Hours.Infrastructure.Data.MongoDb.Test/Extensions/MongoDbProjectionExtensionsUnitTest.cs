using MongoDB.Driver;
using Moq;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Projections;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Extensions;

[Trait("Category", "Unit")]
public class MongoDbProjectionExtensionsUnitTest
{
    [Fact]
    public void ProjectInclude_ShouldReturnProjectionDefinition()
    {
        var collectionMock = new Mock<IMongoCollection<TestEntity>>();

        ProjectionDefinition<TestEntity> projection = collectionMock.Object.ProjectInclude(e => e.Name);

        projection.Should().NotBeNull();
    }

    [Fact]
    public void ProjectExclude_ShouldReturnProjectionDefinition()
    {
        var collectionMock = new Mock<IMongoCollection<TestEntity>>();

        ProjectionDefinition<TestEntity> projection = collectionMock.Object.ProjectExclude(e => e.Name);

        projection.Should().NotBeNull();
    }

    [Fact]
    public async Task FindProjectedAsync_ShouldReturnProjectedResults()
    {
        var collectionMock = new Mock<IMongoCollection<TestEntity>>();
        var projected = new List<ProjectedName> { new() { Name = "Alpha" } };
        collectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<TestEntity>>(),
                It.IsAny<FindOptions<TestEntity, ProjectedName>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FakeAsyncCursor<ProjectedName>(projected));

        List<ProjectedName> results = await collectionMock.Object.FindProjectedAsync(
            e => e.Name != null,
            e => new ProjectedName { Name = e.Name });

        results.Should().ContainSingle().Which.Name.Should().Be("Alpha");
    }

    [Fact]
    public async Task FindOneProjectedAsync_ShouldReturnFirstProjectedResult()
    {
        var collectionMock = new Mock<IMongoCollection<TestEntity>>();
        var projected = new List<ProjectedName> { new() { Name = "First" } };
        collectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<TestEntity>>(),
                It.IsAny<FindOptions<TestEntity, ProjectedName>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FakeAsyncCursor<ProjectedName>(projected));

        ProjectedName result = await collectionMock.Object.FindOneProjectedAsync(
            e => e.Name != null,
            e => new ProjectedName { Name = e.Name });

        result.Name.Should().Be("First");
    }

    [Fact]
    public void CreateProjectionOptions_ShouldReturnNewOptionsInstance()
    {
        var collectionMock = new Mock<IMongoCollection<TestEntity>>();

        MongoDbProjectionOptions<TestEntity, ProjectedName> options = collectionMock.Object.CreateProjectionOptions<TestEntity, ProjectedName>();

        options.Should().NotBeNull();
    }

    [Fact]
    public async Task FindAutoMappedAsync_ShouldReturnAutoMappedResults()
    {
        var collectionMock = new Mock<IMongoCollection<TestEntity>>();
        var projected = new List<ProjectedName> { new() { Name = "Mapped" } };
        collectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<TestEntity>>(),
                It.IsAny<FindOptions<TestEntity, ProjectedName>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FakeAsyncCursor<ProjectedName>(projected));

        List<ProjectedName> results = await collectionMock.Object.FindAutoMappedAsync<TestEntity, ProjectedName>(e => e.Name != null);

        results.Should().ContainSingle().Which.Name.Should().Be("Mapped");
    }
}

public class ProjectedName
{
    public string Name { get; set; } = string.Empty;
}
