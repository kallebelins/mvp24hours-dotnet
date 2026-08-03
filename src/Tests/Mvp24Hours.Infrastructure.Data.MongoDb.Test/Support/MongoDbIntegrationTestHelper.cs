//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Mvp24Hours.Infrastructure.Data.MongoDb;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

internal static class MongoDbIntegrationTestHelper
{
    public static string GetConnectionString(MongoDbIntegrationFixture fixture)
    {
        return fixture.ConnectionString;
    }

    public static Mvp24HoursContext CreateContext(MongoDbIntegrationFixture fixture, string? databaseName = null)
    {
        return new Mvp24HoursContext(
            databaseName ?? fixture.DatabaseName,
            GetConnectionString(fixture));
    }

    public static IOptions<MongoDbRepositoryOptions> CreateRepositoryOptions()
    {
        return Options.Create(new MongoDbRepositoryOptions());
    }

    public static IOptions<MongoDbOptions> CreateMongoDbOptions(MongoDbIntegrationFixture fixture, string? databaseName = null)
    {
        return Options.Create(new MongoDbOptions
        {
            DatabaseName = databaseName ?? fixture.DatabaseName,
            ConnectionString = GetConnectionString(fixture)
        });
    }
}
