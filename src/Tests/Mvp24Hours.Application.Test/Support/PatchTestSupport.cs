//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
using AutoMapper;
using Mvp24Hours.Application.Logic;
using Mvp24Hours.Core.Contract.Domain.Entity;

namespace Mvp24Hours.Application.Test.Support;

/// <summary>
/// Entity used to exercise every branch of the reflection-based PATCH map.
/// </summary>
public class PatchTestEntity : IEntityBase
{
    public int Id { get; set; }

    /// <summary>Nullable reference type on both sides: mapped.</summary>
    public string? Name { get; set; }

    /// <summary>Non-nullable value type while the DTO exposes <c>bool?</c>: not assignable, skipped.</summary>
    public bool Active { get; set; } = true;

    /// <summary>Nullable value type on both sides: mapped.</summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>Non-nullable value type on both sides: always applied, even with the default value.</summary>
    public int Quantity { get; set; } = 10;

    /// <summary>Entity is <c>int</c> while the DTO is <c>string?</c>: not assignable, skipped.</summary>
    public int Code { get; set; } = 55;

    /// <summary>No setter on the entity: skipped.</summary>
    public string Tag => "original";

    public object? EntityKey => Id;
}

public class PatchTestDto
{
    public string? Name { get; set; }
}

public class PatchTestCreateDto
{
    public string? Name { get; set; }
}

public class PatchTestUpdateDto
{
    public string? Name { get; set; }
    public bool? Active { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public int Quantity { get; set; }
    public string? Code { get; set; }
    public string? Tag { get; set; }
}

/// <summary>
/// Sync service that exposes the protected PATCH extension point for direct assertions.
/// </summary>
public sealed class PatchProbeService(IUnitOfWork unitOfWork, IMapper mapper)
    : ApplicationServiceBaseWithSeparateDtos<PatchTestEntity, PatchTestDto, PatchTestCreateDto, PatchTestUpdateDto, IUnitOfWork>(
        unitOfWork, mapper, null, null, null)
{
    public void ApplyPatch(PatchTestUpdateDto dto, PatchTestEntity entity)
    {
        ApplyPatchToEntity(dto, entity);
    }
}

/// <summary>
/// Async service that exposes the protected PATCH extension point for direct assertions.
/// </summary>
public sealed class PatchProbeServiceAsync(IUnitOfWorkAsync unitOfWork, IMapper mapper)
    : ApplicationServiceBaseWithSeparateDtosAsync<PatchTestEntity, PatchTestDto, PatchTestCreateDto, PatchTestUpdateDto, IUnitOfWorkAsync>(
        unitOfWork, mapper, null, null, null)
{
    public void ApplyPatch(PatchTestUpdateDto dto, PatchTestEntity entity)
    {
        ApplyPatchToEntity(dto, entity);
    }
}

public static class PatchTestHelpers
{
    public static PatchProbeService CreateSyncProbe()
    {
        (Mock<IUnitOfWork> uow, _) = ApplicationTestHelpers.CreateSyncRepositoryMocks<PatchTestEntity>();
        return new PatchProbeService(uow.Object, CreateMapper());
    }

    public static PatchProbeServiceAsync CreateAsyncProbe()
    {
        (Mock<IUnitOfWorkAsync> uow, _) = ApplicationTestHelpers.CreateRepositoryMocks<PatchTestEntity>();
        return new PatchProbeServiceAsync(uow.Object, CreateMapper());
    }

    public static PatchTestEntity CreateEntity()
    {
        return new PatchTestEntity
        {
            Id = 1,
            Name = "Original",
            Active = true,
            ModifiedAt = null,
            Quantity = 10,
            Code = 55
        };
    }

    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PatchTestEntity, PatchTestDto>().ReverseMap();
            cfg.CreateMap<PatchTestCreateDto, PatchTestEntity>();
            cfg.CreateMap<PatchTestUpdateDto, PatchTestEntity>()
                .ForMember(d => d.Active, o => o.Ignore())
                .ForMember(d => d.Code, o => o.Ignore());
        }, NullLoggerFactory.Instance);
        return config.CreateMapper();
    }
}
