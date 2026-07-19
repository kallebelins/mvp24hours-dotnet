//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Email.Templates;

namespace Mvp24Hours.Infrastructure.Test.Email.Templates;

[Trait("Category", "Unit")]
public class TemplateValidationAndExceptionTest
{
    [Fact]
    public void TemplateValidationResult_Valid_ShouldHaveNoErrors()
    {
        var result = TemplateValidationResult.Valid();

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void TemplateValidationResult_Invalid_ShouldExposeErrors()
    {
        var result = TemplateValidationResult.Invalid("err1", "err2");

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Equal("err1", "err2");
    }

    [Fact]
    public void TemplateValidationResult_ConstructorWithNullErrors_ShouldUseEmptyList()
    {
        var result = new TemplateValidationResult(isValid: false, errors: null);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void TemplateRenderException_WithMessage_ShouldPopulateErrors()
    {
        var ex = new TemplateRenderException("boom");

        ex.Message.Should().Be("boom");
        ex.Errors.Should().Equal("boom");
    }

    [Fact]
    public void TemplateRenderException_WithErrorList_ShouldExposeErrors()
    {
        var ex = new TemplateRenderException("failed", ["a", "b"]);

        ex.Message.Should().Be("failed");
        ex.Errors.Should().Equal("a", "b");
    }

    [Fact]
    public void TemplateRenderException_WithNullErrorList_ShouldFallbackToMessage()
    {
        var ex = new TemplateRenderException("failed", (IList<string>)null!);

        ex.Errors.Should().Equal("failed");
    }

    [Fact]
    public void TemplateRenderException_WithInnerException_ShouldPreserveInner()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new TemplateRenderException("outer", inner);

        ex.Message.Should().Be("outer");
        ex.InnerException.Should().BeSameAs(inner);
        ex.Errors.Should().Equal("outer");
    }

    [Fact]
    public void TemplateOptions_Defaults_ShouldBeExpected()
    {
        var options = new TemplateOptions();

        options.StrictMode.Should().BeFalse();
        options.DefaultValueForMissingVariables.Should().BeNull();
    }
}
