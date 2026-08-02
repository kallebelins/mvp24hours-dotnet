using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Infrastructure.Data.EFCore.Cqrs;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Cqrs;

[Trait("Category", "Unit")]
public class ReadWriteDbContextTest
{
    [Fact]
    public void ReadDbContextBase_SaveChanges_ShouldThrowInvalidOperationException()
    {
        using TestReadDbContext context = CreateReadContext();

        Action save = () => context.SaveChanges();
        Func<Task> saveAsync = () => context.SaveChangesAsync();

        save.Should().Throw<InvalidOperationException>()
            .WithMessage("*read-only*");
        saveAsync.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*read-only*");
    }

    [Fact]
    public void ReadDbContextBase_ShouldUseNoTracking()
    {
        using TestReadDbContext context = CreateReadContext();

        context.ChangeTracker.QueryTrackingBehavior.Should().Be(QueryTrackingBehavior.NoTracking);
        context.ChangeTracker.AutoDetectChangesEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task WriteDbContextBase_SaveChanges_ShouldPersistEntity()
    {
        using TestWriteDbContext context = CreateWriteContext();
        context.Entities.Add(new TestEntity { Name = "Writable", Active = true, Score = 1 });

        int changes = await context.SaveChangesAsync();

        changes.Should().Be(1);
        context.Entities.Count().Should().Be(1);
    }

    [Fact]
    public void WriteDbContextBase_ShouldUseFullTracking()
    {
        using TestWriteDbContext context = CreateWriteContext();

        context.ChangeTracker.QueryTrackingBehavior.Should().Be(QueryTrackingBehavior.TrackAll);
        context.ChangeTracker.AutoDetectChangesEnabled.Should().BeTrue();
    }

    private static TestReadDbContext CreateReadContext()
    {
        DbContextOptions<TestReadDbContext> options = new DbContextOptionsBuilder<TestReadDbContext>()
            .UseInMemoryDatabase($"ReadCtx_{Guid.NewGuid():N}")
            .Options;
        var context = new TestReadDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static TestWriteDbContext CreateWriteContext()
    {
        DbContextOptions<TestWriteDbContext> options = new DbContextOptionsBuilder<TestWriteDbContext>()
            .UseInMemoryDatabase($"WriteCtx_{Guid.NewGuid():N}")
            .Options;
        var context = new TestWriteDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private sealed class TestReadDbContext(DbContextOptions options) : ReadDbContextBase(options)
    {
        public DbSet<TestEntity> Entities => Set<TestEntity>();
    }

    private sealed class TestWriteDbContext(DbContextOptions options) : WriteDbContextBase(options)
    {
        public DbSet<TestEntity> Entities => Set<TestEntity>();
    }
}
