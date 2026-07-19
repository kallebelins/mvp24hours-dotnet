using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;
using Mvp24Hours.Infrastructure.Data.EFCore.Testing;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Testing;

[Trait("Category", "Unit")]
public class TestDbContextFactoryTest
{
    [Fact]
    public void CreateContext_ShouldReturnConfiguredInMemoryContext()
    {
        using var factory = new InMemoryTestDbContextFactory<TestDbContext>(
            new TestDbContextFactoryOptions { CreateNewDatabasePerTest = false });

        using TestDbContext context = factory.CreateContext();

        context.Database.ProviderName.Should().Contain("InMemory");
        context.Model.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateContextAsync_ShouldReturnContext()
    {
        using var factory = new InMemoryTestDbContextFactory<TestDbContext>();

        TestDbContext context = await factory.CreateContextAsync();

        context.Should().NotBeNull();
        context.Dispose();
    }

    [Fact]
    public async Task InitializeDatabaseAsync_ShouldEnsureCreated()
    {
        using var factory = new InMemoryTestDbContextFactory<TestDbContext>(
            new TestDbContextFactoryOptions { CreateNewDatabasePerTest = true });

        await factory.InitializeDatabaseAsync();

        using TestDbContext context = factory.CreateContext();
        (await context.Database.CanConnectAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task CleanupDatabaseAsync_ShouldDeleteDatabase()
    {
        using var factory = new InMemoryTestDbContextFactory<TestDbContext>(
            new TestDbContextFactoryOptions { CreateNewDatabasePerTest = true });

        await factory.InitializeDatabaseAsync();
        await factory.CleanupDatabaseAsync();

        using TestDbContext context = factory.CreateContext();
        (await context.Database.EnsureCreatedAsync()).Should().BeTrue();
    }

    [Fact]
    public void TestDbContextFactoryOptions_ShouldHaveExpectedDefaults()
    {
        var options = new TestDbContextFactoryOptions();

        options.UseMigrations.Should().BeFalse();
        options.CreateNewDatabasePerTest.Should().BeTrue();
        options.DatabaseNamePrefix.Should().Be("TestDb_");
        options.EnableSensitiveDataLogging.Should().BeTrue();
        options.EnableDetailedErrors.Should().BeTrue();
    }
}
