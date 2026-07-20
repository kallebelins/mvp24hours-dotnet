//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Xunit;

namespace Mvp24Hours.Application.MongoDb.Test.Support;

[CollectionDefinition(Name)]
public sealed class MongoDbIntegrationCollection : ICollectionFixture<MongoDbIntegrationFixture>
{
    public const string Name = "MongoDbIntegration";
}
