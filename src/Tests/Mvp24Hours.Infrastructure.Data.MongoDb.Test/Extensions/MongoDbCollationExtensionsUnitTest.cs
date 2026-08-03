using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.Collation;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Extensions;

[Trait("Category", "Unit")]
public class MongoDbCollationExtensionsUnitTest
{
    [Fact]
    public void EnglishCaseInsensitivePreset_ShouldUseSecondaryStrength()
    {
        MongoDbCollationOptions options = CollationPresets.EnglishCaseInsensitive;

        options.Locale.Should().Be("en");
        options.Strength.Should().Be(CollationStrength.Secondary);
    }

    [Fact]
    public void NumericOrderedPreset_ShouldEnableNumericOrdering()
    {
        MongoDbCollationOptions options = CollationPresets.NumericOrdered;

        options.NumericOrdering.Should().BeTrue();
    }

    [Fact]
    public void PortugueseCaseInsensitivePreset_ShouldUsePortugueseLocale()
    {
        MongoDbCollationOptions options = CollationPresets.PortugueseCaseInsensitive;

        options.Locale.Should().Be("pt");
        options.Strength.Should().Be(CollationStrength.Secondary);
    }

    [Fact]
    public void ToCollation_ShouldMapAllConfiguredProperties()
    {
        var options = new MongoDbCollationOptions
        {
            Locale = "fr",
            CaseLevel = true,
            CaseFirst = CollationCaseFirst.Lower,
            Strength = CollationStrength.Primary,
            NumericOrdering = true,
            Alternate = CollationAlternate.Shifted,
            MaxVariable = CollationMaxVariable.Space,
            Normalization = true,
            Backwards = false
        };

        var collation = options.ToCollation();

        collation.Locale.Should().Be("fr");
        collation.CaseLevel.Should().BeTrue();
        collation.CaseFirst.Should().Be(CollationCaseFirst.Lower);
        collation.Strength.Should().Be(CollationStrength.Primary);
        collation.NumericOrdering.Should().BeTrue();
        collation.Alternate.Should().Be(CollationAlternate.Shifted);
        collation.MaxVariable.Should().Be(CollationMaxVariable.Space);
        collation.Normalization.Should().BeTrue();
        collation.Backwards.Should().BeFalse();
    }

    [Fact]
    public void SimpleBinaryPreset_ShouldUseSimpleLocale()
    {
        MongoDbCollationOptions options = CollationPresets.SimpleBinary;

        options.Locale.Should().Be("simple");
        options.ToCollation().Locale.Should().Be("simple");
    }

    [Fact]
    public async Task CountWithCollationAsync_ShouldReturnDocumentCount()
    {
        Mock<IMongoCollection<CollationDoc>> collectionMock = CreateCollectionMock();
        collectionMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<CollationDoc>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        long count = await collectionMock.Object.CountWithCollationAsync(
            Builders<CollationDoc>.Filter.Empty,
            CollationPresets.EnglishCaseInsensitive);

        count.Should().Be(42);
    }

    [Fact]
    public async Task UpdateWithCollationAsync_ShouldReturnUpdateResult()
    {
        Mock<IMongoCollection<CollationDoc>> collectionMock = CreateCollectionMock();
        var updateResultMock = new Mock<UpdateResult>();
        updateResultMock.SetupGet(r => r.ModifiedCount).Returns(3);
        updateResultMock.SetupGet(r => r.IsAcknowledged).Returns(true);
        collectionMock
            .Setup(c => c.UpdateManyAsync(
                It.IsAny<FilterDefinition<CollationDoc>>(),
                It.IsAny<UpdateDefinition<CollationDoc>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updateResultMock.Object);

        UpdateResult result = await collectionMock.Object.UpdateWithCollationAsync(
            Builders<CollationDoc>.Filter.Empty,
            Builders<CollationDoc>.Update.Set(d => d.Name, "updated"),
            CollationPresets.EnglishCaseInsensitive);

        result.ModifiedCount.Should().Be(3);
    }

    [Fact]
    public async Task DeleteWithCollationAsync_ShouldReturnDeleteResult()
    {
        Mock<IMongoCollection<CollationDoc>> collectionMock = CreateCollectionMock();
        var deleteResultMock = new Mock<DeleteResult>();
        deleteResultMock.SetupGet(r => r.DeletedCount).Returns(2);
        deleteResultMock.SetupGet(r => r.IsAcknowledged).Returns(true);
        collectionMock
            .Setup(c => c.DeleteManyAsync(
                It.IsAny<FilterDefinition<CollationDoc>>(),
                It.IsAny<DeleteOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleteResultMock.Object);

        DeleteResult result = await collectionMock.Object.DeleteWithCollationAsync(
            Builders<CollationDoc>.Filter.Empty,
            CollationPresets.EnglishCaseInsensitive);

        result.DeletedCount.Should().Be(2);
    }

    [Fact]
    public async Task CreateIndexWithCollationAsync_ShouldReturnIndexName()
    {
        Mock<IMongoCollection<CollationDoc>> collectionMock = CreateCollectionMock();
        var indexesMock = new Mock<IMongoIndexManager<CollationDoc>>();
        indexesMock
            .Setup(i => i.CreateOneAsync(
                It.IsAny<CreateIndexModel<CollationDoc>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("name_1");
        collectionMock.SetupGet(c => c.Indexes).Returns(indexesMock.Object);

        string indexName = await collectionMock.Object.CreateIndexWithCollationAsync(
            "Name",
            CollationPresets.EnglishCaseInsensitive);

        indexName.Should().Be("name_1");
    }

    [Fact]
    public async Task DistinctWithCollationAsync_ShouldReturnDistinctValues()
    {
        Mock<IMongoCollection<CollationDoc>> collectionMock = CreateCollectionMock();
        Mock<IAsyncCursor<string>> cursorMock = new();
        cursorMock.Setup(c => c.Current).Returns(["a", "b"]);
        cursorMock.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        collectionMock
            .Setup(c => c.Distinct(
                It.IsAny<FieldDefinition<CollationDoc, string>>(),
                It.IsAny<FilterDefinition<CollationDoc>>(),
                It.IsAny<DistinctOptions>()))
            .Returns(cursorMock.Object);

        IList<string> values = await collectionMock.Object.DistinctWithCollationAsync<CollationDoc, string>(
            "Name",
            CollationPresets.EnglishCaseInsensitive);

        values.Should().BeEquivalentTo("a", "b");
    }

    private static Mock<IMongoCollection<CollationDoc>> CreateCollectionMock()
    {
        var collectionMock = new Mock<IMongoCollection<CollationDoc>>();
        collectionMock.SetupGet(c => c.DocumentSerializer)
            .Returns(MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry.GetSerializer<CollationDoc>());
        collectionMock.SetupGet(c => c.Settings).Returns(new MongoCollectionSettings());
        return collectionMock;
    }
}

public class CollationDoc
{
    public string Name { get; set; } = string.Empty;
}
