namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

[CollectionDefinition(MongoDbIntegrationCollection.Name)]
public sealed class MongoDbIntegrationCollection : ICollectionFixture<MongoDbIntegrationFixture>
{
    public const string Name = "MongoDbIntegration";
}
