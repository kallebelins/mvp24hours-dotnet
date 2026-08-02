using Mvp24Hours.Core.Domain.Entities;

namespace Mvp24Hours.Core.Test.Domain;

[Trait("Category", "Unit")]
public class EntityBaseTest
{
    private sealed class TestGuidEntity : GuidEntityBase
    {
        public TestGuidEntity() { }

        public TestGuidEntity(Guid id) : base(id) { }
    }

    private sealed class TestIntEntity : IntEntityBase;

    private sealed class TestAuditableEntity(Guid id) : AuditableGuidEntity(id)
    {
    }

    private sealed class TestSoftDeletableEntity(Guid id) : SoftDeletableGuidEntity(id)
    {
    }

    [Fact]
    public void GuidEntityBase_AssignsNewId()
    {
        var entity = new TestGuidEntity();

        entity.Id.Should().NotBe(Guid.Empty);
        entity.IsTransient().Should().BeFalse();
    }

    [Fact]
    public void EntityBase_Equality_BasedOnIdAndType()
    {
        var id = Guid.NewGuid();
        var entity1 = new TestGuidEntity(id);
        var entity2 = new TestGuidEntity(id);
        var entity3 = new TestGuidEntity(Guid.NewGuid());

        entity1.Should().Be(entity2);
        entity1.Should().NotBe(entity3);
        (entity1 == entity2).Should().BeTrue();
        (entity1 != entity3).Should().BeTrue();
    }

    [Fact]
    public void EntityBase_TransientEntities_AreNeverEqual()
    {
        var transient1 = new TestIntEntity();
        var transient2 = new TestIntEntity();

        transient1.IsTransient().Should().BeTrue();
        transient1.Equals(transient2).Should().BeFalse();
    }

    [Fact]
    public void EntityBase_EntityKey_ReturnsId()
    {
        var id = Guid.NewGuid();
        var entity = new TestGuidEntity(id);

        ((Mvp24Hours.Core.Contract.Domain.Entity.IEntityBase)entity).EntityKey.Should().Be(id);
    }

    [Fact]
    public void AuditableEntity_StoresAuditFields()
    {
        DateTime createdAt = DateTime.UtcNow;
        var entity = new TestAuditableEntity(Guid.NewGuid())
        {
            CreatedAt = createdAt,
            CreatedBy = "creator",
            ModifiedAt = createdAt.AddMinutes(1),
            ModifiedBy = "editor"
        };

        entity.CreatedBy.Should().Be("creator");
        entity.ModifiedBy.Should().Be("editor");
    }

    [Fact]
    public void SoftDeletableEntity_SoftDeleteAndRestore()
    {
        var entity = new TestSoftDeletableEntity(Guid.NewGuid());

        entity.SoftDelete("admin");

        entity.IsDeleted.Should().BeTrue();
        entity.DeletedBy.Should().Be("admin");
        entity.DeletedAt.Should().NotBeNull();

        entity.Restore();

        entity.IsDeleted.Should().BeFalse();
        entity.DeletedBy.Should().BeEmpty();
        entity.DeletedAt.Should().BeNull();
    }
}
