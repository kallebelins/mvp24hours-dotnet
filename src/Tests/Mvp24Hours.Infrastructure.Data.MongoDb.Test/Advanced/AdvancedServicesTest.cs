using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.CappedCollections;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.ChangeStreams;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.Geospatial;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.GridFS;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.SchemaValidation;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.TextSearch;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Advanced;

[Trait("Category", "Integration")]
[Collection(MongoDbIntegrationCollection.Name)]
public class AdvancedServicesIntegrationTest(MongoDbIntegrationFixture fixture)
{
    [DockerFact]
    public async Task TextSearchService_ShouldCreateIndexAndSearch()
    {
        IMongoCollection<TestArticle> collection = fixture.GetCollection<TestArticle>("articles_search");
        await collection.DeleteManyAsync(FilterDefinition<TestArticle>.Empty);

        var service = new MongoDbTextSearchService<TestArticle>(collection);
        await service.CreateTextIndexAsync(["Title", "Description"]);

        await collection.InsertManyAsync(
        [
            new TestArticle { Title = "MongoDB tutorial", Description = "Learn MongoDB basics" },
            new TestArticle { Title = "Redis caching", Description = "Cache patterns" },
            new TestArticle { Title = "Advanced MongoDB", Description = "MongoDB tutorial advanced" }
        ]);

        IList<TextSearchResult<TestArticle>> results = await service.SearchAsync("mongodb tutorial");

        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.Document.Title.Contains("MongoDB", StringComparison.OrdinalIgnoreCase));
        long count = await service.CountAsync("mongodb");
        count.Should().BeGreaterThan(0);
    }

    [DockerFact]
    public async Task TextSearchService_ShouldSearchWithExclusionsAndPhrase()
    {
        IMongoCollection<TestArticle> collection = fixture.GetCollection<TestArticle>("articles_search_excl");
        await collection.DeleteManyAsync(FilterDefinition<TestArticle>.Empty);

        var service = new MongoDbTextSearchService<TestArticle>(collection);
        await service.CreateTextIndexAsync(["Title", "Description"]);
        await collection.InsertManyAsync(
        [
            new TestArticle { Title = "MongoDB tutorial", Description = "beginner guide" },
            new TestArticle { Title = "MongoDB expert", Description = "advanced patterns" }
        ]);

        IList<TextSearchResult<TestArticle>> excluded = await service.SearchWithExclusionsAsync(
            ["mongodb"],
            ["beginner"]);

        excluded.Should().NotBeEmpty();

        IList<TextSearchResult<TestArticle>> phrase = await service.SearchPhraseAsync("MongoDB tutorial");
        phrase.Should().NotBeEmpty();
    }

    [DockerFact]
    public async Task GeospatialService_ShouldFindNearAndWithin()
    {
        IMongoCollection<TestPlace> collection = fixture.GetCollection<TestPlace>("places_geo");
        await collection.DeleteManyAsync(FilterDefinition<TestPlace>.Empty);

        var service = new MongoDbGeospatialService<TestPlace>(collection);
        await service.Create2dSphereIndexAsync("Location");

        var saoPaulo = new GeoPoint(-46.6333, -23.5505);
        var nearby = new GeoPoint(-46.64, -23.55);
        var farAway = new GeoPoint(-43.2, -22.9);

        await collection.InsertManyAsync(
        [
            new TestPlace { Name = "Near SP", Location = nearby },
            new TestPlace { Name = "Far RJ", Location = farAway }
        ]);

        IList<TestPlace> nearResults = await service.FindNearAsync("Location", saoPaulo, 5000);
        nearResults.Should().Contain(p => p.Name == "Near SP");

        long withinCount = await service.CountWithinRadiusAsync("Location", saoPaulo, 5000);
        withinCount.Should().BeGreaterThan(0);

        var box = GeoPolygon.FromPoints(
            new GeoPoint(-46.65, -23.56),
            new GeoPoint(-46.63, -23.56),
            new GeoPoint(-46.63, -23.54),
            new GeoPoint(-46.65, -23.54),
            new GeoPoint(-46.65, -23.56));

        IList<TestPlace> inPolygon = await service.FindWithinPolygonAsync("Location", box);
        inPolygon.Should().Contain(p => p.Name == "Near SP");
        inPolygon.Should().NotContain(p => p.Name == "Far RJ");
    }

    [DockerFact]
    public async Task GridFsService_ShouldUploadDownloadRenameAndDelete()
    {
        var service = new MongoDbGridFsService(fixture.Database);
        byte[] content = "gridfs-test-content"u8.ToArray();

        ObjectId fileId = await service.UploadAsync("test.txt", content);
        fileId.Should().NotBe(ObjectId.Empty);

        (await service.ExistsAsync(fileId)).Should().BeTrue();
        (await service.ExistsByNameAsync("test.txt")).Should().BeTrue();

        byte[] downloaded = await service.DownloadAsBytesAsync(fileId);
        downloaded.Should().BeEquivalentTo(content);

        await service.RenameAsync(fileId, "renamed.txt");
        GridFSFileInfo info = await service.GetFileInfoByNameAsync("renamed.txt");
        info.Filename.Should().Be("renamed.txt");

        IList<GridFSFileInfo> files = await service.ListFilesAsync();
        files.Should().NotBeEmpty();
        (await service.GetTotalSizeAsync()).Should().BeGreaterThan(0);

        await service.DeleteAsync(fileId);
        (await service.ExistsAsync(fileId)).Should().BeFalse();
    }

    [DockerFact]
    public async Task CappedCollectionService_ShouldCreateInsertAndQuery()
    {
        string collectionName = $"logs_{Guid.NewGuid():N}";
        var service = new MongoDbCappedCollectionService<TestLogEntry>(
            fixture.Database,
            collectionName);

        await service.CreateCappedCollectionAsync(collectionName, new CappedCollectionOptions
        {
            MaxSizeBytes = 1024 * 1024,
            MaxDocuments = 1000
        });

        await service.InsertManyAsync(
        [
            new TestLogEntry { Message = "first" },
            new TestLogEntry { Message = "second" },
            new TestLogEntry { Message = "third" }
        ]);

        (await service.IsCappedAsync()).Should().BeTrue();
        CappedCollectionStats stats = await service.GetStatsAsync();
        stats.IsCapped.Should().BeTrue();
        stats.DocumentCount.Should().BeGreaterThanOrEqualTo(3);

        IList<TestLogEntry> latest = await service.GetLatestAsync(2);
        latest.Should().HaveCount(2);

        IList<TestLogEntry> oldest = await service.GetOldestAsync(2);
        oldest.Should().HaveCount(2);
        oldest[0].Message.Should().Be("first");
    }

    [DockerFact]
    public async Task SchemaValidationService_ShouldCreateValidateAndRemove()
    {
        string collectionName = $"users_{Guid.NewGuid():N}";
        var service = new MongoDbSchemaValidationService(fixture.Database);

        BsonDocument schema = new JsonSchemaBuilder()
            .WithBsonType("object")
            .WithRequired("Name", "Email")
            .WithProperty("Name", p => p.WithBsonType("string").WithMinLength(1))
            .WithProperty("Email", p => p.WithBsonType("string"))
            .Build();

        await service.CreateCollectionWithValidationAsync(collectionName, schema);

        BsonDocument? validator = await service.GetValidationAsync(collectionName);
        validator.Should().NotBeNull();

        SchemaValidationResult valid = await service.ValidateDocumentAsync(collectionName, new ValidatedUser
        {
            Name = "Alice",
            Email = "alice@example.com",
            Age = 30
        });
        valid.IsValid.Should().BeTrue();

        SchemaValidationResult invalid = await service.ValidateDocumentAsync(collectionName, new ValidatedUser
        {
            Name = "",
            Email = "invalid",
            Age = 30
        });
        invalid.IsValid.Should().BeFalse();

        BsonDocument generated = service.GenerateSchemaFromType<ValidatedUser>();
        generated.Contains("properties").Should().BeTrue();

        await service.RemoveValidationAsync(collectionName);
    }

    [DockerFact]
    public async Task TextSearchService_GetTextIndexesAsync_ShouldIdentifyTextIndexes()
    {
        IMongoCollection<TestArticle> collection = fixture.GetCollection<TestArticle>("articles_text_indexes");
        await collection.DeleteManyAsync(FilterDefinition<TestArticle>.Empty);

        var service = new MongoDbTextSearchService<TestArticle>(collection);
        await service.CreateTextIndexAsync(["Title"]);

        IList<BsonDocument> indexes = await service.GetTextIndexesAsync();
        indexes.Should().NotBeEmpty();
        indexes.Should().Contain(i => i["key"].AsBsonDocument.Elements.Any(e =>
            e.Value.IsString && e.Value.AsString == "text"));
    }
}

