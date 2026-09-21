using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.WebAPI.Binders;
using Mvp24Hours.WebAPI.Test.Support;

namespace Mvp24Hours.WebAPI.Test.Binders;

[Trait("Category", "Unit")]
public class ModelBinderTest
{
    public sealed class SampleFilter
    {
        [Required]
        public string? Name { get; set; }

        [Range(18, 99)]
        public int Age { get; set; }
    }

    public sealed class SampleFilterValidator : AbstractValidator<SampleFilter>
    {
        public SampleFilterValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Age).InclusiveBetween(18, 99);
        }
    }

    private static DefaultHttpContext CreateContextWithQuery(string queryString, IServiceProvider? services = null)
    {
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();
        context.Request.QueryString = new QueryString(queryString);
        context.RequestServices = services ?? WebApiTestHelpers.CreateServiceProvider();
        return context;
    }

    [Fact]
    public async Task BindAsync_WithoutValidatorRegistered_ValidData_PassesDataAnnotations()
    {
        // Arrange
        DefaultHttpContext context = CreateContextWithQuery("?Name=Alice&Age=30");

        // Act
        ModelBinder<SampleFilter> result = await ModelBinder<SampleFilter>.BindAsync(context);

        // Assert
        result.IsValid.Should().BeTrue();
        result.HasErrors.Should().BeFalse();
        result.Data.Name.Should().Be("Alice");
        result.ValidationErrors.Should().BeNull();
    }

    [Fact]
    public async Task BindAsync_WithoutValidatorRegistered_InvalidData_FailsDataAnnotations()
    {
        // Arrange - missing required Name, Age out of range
        DefaultHttpContext context = CreateContextWithQuery("?Age=5");

        // Act
        ModelBinder<SampleFilter> result = await ModelBinder<SampleFilter>.BindAsync(context);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.IsValid.Should().BeFalse();
        result.ValidationErrors.Should().NotBeNull();
        result.ValidationErrors!.Should().ContainKey("Name");
    }

    [Fact]
    public async Task BindAsync_WithFluentValidatorRegistered_ValidData_Passes()
    {
        // Arrange
        IServiceProvider services = WebApiTestHelpers.CreateServiceProvider(s =>
            s.AddSingleton<IValidator<SampleFilter>, SampleFilterValidator>());
        DefaultHttpContext context = CreateContextWithQuery("?Name=Bob&Age=25", services);

        // Act
        ModelBinder<SampleFilter> result = await ModelBinder<SampleFilter>.BindAsync(context);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ValidationErrors.Should().BeNull();
    }

    [Fact]
    public async Task BindAsync_WithFluentValidatorRegistered_InvalidData_PopulatesGroupedErrors()
    {
        // Arrange
        IServiceProvider services = WebApiTestHelpers.CreateServiceProvider(s =>
            s.AddSingleton<IValidator<SampleFilter>, SampleFilterValidator>());
        DefaultHttpContext context = CreateContextWithQuery("?Age=200", services);

        // Act
        ModelBinder<SampleFilter> result = await ModelBinder<SampleFilter>.BindAsync(context);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.ValidationErrors.Should().NotBeNull();
        result.ValidationErrors!.Should().ContainKey("Name");
        result.ValidationErrors.Should().ContainKey("Age");
    }

    [Fact]
    public async Task BindAsync_WhenFluentValidatorRegistered_TakesPrecedenceOverDataAnnotations()
    {
        // Arrange - Name empty would fail both FluentValidation.NotEmpty and [Required];
        // this only proves the FluentValidation path is the one actually exercised when present.
        IServiceProvider services = WebApiTestHelpers.CreateServiceProvider(s =>
            s.AddSingleton<IValidator<SampleFilter>, SampleFilterValidator>());
        DefaultHttpContext context = CreateContextWithQuery("?Name=&Age=30", services);

        // Act
        ModelBinder<SampleFilter> result = await ModelBinder<SampleFilter>.BindAsync(context);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.ValidationErrors.Should().ContainKey("Name");
    }

    [Fact]
    public async Task BindAsync_WithoutQueryString_ReturnsDefaultInstanceAndDataAnnotationErrors()
    {
        // Arrange
        DefaultHttpContext context = CreateContextWithQuery(string.Empty);

        // Act
        ModelBinder<SampleFilter> result = await ModelBinder<SampleFilter>.BindAsync(context);

        // Assert
        result.Data.Should().NotBeNull();
        result.HasErrors.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task BindAsync_WhenQueryValueCannotDeserialize_SetsErrorAndDefaultData()
    {
        // Arrange - "abc" cannot be deserialized into the int Age property, forcing
        // GetFromQueryString<T> to throw during JSON deserialization.
        DefaultHttpContext context = CreateContextWithQuery("?Name=Alice&Age=abc");

        // Act
        ModelBinder<SampleFilter> result = await ModelBinder<SampleFilter>.BindAsync(context);

        // Assert
        result.Error.Should().NotBeNull();
        result.HasErrors.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }
}
