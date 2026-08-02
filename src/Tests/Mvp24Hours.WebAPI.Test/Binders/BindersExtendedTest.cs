using Microsoft.AspNetCore.Mvc.ModelBinding;
using Mvp24Hours.Core.ValueObjects;
using Mvp24Hours.WebAPI.Binders;
using Mvp24Hours.WebAPI.Test.Support;

namespace Mvp24Hours.WebAPI.Test.Binders;

[Trait("Category", "Unit")]
public class BindersExtendedTest
{
    // -----------------------------------------------------------------------
    // DateTimeOffsetModelBinder
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DateTimeOffsetModelBinder_Should_BindIso8601WithMilliseconds()
    {
        ModelBindingContext context = WebApiTestHelpers.CreateModelBindingContext("ts", "2026-07-19T13:45:00.000Z", typeof(DateTimeOffset));

        await new DateTimeOffsetModelBinder().BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
        var result = (DateTimeOffset)context.Result.Model!;
        result.Year.Should().Be(2026);
        result.Month.Should().Be(7);
        result.Day.Should().Be(19);
    }

    [Fact]
    public async Task DateTimeOffsetModelBinder_Should_BindIso8601WithoutMilliseconds()
    {
        ModelBindingContext context = WebApiTestHelpers.CreateModelBindingContext("ts", "2026-01-15T14:30:00Z", typeof(DateTimeOffset));

        await new DateTimeOffsetModelBinder().BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
    }

    [Fact]
    public async Task DateTimeOffsetModelBinder_Should_AddError_OnInvalidValue()
    {
        ModelBindingContext context = WebApiTestHelpers.CreateModelBindingContext("ts", "not-a-date", typeof(DateTimeOffset));

        await new DateTimeOffsetModelBinder().BindModelAsync(context);

        context.ModelState.ErrorCount.Should().Be(1);
    }

    [Fact]
    public async Task DateTimeOffsetModelBinder_Should_ReturnWithoutBinding_WhenValueIsNone()
    {
        ModelBindingContext context = WebApiTestHelpers.CreateModelBindingContext("ts", null, typeof(DateTimeOffset));

        await new DateTimeOffsetModelBinder().BindModelAsync(context);

        context.Result.IsModelSet.Should().BeFalse();
    }

    [Fact]
    public async Task DateTimeOffsetModelBinder_Should_BindDateTimeWithSlash()
    {
        ModelBindingContext context = WebApiTestHelpers.CreateModelBindingContext("ts", "2026-07-19 10:30:00", typeof(DateTimeOffset));

        await new DateTimeOffsetModelBinder().BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
    }

    [Fact]
    public async Task DateTimeOffsetModelBinder_Should_Throw_WhenContextIsNull()
    {
        var binder = new DateTimeOffsetModelBinder();
        await Assert.ThrowsAsync<ArgumentNullException>(() => binder.BindModelAsync(null!));
    }

    // -----------------------------------------------------------------------
    // EntityIdModelBinder
    // -----------------------------------------------------------------------

    [Fact]
    public async Task EntityIdModelBinder_Should_BindGuidEntityId()
    {
        var guid = Guid.NewGuid();
        ModelBindingContext context = WebApiTestHelpers.CreateModelBindingContext("id", guid.ToString(), typeof(TestGuidId));

        await new EntityIdModelBinder().BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
        var id = (TestGuidId)context.Result.Model!;
        id.Value.Should().Be(guid);
    }

    [Fact]
    public async Task EntityIdModelBinder_Should_BindIntEntityId()
    {
        ModelBindingContext context = WebApiTestHelpers.CreateModelBindingContext("id", "42", typeof(TestIntId));

        await new EntityIdModelBinder().BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
        var id = (TestIntId)context.Result.Model!;
        id.Value.Should().Be(42);
    }

    [Fact]
    public async Task EntityIdModelBinder_Should_BindLongEntityId()
    {
        ModelBindingContext context = WebApiTestHelpers.CreateModelBindingContext("id", "123456789012345", typeof(TestLongId));

        await new EntityIdModelBinder().BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
        var id = (TestLongId)context.Result.Model!;
        id.Value.Should().Be(123456789012345L);
    }

    [Fact]
    public async Task EntityIdModelBinder_Should_BindStringEntityId()
    {
        ModelBindingContext context = WebApiTestHelpers.CreateModelBindingContext("id", "my-string-id", typeof(TestStringId));

        await new EntityIdModelBinder().BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
        var id = (TestStringId)context.Result.Model!;
        id.Value.Should().Be("my-string-id");
    }

    [Fact]
    public async Task EntityIdModelBinder_Should_AddError_OnInvalidGuid()
    {
        ModelBindingContext context = WebApiTestHelpers.CreateModelBindingContext("id", "not-a-guid", typeof(TestGuidId));

        await new EntityIdModelBinder().BindModelAsync(context);

        context.ModelState.ErrorCount.Should().Be(1);
    }

