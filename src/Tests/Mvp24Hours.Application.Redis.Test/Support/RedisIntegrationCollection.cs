//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Xunit;

namespace Mvp24Hours.Application.Redis.Test.Support;

[CollectionDefinition(Name)]
public sealed class RedisIntegrationCollection : ICollectionFixture<RedisIntegrationFixture>
{
    public const string Name = "RedisIntegration";
}
