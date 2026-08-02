using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Mvp24Hours.WebAPI.Binders;
using Mvp24Hours.WebAPI.Test.Support;

namespace Mvp24Hours.WebAPI.Test.Binders;

[Trait("Category", "Unit")]
public class BindersTest
{
    [Fact]
    public async Task TimeOnlyModelBinder_Should_BindValidTime()
    {
        ModelBindingContext context = WebApiTestHelpers.CreateModelBindingContext("time", "13:45", typeof(TimeOnly));

        await new TimeOnlyModelBinder().BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
        context.Result.Model.Should().Be(TimeOnly.Parse("13:45"));
    }

    [Fact]
    public async Task TimeOnlyModelBinder_Should_AddError_OnInvalidTime()
    {
        ModelBindingContext context = WebApiTestHelpers.CreateModelBindingContext("time", "bad-time", typeof(TimeOnly));

        await new TimeOnlyModelBinder().BindModelAsync(context);

        context.ModelState.ErrorCount.Should().Be(1);
    }

    [Fact]
    public async Task DateOnlyModelBinder_Should_BindValidDate()
    {
        ModelBindingContext context = WebApiTestHelpers.CreateModelBindingContext("date", "2026-07-18", typeof(DateOnly));

        await new DateOnlyModelBinder().BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
        context.Result.Model.Should().Be(DateOnly.Parse("2026-07-18"));
    }

    [Fact]
    public async Task DateOnlyModelBinder_Should_AddError_OnInvalidDate()
    {
        ModelBindingContext context = WebApiTestHelpers.CreateModelBindingContext("date", "99-99-9999", typeof(DateOnly));

        await new DateOnlyModelBinder().BindModelAsync(context);

        context.ModelState.ErrorCount.Should().Be(1);
    }

    [Fact]
    public void Mvp24HoursModelBinderProvider_Should_ReturnDateOnlyBinder()
    {
        var provider = new Mvp24HoursModelBinderProvider();
        var context = new TestModelBinderProviderContext(typeof(DateOnly));

        IModelBinder? binder = provider.GetBinder(context);

        binder.Should().BeOfType<DateOnlyModelBinder>();
    }

    [Fact]
    public void Mvp24HoursModelBinderProvider_Should_ReturnTimeOnlyBinder()
    {
        var provider = new Mvp24HoursModelBinderProvider();
        var context = new TestModelBinderProviderContext(typeof(TimeOnly));

        IModelBinder? binder = provider.GetBinder(context);

        binder.Should().BeOfType<TimeOnlyModelBinder>();
    }

    [Fact]
    public void Mvp24HoursModelBinderProvider_Should_ReturnNull_ForUnsupportedType()
    {
        var provider = new Mvp24HoursModelBinderProvider();
        var context = new TestModelBinderProviderContext(typeof(Guid));

        IModelBinder? binder = provider.GetBinder(context);

        binder.Should().BeNull();
    }
}

internal sealed class TestModelBinderProviderContext(Type modelType) : ModelBinderProviderContext
{
    private readonly EmptyModelMetadataProvider _provider = new();
    public override BindingInfo BindingInfo => new();
    public override ModelMetadata Metadata { get; } = new EmptyModelMetadataProvider().GetMetadataForType(modelType);
    public override IModelMetadataProvider MetadataProvider => _provider;
    public override IModelBinder CreateBinder(ModelMetadata metadata)
    {
        throw new NotImplementedException();
    }
}