    [Fact]
    public async Task EntityIdModelBinder_Should_AddError_OnNonEntityIdType()
    {
        ModelBindingContext context = WebApiTestHelpers.CreateModelBindingContext("id", "123", typeof(int));

        await new EntityIdModelBinder().BindModelAsync(context);

        context.ModelState.ErrorCount.Should().Be(1);
    }

    [Fact]
    public async Task EntityIdModelBinder_Should_ReturnWithoutBinding_WhenValueIsNone()
    {
        ModelBindingContext context = WebApiTestHelpers.CreateModelBindingContext("id", null, typeof(TestGuidId));

        await new EntityIdModelBinder().BindModelAsync(context);

        context.Result.IsModelSet.Should().BeFalse();
    }

    [Fact]
    public async Task EntityIdModelBinder_Should_Throw_WhenContextIsNull()
    {
        var binder = new EntityIdModelBinder();
        await Assert.ThrowsAsync<ArgumentNullException>(() => binder.BindModelAsync(null!));
    }

    // -----------------------------------------------------------------------
    // PagingCriteriaModelBinder
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PagingCriteriaModelBinder_Should_BindDefaultValues()
    {
        ModelBindingContext context = WebApiTestHelpers.CreatePagingModelBindingContext();

        await new PagingCriteriaModelBinder().BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
        var result = (Mvp24Hours.Core.ValueObjects.Logic.PagingCriteria)context.Result.Model!;
        result.Limit.Should().Be(20);
        result.Offset.Should().Be(0);
    }

    [Fact]
    public async Task PagingCriteriaModelBinder_Should_BindLimitAndOffset()
    {
        ModelBindingContext context = WebApiTestHelpers.CreatePagingModelBindingContext(limit: "10", offset: "5");

        await new PagingCriteriaModelBinder().BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
        var result = (Mvp24Hours.Core.ValueObjects.Logic.PagingCriteria)context.Result.Model!;
        result.Limit.Should().Be(10);
        result.Offset.Should().Be(5);
    }

    [Fact]
    public async Task PagingCriteriaModelBinder_Should_BindOrderBy()
    {
        ModelBindingContext context = WebApiTestHelpers.CreatePagingModelBindingContext(orderBy: "Name,Email");

        await new PagingCriteriaModelBinder().BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
        var result = (Mvp24Hours.Core.ValueObjects.Logic.PagingCriteria)context.Result.Model!;
        result.OrderBy.Should().Contain("Name");
        result.OrderBy.Should().Contain("Email");
    }

    [Fact]
    public async Task PagingCriteriaModelBinder_Should_BindNavigation()
    {
        ModelBindingContext context = WebApiTestHelpers.CreatePagingModelBindingContext(navigation: "Orders,Products");

        await new PagingCriteriaModelBinder().BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
        var result = (Mvp24Hours.Core.ValueObjects.Logic.PagingCriteria)context.Result.Model!;
        result.Navigation.Should().Contain("Orders");
        result.Navigation.Should().Contain("Products");
    }

    [Fact]
    public async Task PagingCriteriaModelBinder_Should_UseDefaultLimit_WhenInvalidLimitProvided()
    {
        ModelBindingContext context = WebApiTestHelpers.CreatePagingModelBindingContext(limit: "invalid");

        await new PagingCriteriaModelBinder().BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
        var result = (Mvp24Hours.Core.ValueObjects.Logic.PagingCriteria)context.Result.Model!;
        result.Limit.Should().Be(20);
        context.ModelState.Should().ContainKey("paging.limit");
    }

    [Fact]
    public async Task PagingCriteriaModelBinder_Should_BindPageSizeAlias()
    {
        ModelBindingContext context = WebApiTestHelpers.CreatePagingModelBindingContext(pageSize: "15");

        await new PagingCriteriaModelBinder().BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
        var result = (Mvp24Hours.Core.ValueObjects.Logic.PagingCriteria)context.Result.Model!;
        result.Limit.Should().Be(15);
    }

    [Fact]
    public async Task PagingCriteriaModelBinder_Should_AddError_ForUnsupportedType()
    {
        ModelBindingContext context2 = WebApiTestHelpers.CreateModelBindingContext("paging", "irrelevant", typeof(string));
        await new PagingCriteriaModelBinder().BindModelAsync(context2);

        context2.ModelState.ErrorCount.Should().Be(1);
    }
}

// -------------------------------------------------------------------
// Test EntityId implementations
// -------------------------------------------------------------------

internal sealed class TestGuidId(Guid value) : GuidEntityId<TestGuidId>(value);
internal sealed class TestIntId(int value) : IntEntityId<TestIntId>(value);
internal sealed class TestLongId(long value) : LongEntityId<TestLongId>(value);
internal sealed class TestStringId(string value) : StringEntityId<TestStringId>(value);
