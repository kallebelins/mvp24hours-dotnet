//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Sms.Models;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.Sms.Models;

[Trait("Category", "Unit")]
public class SmsTemplateTest
{
    [Fact]
    public void DefaultConstructor_ShouldUseSimpleTemplateEngine()
    {
        var template = new SmsTemplate();

        template.TemplateEngine.Should().Be("Simple");
    }

    [Fact]
    public void Validate_WithValidTemplate_ShouldReturnEmptyList()
    {
        SmsTemplate template = SmsTestHelpers.CreateSmsTemplate();

        IList<string> errors = template.Validate();

        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithMissingId_ShouldReturnError()
    {
        SmsTemplate template = SmsTestHelpers.CreateSmsTemplate(id: "  ", body: "Hello");

        IList<string> errors = template.Validate();

        errors.Should().ContainSingle()
            .Which.Should().Be("Template ID is required.");
    }

    [Fact]
    public void Validate_WithMissingBody_ShouldReturnError()
    {
        SmsTemplate template = SmsTestHelpers.CreateSmsTemplate(body: "  ");

        IList<string> errors = template.Validate();

        errors.Should().ContainSingle()
            .Which.Should().Be("Template body is required.");
    }

    [Fact]
    public void Validate_WithMissingIdAndBody_ShouldReturnBothErrors()
    {
        var template = new SmsTemplate();

        IList<string> errors = template.Validate();

        errors.Should().HaveCount(2);
        errors.Should().Contain("Template ID is required.");
        errors.Should().Contain("Template body is required.");
    }

    [Fact]
    public void Properties_ShouldStoreMetadataAndDefaults()
    {
        var metadata = new Dictionary<string, string> { ["category"] = "auth" };
        SmsTemplate template = SmsTestHelpers.CreateSmsTemplate(
            defaultFrom: "+5511888888888",
            metadata: metadata);

        template.DefaultFrom.Should().Be("+5511888888888");
        template.Metadata.Should().BeSameAs(metadata);
        template.Name.Should().Be("Welcome Message");
    }
}
