//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Sms.Models;
using Mvp24Hours.Infrastructure.Sms.Services;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.Sms.Services;

[Trait("Category", "Unit")]
public class InMemorySmsTemplateServiceTest
{
    private readonly InMemorySmsTemplateService _service = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetTemplateAsync_WithInvalidId_ShouldThrowArgumentException(string? templateId)
    {
        Func<Task> act = async () => await _service.GetTemplateAsync(templateId!);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("templateId");
    }

    [Fact]
    public async Task GetTemplateAsync_WhenNotFound_ShouldReturnNull()
    {
        SmsTemplate? result = await _service.GetTemplateAsync("missing-template");

        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveTemplateAsync_WithNullTemplate_ShouldThrowArgumentNullException()
    {
        Func<Task> act = async () => await _service.SaveTemplateAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("template");
    }

    [Fact]
    public async Task SaveTemplateAsync_WithInvalidTemplate_ShouldThrowArgumentException()
    {
        SmsTemplate template = SmsTestHelpers.CreateSmsTemplate(id: "", body: "");

        Func<Task> act = async () => await _service.SaveTemplateAsync(template);

        (await act.Should().ThrowAsync<ArgumentException>())
            .Which.Message.Should().Contain("Template validation failed");
    }

    [Fact]
    public async Task SaveTemplateAsync_WithNewTemplate_ShouldSetCreatedAndUpdatedTimestamps()
    {
        SmsTemplate template = SmsTestHelpers.CreateSmsTemplate();

        await _service.SaveTemplateAsync(template);

        template.CreatedAt.Should().NotBeNull();
        template.UpdatedAt.Should().NotBeNull();
        template.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        template.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SaveTemplateAsync_WithExistingTemplate_ShouldPreserveCreatedAtAndUpdateTimestamp()
    {
        DateTimeOffset createdAt = DateTimeOffset.UtcNow.AddDays(-1);
        SmsTemplate template = SmsTestHelpers.CreateSmsTemplate();
        template.CreatedAt = createdAt;

        await _service.SaveTemplateAsync(template);
        DateTimeOffset firstUpdatedAt = template.UpdatedAt!.Value;

        template.Body = "Updated body {Name}";
        await _service.SaveTemplateAsync(template);

        template.CreatedAt.Should().Be(createdAt);
        template.UpdatedAt.Should().BeOnOrAfter(firstUpdatedAt);
    }

    [Fact]
    public async Task GetTemplateAsync_AfterSave_ShouldReturnTemplate()
    {
        SmsTemplate template = SmsTestHelpers.CreateSmsTemplate(id: "otp-code");

        await _service.SaveTemplateAsync(template);
        SmsTemplate? result = await _service.GetTemplateAsync("otp-code");

        result.Should().NotBeNull();
        result!.Id.Should().Be("otp-code");
        result.Body.Should().Be(template.Body);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteTemplateAsync_WithInvalidId_ShouldThrowArgumentException(string? templateId)
    {
        Func<Task> act = async () => await _service.DeleteTemplateAsync(templateId!);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("templateId");
    }

    [Fact]
    public async Task DeleteTemplateAsync_ShouldRemoveTemplate()
    {
        SmsTemplate template = SmsTestHelpers.CreateSmsTemplate(id: "to-delete");
        await _service.SaveTemplateAsync(template);

        await _service.DeleteTemplateAsync("to-delete");

        SmsTemplate? result = await _service.GetTemplateAsync("to-delete");
        result.Should().BeNull();
    }

    [Fact]
    public async Task ListTemplatesAsync_ShouldReturnAllSavedTemplates()
    {
        await _service.SaveTemplateAsync(SmsTestHelpers.CreateSmsTemplate(id: "template-a"));
        await _service.SaveTemplateAsync(SmsTestHelpers.CreateSmsTemplate(id: "template-b", name: "Second"));

        IList<SmsTemplate> templates = await _service.ListTemplatesAsync();

        templates.Should().HaveCount(2);
        templates.Select(t => t.Id).Should().BeEquivalentTo(["template-a", "template-b"]);
    }

    [Fact]
    public async Task RenderAsync_WithNullTemplate_ShouldThrowArgumentNullException()
    {
        Func<Task> act = async () => await _service.RenderAsync(null!, new Dictionary<string, object>());

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("template");
    }

    [Fact]
    public async Task RenderAsync_WithEmptyBody_ShouldThrowArgumentException()
    {
        SmsTemplate template = SmsTestHelpers.CreateSmsTemplate(body: "   ");

        Func<Task> act = async () => await _service.RenderAsync(template, new Dictionary<string, object>());

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("template");
    }

    [Fact]
    public async Task RenderAsync_WithPlaceholders_ShouldReplaceValues()
    {
        SmsTemplate template = SmsTestHelpers.CreateSmsTemplate(
            body: "Hello {Name}, your code is {Code}.");

        string rendered = await _service.RenderAsync(template, new Dictionary<string, object>
        {
            ["Name"] = "Alice",
            ["Code"] = 123456
        });

        rendered.Should().Be("Hello Alice, your code is 123456.");
    }

    [Fact]
    public async Task RenderAsync_WithNullValues_ShouldUseEmptyDictionary()
    {
        SmsTemplate template = SmsTestHelpers.CreateSmsTemplate(body: "Static message");

        string rendered = await _service.RenderAsync(template, null!);

        rendered.Should().Be("Static message");
    }

    [Fact]
    public async Task RenderAsync_WithNullPlaceholderValue_ShouldReplaceWithEmptyString()
    {
        SmsTemplate template = SmsTestHelpers.CreateSmsTemplate(body: "Hello {Name}!");

        string rendered = await _service.RenderAsync(template, new Dictionary<string, object>
        {
            ["Name"] = null!
        });

        rendered.Should().Be("Hello !");
    }

    [Fact]
    public async Task RenderAsync_WithUnsupportedEngine_ShouldThrowNotSupportedException()
    {
        SmsTemplate template = SmsTestHelpers.CreateSmsTemplate(
            body: "Hello {Name}",
            templateEngine: "Razor");

        Func<Task> act = async () => await _service.RenderAsync(template, new Dictionary<string, object>
        {
            ["Name"] = "Alice"
        });

        (await act.Should().ThrowAsync<NotSupportedException>())
            .Which.Message.Should().Contain("Razor");
    }

    [Fact]
    public async Task RenderByIdAsync_WhenTemplateMissing_ShouldThrowKeyNotFoundException()
    {
        Func<Task> act = async () => await _service.RenderByIdAsync(
            "missing",
            new Dictionary<string, object> { ["Name"] = "Alice" });

        (await act.Should().ThrowAsync<KeyNotFoundException>())
            .Which.Message.Should().Contain("missing");
    }

    [Fact]
    public async Task RenderByIdAsync_WithExistingTemplate_ShouldRenderBody()
    {
        await _service.SaveTemplateAsync(SmsTestHelpers.CreateSmsTemplate(
            id: "greeting",
            body: "Hi {Name}!"));

        string rendered = await _service.RenderByIdAsync("greeting", new Dictionary<string, object>
        {
            ["Name"] = "Bob"
        });

        rendered.Should().Be("Hi Bob!");
    }
}
