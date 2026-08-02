using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;
using Mvp24Hours.Infrastructure.Data.EFCore.Testing;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Testing;

[Trait("Category", "Unit")]
public class InMemoryDbContextFactoryTest
{
    [Fact]
    public void CreateContext_ShouldReturnInMemoryContext()
    {
        using var factory = new InMemoryDbContextFactory<TestDbContext>(
            new InMemoryDbContextOptions { UseUniqueDatabaseName = true });

        using TestDbContext context = factory.CreateContext();

        context.Database.IsInMemory().Should().BeTrue();
    }

    [Fact]
    public void CreateContextWithDatabase_ShouldEnsureCreated()
    {
        using var factory = new InMemoryDbContextFactory<TestDbContext>();

        using TestDbContext context = factory.CreateContextWithDatabase();

        context.Database.CanConnect().Should().BeTrue();
    }

    [Fact]
    public void CreateContextWithData_UsingSeederType_ShouldSeedEntities()
    {
        using var factory = new InMemoryDbContextFactory<TestDbContext>(
            new InMemoryDbContextOptions { UseUniqueDatabaseName = true });

        using TestDbContext context = factory.CreateContextWithData<TestEntitySeeder>();

        context.Entities.Should().HaveCount(TestEntitySeeder.DefaultSeedCount);
        context.Entities.Select(e => e.Name).Should().Contain("Seeded-1");
    }

    [Fact]
    public void CreateContextWithData_UsingSeederInstance_ShouldSeedEntities()
    {
        using var factory = new InMemoryDbContextFactory<TestDbContext>();

        using TestDbContext context = factory.CreateContextWithData(new TestEntitySeeder());

        context.Entities.Should().HaveCount(TestEntitySeeder.DefaultSeedCount);
    }

    [Fact]
    public void CreateContextWithData_UsingAction_ShouldSeedEntities()
    {
        using var factory = new InMemoryDbContextFactory<TestDbContext>();

        using TestDbContext context = factory.CreateContextWithData(ctx => ctx.Entities.Add(new TestEntity { Id = 99, Name = "Inline" }));

        context.Entities.Should().ContainSingle(e => e.Name == "Inline");
    }

    [Fact]
    public void InMemoryDbContextHelper_CreateOptions_ShouldConfigureInMemory()
    {
        DbContextOptions<TestDbContext> options = InMemoryDbContextHelper.CreateOptions<TestDbContext>("HelperDb");

        using var context = new TestDbContext(options);
        context.Database.IsInMemory().Should().BeTrue();
    }
}
