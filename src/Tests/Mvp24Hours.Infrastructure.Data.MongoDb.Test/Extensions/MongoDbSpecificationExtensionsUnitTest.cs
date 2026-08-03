using MongoDB.Driver;
using Moq;
using Mvp24Hours.Core.Contract.Domain.Specifications;
using Mvp24Hours.Core.Domain.Specifications;
using Mvp24Hours.Extensions;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Extensions;

[Trait("Category", "Unit")]
public class MongoDbSpecificationExtensionsUnitTest
{
    [Fact]
    public void WithSpecification_WithNullCollection_ShouldThrow()
    {
        Action act = () => ((IMongoCollection<ExtensionDoc>)null!).WithSpecification(new ActiveNameSpecification());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithSpecification_WithNullSpecification_ShouldThrow()
    {
        Mock<IMongoCollection<ExtensionDoc>> collectionMock = new();

        Action act = () => collectionMock.Object.WithSpecification((ISpecificationQuery<ExtensionDoc>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task AggregateCountBySpecificationAsync_ShouldReturnDocumentCount()
    {
        Mock<IMongoCollection<ExtensionDoc>> collectionMock = new();
        collectionMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<ExtensionDoc>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        long count = await collectionMock.Object.AggregateCountBySpecificationAsync(new ActiveNameSpecification());

        count.Should().Be(3);
    }

    [Fact]
    public void ToFilterDefinition_ShouldConvertSpecification()
    {
        var filter = new ActiveNameSpecification().ToFilterDefinition();

        filter.Should().NotBeNull();
    }

    [Fact]
    public void ToSortDefinition_ShouldConvertEnhancedSpecification()
    {
        var sort = new ActiveNameSpecification().ToSortDefinition();

        sort.Should().NotBeNull();
    }

    private sealed class ActiveNameSpecification : Specification<ExtensionDoc>
    {
        protected override System.Linq.Expressions.Expression<Func<ExtensionDoc, bool>> Criteria =>
            doc => doc.Active;

        public ActiveNameSpecification()
        {
            AddOrderByDescending(doc => doc.Name);
            ApplyPaging(0, 5);
        }
    }
}
