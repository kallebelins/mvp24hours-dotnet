using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Application.Test.Support;
using Mvp24Hours.Core.Contract.Logic;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Application.Test.Logic.Async;

[Trait("Category", "Unit")]
public class BulkCommandServiceWithSeparateDtosBaseAsyncTest : IDisposable
{
    private readonly BulkTestDbContext _context;
    private readonly TestBulkSeparateDtosService _service;

    public BulkCommandServiceWithSeparateDtosBaseAsyncTest()
    {
        DbContextOptions<BulkTestDbContext> options = new DbContextOptionsBuilder<BulkTestDbContext>()
            .UseInMemoryDatabase($"BulkSep_{Guid.NewGuid():N}")
            .Options;
        _context = new BulkTestDbContext(options);
        _context.Database.EnsureCreated();
        _service = new TestBulkSeparateDtosService(
            _context,
            ApplicationTestHelpers.CreateTestMapper(),
            new AppTestCreateDtoValidator(),
            new AppTestUpdateDtoValidator());
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task BulkAddAsync_CreateDtos_ShouldInsertEntities()
    {
        IList<AppTestCreateDto> dtos =
        [
            new() { Name = "Create-1" },
            new() { Name = "Create-2" }
        ];

        IBusinessResult<BulkOperationResult> result = await _service.BulkAddAsync(dtos);

        result.Data!.IsSuccess.Should().BeTrue();
        _context.Entities.Should().HaveCount(2);
    }

    [Fact]
    public async Task BulkModifyAsync_UpdateDtos_ShouldUpdateEntities()
    {
        var entity = new TestEntity { Name = "Original", Active = true, Score = 1 };
        _context.Entities.Add(entity);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        IList<AppTestUpdateDto> dtos = [new() { Id = entity.Id, Name = "Updated" }];

        IBusinessResult<BulkOperationResult> result = await _service.BulkModifyAsync(dtos);

        result.Data!.IsSuccess.Should().BeTrue();
        _context.Entities.Single().Name.Should().Be("Updated");
    }

    [Fact]
    public async Task BulkAddAsync_InvalidCreateDto_ShouldFailValidation()
    {
        IList<AppTestCreateDto> dtos = [new() { Name = "" }];

        IBusinessResult<BulkOperationResult> result = await _service.BulkAddAsync(dtos);

        result.Data!.IsSuccess.Should().BeFalse();
    }
}