[Trait("Category", "Unit")]
public class AdvancedServicesUnitTest
{
    [Fact]
    public void GeoPoint_ShouldValidateCoordinatesAndCalculateDistance()
    {
        var point = new GeoPoint(-46.6333, -23.5505);
        point.Longitude.Should().Be(-46.6333);
        point.Latitude.Should().Be(-23.5505);

        var other = GeoPoint.FromLatLng(-23.5506, -46.6334);
        point.DistanceTo(other).Should().BeGreaterThan(0).And.BeLessThan(200);

        Action invalidLng = () => _ = new GeoPoint(200, 0);
        invalidLng.Should().Throw<ArgumentOutOfRangeException>();

        Action invalidLat = () => _ = new GeoPoint(0, 100);
        invalidLat.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GeoPolygon_ShouldRequireMinimumPoints()
    {
        Action tooFew = () => GeoPolygon.FromPoints(
            new GeoPoint(0, 0),
            new GeoPoint(1, 0),
            new GeoPoint(1, 1));

        tooFew.Should().Throw<ArgumentException>();

        var circle = GeoPolygon.CreateCircle(new GeoPoint(0, 0), 1000, 8);
        circle.Coordinates.Should().NotBeEmpty();
        circle.ToBsonDocument()["type"].AsString.Should().Be("Polygon");
    }

    [Fact]
    public void JsonSchemaBuilder_ShouldBuildSchemaWithProperties()
    {
        BsonDocument schema = new JsonSchemaBuilder()
            .WithBsonType("object")
            .WithRequired("name")
            .WithProperty("name", p => p.WithBsonType("string").WithMinLength(1).WithMaxLength(50))
            .WithProperty("status", p => p.WithEnum("active", "inactive"))
            .Build();

        schema["bsonType"].AsString.Should().Be("object");
        schema["required"].AsBsonArray.Should().Contain("name");
        schema["properties"].AsBsonDocument.Contains("name").Should().BeTrue();
    }

    [Fact]
    public void TextSearchService_ShouldThrowOnEmptySearch()
    {
        var collection = new Moq.Mock<IMongoCollection<TestArticle>>();
        var service = new MongoDbTextSearchService<TestArticle>(collection.Object);

        Func<Task> act = () => service.SearchAsync("  ");
        act.Should().ThrowAsync<ArgumentException>();

        Func<Task> createIndex = () => service.CreateTextIndexAsync([]);
        createIndex.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void GeospatialService_ShouldThrowOnInvalidPoint()
    {
        var collection = new Moq.Mock<IMongoCollection<TestPlace>>();
        var service = new MongoDbGeospatialService<TestPlace>(collection.Object);

        Func<Task> act = () => service.FindWithinPolygonAsync("Location", null!);
        act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void GridFsService_ShouldValidateConstructorAndUploadArgs()
    {
        Action nullDb = () => _ = new MongoDbGridFsService((IMongoDatabase)null!);
        nullDb.Should().Throw<ArgumentNullException>();

        var bucket = new Moq.Mock<IGridFSBucket>();
        var service = new MongoDbGridFsService(bucket.Object);

        Func<Task> emptyName = () => service.UploadAsync(" ", new MemoryStream([1]));
        emptyName.Should().ThrowAsync<ArgumentException>();

        Func<Task> emptyBytes = () => service.UploadAsync("file.bin", []);
        emptyBytes.Should().ThrowAsync<ArgumentException>();

        Func<Task> missingFile = () => service.UploadFromFileAsync("x.txt", "missing-file-path.bin");
        missingFile.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public void CappedCollectionService_ShouldValidateOptions()
    {
        var database = new Moq.Mock<IMongoDatabase>();
        var service = new MongoDbCappedCollectionService<TestLogEntry>(database.Object, "logs");

        Func<Task> nullOptions = () => service.CreateCappedCollectionAsync("logs", null!);
        nullOptions.Should().ThrowAsync<ArgumentNullException>();

        Func<Task> invalidSize = () => service.CreateCappedCollectionAsync("logs", new CappedCollectionOptions { MaxSizeBytes = 0 });
        invalidSize.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void ChangeStreamService_ShouldValidateHandlers()
    {
        var collection = new Moq.Mock<IMongoCollection<TestArticle>>();
        var service = new MongoDbChangeStreamService<TestArticle>(collection.Object);

        Func<Task> nullHandler = () => service.WatchCollectionAsync(null!);
        nullHandler.Should().ThrowAsync<ArgumentNullException>();

        Func<Task> emptyOps = () => service.WatchCollectionAsync(_ => Task.CompletedTask, []);
        emptyOps.Should().ThrowAsync<ArgumentException>();
    }
}
