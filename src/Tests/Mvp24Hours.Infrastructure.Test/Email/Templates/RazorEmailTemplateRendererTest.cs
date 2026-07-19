//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Email.Templates;

namespace Mvp24Hours.Infrastructure.Test.Email.Templates;

[Trait("Category", "Unit")]
public class RazorEmailTemplateRendererTest
{
    private readonly RazorEmailTemplateRenderer _sut = new();

    [Fact]
    public async Task RenderAsync_WithObjectModel_ShouldReplaceModelAndBarePlaceholders()
    {
        const string template = "Hello @Model.Name, welcome to @Company!";
        var model = new TemplateModel { Name = "John", Company = "Acme" };

        string result = await _sut.RenderAsync(template, model);

        result.Should().Be("Hello John, welcome to Acme!");
    }

    [Fact]
    public async Task RenderAsync_WithDictionary_ShouldReplacePlaceholders()
    {
        const string template = "Order #@Model.OrderId for @Customer";
        var variables = new Dictionary<string, object?>
        {
            ["OrderId"] = 42,
            ["Customer"] = "Jane"
        };

        string result = await _sut.RenderAsync(template, variables);

        result.Should().Be("Order #42 for Jane");
    }

    [Fact]
    public async Task RenderAsync_WithNullModel_ShouldReturnTemplateUnchanged()
    {
        const string template = "Hello @Model.Name";

        string result = await _sut.RenderAsync(template, model: null);

        result.Should().Be(template);
    }

    [Fact]
    public async Task RenderAsync_WithNullPropertyValue_ShouldReplaceWithEmptyString()
    {
        const string template = "Hi @Model.Name!";
        var model = new TemplateModel { Name = null, Company = "Acme" };

        string result = await _sut.RenderAsync(template, model);

        result.Should().Be("Hi !");
    }

    [Fact]
    public async Task RenderAsync_WithPublicFields_ShouldReplaceFieldPlaceholders()
    {
        const string template = "Code=@Model.Code / @Code";
        var model = new TemplateModelWithFields { Code = "ABC" };

        string result = await _sut.RenderAsync(template, model);

        result.Should().Be("Code=ABC / ABC");
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
        Func<Task> act = () => _sut.RenderAsync("Hello @Name", (IDictionary<string, object?>)null!);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("variables");
    }

    [Fact]
    public async Task RenderAsync_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        Func<Task> act = () => _sut.RenderAsync("Hello @Name", new { Name = "X" }, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task RenderFromFileAsync_WithValidFile_ShouldRenderTemplate()
    {
        string path = Path.Combine(Path.GetTempPath(), $"razor-template-{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(path, "Hello @Model.Name");

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
        string missing = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.razor");

        Func<Task> act = () => _sut.RenderFromFileAsync(missing, new { Name = "X" });

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ValidateAsync_WithValidTemplate_ShouldReturnValid()
    {
        TemplateValidationResult result = await _sut.ValidateAsync("Hello @Model.Name");

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithUnbalancedBraces_ShouldReturnInvalid()
    {
        TemplateValidationResult result = await _sut.ValidateAsync("@if (true) { Hello");

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("braces", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ValidateAsync_WithUnbalancedParentheses_ShouldReturnInvalid()
    {
        TemplateValidationResult result = await _sut.ValidateAsync("@if (true { }");

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("parentheses", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ValidateAsync_WithTrailingAtSymbol_ShouldReturnInvalid()
    {
        TemplateValidationResult result = await _sut.ValidateAsync("Hello @");

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("@", StringComparison.Ordinal));
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

        Func<Task> act = () => _sut.ValidateAsync("Hello @Name", cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private sealed class TemplateModel
    {
        public string? Name { get; set; }
        public string? Company { get; set; }
    }

    private sealed class TemplateModelWithFields
    {
        public string Code = string.Empty;
    }
}
