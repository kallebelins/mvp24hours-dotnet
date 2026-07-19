//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Email.Templates;

namespace Mvp24Hours.Infrastructure.Test.Email.Templates;

[Trait("Category", "Unit")]
public class ScribanEmailTemplateRendererTest
{
    private readonly ScribanEmailTemplateRenderer _sut = new();

    [Fact]
    public async Task RenderAsync_WithObjectModel_ShouldReplaceVariables()
    {
        const string template = "Hello {{ Name }}, welcome to {{ Company }}!";
        var model = new TemplateModel { Name = "John", Company = "Acme" };

        string result = await _sut.RenderAsync(template, model);

        result.Should().Be("Hello John, welcome to Acme!");
    }

    [Fact]
    public async Task RenderAsync_WithAnonymousLowercaseProperties_ShouldReplaceVariables()
    {
        const string template = "Hello {{ name }}, order #{{ orderId }}";
        var model = new { name = "Jane", orderId = 99 };

        string result = await _sut.RenderAsync(template, model);

        result.Should().Be("Hello Jane, order #99");
    }

    [Fact]
    public async Task RenderAsync_WithDictionary_ShouldReplaceVariables()
    {
        const string template = "Hi {{ Customer }} — status {{ Status }}";
        var variables = new Dictionary<string, object?>
        {
            ["Customer"] = "Alice",
            ["Status"] = "Shipped"
        };

        string result = await _sut.RenderAsync(template, variables);

        result.Should().Be("Hi Alice — status Shipped");
    }

    [Fact]
    public async Task RenderAsync_WithNullModel_ShouldRenderWithoutVariables()
    {
        const string template = "Static greeting";

        string result = await _sut.RenderAsync(template, model: null);

        result.Should().Be("Static greeting");
    }

    [Fact]
    public async Task RenderAsync_WithConditional_ShouldEvaluateBranch()
    {
        const string template = "{{ if IsPremium }}Premium{{ else }}Standard{{ end }}";
        var model = new TemplateModel { Name = "X", Company = "Y", IsPremium = true };

        string result = await _sut.RenderAsync(template, model);

        result.Trim().Should().Be("Premium");
    }

    [Fact]
    public async Task RenderAsync_WithPublicFields_ShouldExposeFields()
    {
        const string template = "Code={{ Code }}";
        var model = new TemplateModelWithFields { Code = "XYZ" };

        string result = await _sut.RenderAsync(template, model);

        result.Should().Be("Code=XYZ");
    }

    [Fact]
    public async Task RenderAsync_WithInvalidSyntax_ShouldThrowTemplateRenderException()
    {
        // Unclosed statement — Scriban Parse reports HasErrors
        const string template = "{{ if IsPremium }}broken";

        Func<Task> act = () => _sut.RenderAsync(template, new TemplateModel { IsPremium = true });

        TemplateRenderException ex = (await act.Should().ThrowAsync<TemplateRenderException>()).Which;
        ex.Errors.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RenderAsync_WithEmptyTemplate_ShouldThrowArgumentException(string? template)
    {
        Func<Task> act = () => _sut.RenderAsync(template!, new TemplateModel { Name = "X" });

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("template");
    }

    [Fact]
    public async Task RenderAsync_WithNullVariablesDictionary_ShouldThrowArgumentNullException()
    {
        Func<Task> act = () => _sut.RenderAsync("Hello {{ Name }}", (IDictionary<string, object?>)null!);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("variables");
    }

    [Fact]
    public async Task RenderAsync_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        Func<Task> act = () => _sut.RenderAsync("Hello {{ Name }}", new { Name = "X" }, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task RenderAsync_WithCustomOptions_ShouldStillRender()
    {
        var renderer = new ScribanEmailTemplateRenderer(new TemplateOptions
        {
            StrictMode = true,
            DefaultValueForMissingVariables = "N/A"
        });

        string result = await renderer.RenderAsync("Hello {{ Name }}", new TemplateModel { Name = "Bob" });

        result.Should().Be("Hello Bob");
    }

    [Fact]
    public async Task RenderFromFileAsync_WithValidFile_ShouldRenderTemplate()
    {
        string path = Path.Combine(Path.GetTempPath(), $"scriban-template-{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(path, "Hello {{ Name }}");

        try
        {
            string result = await _sut.RenderFromFileAsync(path, new TemplateModel { Name = "FileUser" });
            result.Should().Be("Hello FileUser");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RenderFromFileAsync_WithEmptyPath_ShouldThrowArgumentException(string? path)
    {
        Func<Task> act = () => _sut.RenderFromFileAsync(path!);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("templatePath");
    }

    [Fact]
    public async Task RenderFromFileAsync_WhenFileMissing_ShouldThrowFileNotFoundException()
    {
        string missing = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.scriban");

        Func<Task> act = () => _sut.RenderFromFileAsync(missing, new { Name = "X" });

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ValidateAsync_WithValidTemplate_ShouldReturnValid()
    {
        TemplateValidationResult result = await _sut.ValidateAsync("Hello {{ Name }}");

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidSyntax_ShouldReturnInvalid()
    {
        TemplateValidationResult result = await _sut.ValidateAsync("{{ if IsPremium }}broken");

        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_WithEmptyTemplate_ShouldThrowArgumentException(string? template)
    {
        Func<Task> act = () => _sut.ValidateAsync(template!);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("template");
    }

    [Fact]
    public async Task ValidateAsync_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        Func<Task> act = () => _sut.ValidateAsync("Hello {{ Name }}", cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private sealed class TemplateModel
    {
        public string? Name { get; set; }
        public string? Company { get; set; }
        public bool IsPremium { get; set; }
    }

    private sealed class TemplateModelWithFields
    {
        public string Code = string.Empty;
    }
}
