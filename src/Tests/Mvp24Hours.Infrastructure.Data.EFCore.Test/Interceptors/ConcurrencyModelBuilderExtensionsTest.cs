using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Mvp24Hours.Infrastructure.Data.EFCore.Interceptors;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Interceptors;

[Trait("Category", "Unit")]
public class ConcurrencyModelBuilderExtensionsTest
{
    [Fact]
    public void ApplyConcurrencyTokens_ForVersionedEntityWithCounter_ShouldConfigureConcurrencyToken()
    {
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<TestVersionedEntity>();

        modelBuilder.ApplyConcurrencyTokens();

        IMutableEntityType entityType = modelBuilder.Model.GetEntityTypes().Single(e => e.ClrType == typeof(TestVersionedEntity));
        IMutableProperty? versionProperty = entityType.FindProperty(nameof(TestVersionedEntity.Version));

        versionProperty.Should().NotBeNull();
        versionProperty!.IsConcurrencyToken.Should().BeTrue();
    }

    [Fact]
    public void ApplyConcurrencyTokens_ForEntityWithoutVersionInterfaces_ShouldNotThrowOrConfigureToken()
    {
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<TestEntity>().Property(e => e.Score);

        Action act = () => modelBuilder.ApplyConcurrencyTokens();

        act.Should().NotThrow();
        IMutableEntityType entityType = modelBuilder.Model.GetEntityTypes().Single(e => e.ClrType == typeof(TestEntity));
        IMutableProperty? scoreProperty = entityType.FindProperty(nameof(TestEntity.Score));
        scoreProperty.Should().NotBeNull();
        scoreProperty!.IsConcurrencyToken.Should().BeFalse();
    }

    [Fact]
    public void HasVersionCounter_ShouldConfigureVersionPropertyAsConcurrencyToken()
    {
        var modelBuilder = new ModelBuilder();

        modelBuilder.Entity<TestVersionedEntity>().HasVersionCounter();

        IMutableEntityType entityType = modelBuilder.Model.GetEntityTypes().Single(e => e.ClrType == typeof(TestVersionedEntity));
        IMutableProperty? versionProperty = entityType.FindProperty(nameof(TestVersionedEntity.Version));

        versionProperty!.IsConcurrencyToken.Should().BeTrue();
    }

    [Fact]
    public void HasRowVersion_ShouldConfigureRowVersionPropertyAsRowVersion()
    {
        var modelBuilder = new ModelBuilder();

        modelBuilder.Entity<TestRowVersionEntity>().HasRowVersion();

        IMutableEntityType entityType = modelBuilder.Model.GetEntityTypes().Single(e => e.ClrType == typeof(TestRowVersionEntity));
        IMutableProperty? rowVersionProperty = entityType.FindProperty(nameof(TestRowVersionEntity.RowVersion));

        rowVersionProperty.Should().NotBeNull();
        rowVersionProperty!.ValueGenerated.Should().Be(ValueGenerated.OnAddOrUpdate);
        rowVersionProperty.IsConcurrencyToken.Should().BeTrue();
    }

    [Fact]
    public void ApplyConcurrencyTokens_ForRowVersionEntity_ShouldConfigureRowVersion()
    {
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<TestRowVersionEntity>();

        modelBuilder.ApplyConcurrencyTokens();

        IMutableEntityType entityType = modelBuilder.Model.GetEntityTypes().Single(e => e.ClrType == typeof(TestRowVersionEntity));
        IMutableProperty? rowVersionProperty = entityType.FindProperty(nameof(TestRowVersionEntity.RowVersion));

        rowVersionProperty!.IsConcurrencyToken.Should().BeTrue();
    }
}
