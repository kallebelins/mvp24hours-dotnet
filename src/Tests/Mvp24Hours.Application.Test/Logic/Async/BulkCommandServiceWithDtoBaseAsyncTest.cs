using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Application.Test.Support;
using Mvp24Hours.Core.Contract.Logic;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Application.Test.Logic.Async;

[Trait("Category", "Unit")]
public class BulkCommandServiceWithDtoBaseAsyncTest : IDisposable
{
    private readonly BulkTestDbContext _context;
    private readonly TestBulkDtoService _service;

    public BulkCommandServiceWithDtoBaseAsyncTest()
    {
        _context = CreateBulkContext();
        _service = new TestBulkDtoService(
            _context,
            ApplicationTestHelpers.CreateTestMapper(),
            new AppTestEntityDtoValidator());
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private static BulkTestDbContext CreateBulkContext()
    {
        DbContextOptions<BulkTestDbContext> options = new DbContextOptionsBuilder<BulkTestDbContext>()
            .UseInMemoryDatabase($"BulkDto_{Guid.NewGuid():N}")
            .Options;
        var context = new BulkTestDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task BulkAddAsync_ValidDtos_ShouldInsertEntities()
    {
        IList<AppTestEntityDto> dtos =
        [
            new() { Name = "Bulk-1", Active = true },
            new() { Name = "Bulk-2", Active = false }
        ];

        IBusinessResult<BulkOperationResult> result = await _service.BulkAddAsync(dtos);

        result.Data!.IsSuccess.Should().BeTrue();
        result.Data.RowsAffected.Should().Be(2);
        _context.Entities.Count().Should().Be(2);
    }

    [Fact]
    public async Task BulkAddAsync_InvalidDto_ShouldFailValidation()
    {
        IList<AppTestEntityDto> dtos = [new() { Name = "" }];

        IBusinessResult<BulkOperationResult> result = await _service.BulkAddAsync(dtos);

        result.Data!.IsSuccess.Should().BeFalse();
        _context.Entities.Count().Should().Be(0);
    }

    [Fact]
    public async Task BulkAddAsync_EmptyList_ShouldReturnValidationFailure()
    {
        IBusinessResult<BulkOperationResult> result = await _service.BulkAddAsync([]);

        result.Data!.IsSuccess.Should().BeFalse();
    }
}
