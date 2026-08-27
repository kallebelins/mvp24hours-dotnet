//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
using Mvp24Hours.Application.Test.Support;

namespace Mvp24Hours.Application.Test.Logic;

/// <summary>
/// Covers the cached reflection-based PATCH map shared by the sync and async
/// <c>ApplicationServiceBaseWithSeparateDtos</c> bases (task 8.5).
/// </summary>
[Trait("Category", "Unit")]
public class ApplyPatchToEntityTest
{
    [Fact]
    public void ApplyPatch_NullProperty_DoesNotOverwriteEntityValue()
    {
        PatchProbeService service = PatchTestHelpers.CreateSyncProbe();
        PatchTestEntity entity = PatchTestHelpers.CreateEntity();

        service.ApplyPatch(new PatchTestUpdateDto { Name = null }, entity);

        entity.Name.Should().Be("Original");
        entity.ModifiedAt.Should().BeNull();
    }

    [Fact]
    public void ApplyPatch_NonNullProperty_OverwritesEntityValue()
    {
        PatchProbeService service = PatchTestHelpers.CreateSyncProbe();
        PatchTestEntity entity = PatchTestHelpers.CreateEntity();
        var modifiedAt = new DateTime(2026, 8, 27, 10, 0, 0, DateTimeKind.Utc);

        service.ApplyPatch(new PatchTestUpdateDto { Name = "Patched", ModifiedAt = modifiedAt }, entity);

        entity.Name.Should().Be("Patched");
        entity.ModifiedAt.Should().Be(modifiedAt);
    }

    [Fact]
    public void ApplyPatch_IncompatibleType_IsIgnored()
    {
        PatchProbeService service = PatchTestHelpers.CreateSyncProbe();
        PatchTestEntity entity = PatchTestHelpers.CreateEntity();

        // DTO exposes Code as string? while the entity exposes int: not assignable.
        service.ApplyPatch(new PatchTestUpdateDto { Code = "999" }, entity);

        entity.Code.Should().Be(55);
    }

    [Fact]
    public void ApplyPatch_NullableDtoPropertyOnNonNullableEntityProperty_IsIgnored()
    {
        PatchProbeService service = PatchTestHelpers.CreateSyncProbe();
        PatchTestEntity entity = PatchTestHelpers.CreateEntity();

        // Documented limitation: bool is not assignable from bool?, so the pair is skipped.
        service.ApplyPatch(new PatchTestUpdateDto { Active = false }, entity);

        entity.Active.Should().BeTrue();
    }

    [Fact]
    public void ApplyPatch_PropertyWithoutSetterOnEntity_IsIgnored()
    {
        PatchProbeService service = PatchTestHelpers.CreateSyncProbe();
        PatchTestEntity entity = PatchTestHelpers.CreateEntity();

        service.ApplyPatch(new PatchTestUpdateDto { Tag = "changed" }, entity);

        entity.Tag.Should().Be("original");
    }

    [Fact]
    public void ApplyPatch_ValueTypeWithDefaultValue_IsApplied()
    {
        PatchProbeService service = PatchTestHelpers.CreateSyncProbe();
        PatchTestEntity entity = PatchTestHelpers.CreateEntity();

        // Documented limitation: a non-nullable value type left at its default is
        // indistinguishable from "informed as default" and is always applied.
        service.ApplyPatch(new PatchTestUpdateDto { Name = "Patched" }, entity);

        entity.Quantity.Should().Be(0);
    }

    [Fact]
    public void ApplyPatch_CalledTwice_ProducesSameResult()
    {
        PatchProbeService service = PatchTestHelpers.CreateSyncProbe();
        var dto = new PatchTestUpdateDto { Name = "Patched", Quantity = 7, Code = "999", Tag = "changed", Active = false };

        PatchTestEntity first = PatchTestHelpers.CreateEntity();
        service.ApplyPatch(dto, first);

        PatchTestEntity second = PatchTestHelpers.CreateEntity();
        service.ApplyPatch(dto, second);

        second.Name.Should().Be(first.Name);
        second.Active.Should().Be(first.Active);
        second.Quantity.Should().Be(first.Quantity);
        second.Code.Should().Be(first.Code);
        second.Tag.Should().Be(first.Tag);
        second.ModifiedAt.Should().Be(first.ModifiedAt);
    }

    [Fact]
    public void ApplyPatch_AsyncBase_BehavesLikeSyncBase()
    {
        PatchProbeService syncService = PatchTestHelpers.CreateSyncProbe();
        PatchProbeServiceAsync asyncService = PatchTestHelpers.CreateAsyncProbe();
        var dto = new PatchTestUpdateDto { Name = "Patched", Quantity = 7, Code = "999", Tag = "changed", Active = false };

        PatchTestEntity syncEntity = PatchTestHelpers.CreateEntity();
        PatchTestEntity asyncEntity = PatchTestHelpers.CreateEntity();

        syncService.ApplyPatch(dto, syncEntity);
        asyncService.ApplyPatch(dto, asyncEntity);

        asyncEntity.Name.Should().Be(syncEntity.Name);
        asyncEntity.Active.Should().Be(syncEntity.Active);
        asyncEntity.Quantity.Should().Be(syncEntity.Quantity);
        asyncEntity.Code.Should().Be(syncEntity.Code);
        asyncEntity.Tag.Should().Be(syncEntity.Tag);
        asyncEntity.ModifiedAt.Should().Be(syncEntity.ModifiedAt);
    }
}
