//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Moq;
using Mvp24Hours.Infrastructure.Security.Contract;
using Mvp24Hours.Infrastructure.Security.Helpers;

namespace Mvp24Hours.Infrastructure.Test.Security.Helpers;

[Trait("Category", "Unit")]
public class SecretRotationHelperTest
{
    [Fact]
    public void Constructor_WithNullProvider_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new SecretRotationHelper(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("secretProvider");
    }

    [Fact]
    public void Constructor_WithLogger_ShouldNotThrow()
    {
        var provider = new Mock<ISecretProvider>();
        var logger = new Mock<ILogger<SecretRotationHelper>>();

        Action act = () => _ = new SecretRotationHelper(provider.Object, logger.Object);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NeedsRotationAsync_WithInvalidName_ShouldThrowArgumentException(string? name)
    {
        SecretRotationHelper helper = CreateHelper();

        Func<Task> act = () => helper.NeedsRotationAsync(name!, TimeSpan.FromDays(30));

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("secretName");
    }

    [Fact]
    public async Task NeedsRotationAsync_WhenLastRotationUnknown_ShouldReturnTrue()
    {
        SecretRotationHelper helper = CreateHelper();

        bool needsRotation = await helper.NeedsRotationAsync("ApiKey", TimeSpan.FromDays(90));

        needsRotation.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RotateSecretAsync_WithInvalidName_ShouldThrowArgumentException(string? name)
    {
        SecretRotationHelper helper = CreateHelper();

        Func<Task> act = () => helper.RotateSecretAsync(name!, () => Task.FromResult("new-secret"));

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("secretName");
    }

    [Fact]
    public async Task RotateSecretAsync_WithNullCallback_ShouldThrowArgumentNullException()
    {
        SecretRotationHelper helper = CreateHelper();

        Func<Task> act = () => helper.RotateSecretAsync("ApiKey", null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("generateNewSecret");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RotateSecretAsync_WhenGeneratedSecretEmpty_ShouldThrowInvalidOperationException(string? generated)
    {
        SecretRotationHelper helper = CreateHelper();

        Func<Task> act = () => helper.RotateSecretAsync("ApiKey", () => Task.FromResult(generated!));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Generated secret cannot be null or empty*");
    }

    [Fact]
    public async Task RotateSecretAsync_WithValidCallback_ShouldReturnNewSecret()
    {
        var provider = new Mock<ISecretProvider>(MockBehavior.Strict);
        var helper = new SecretRotationHelper(provider.Object);

        string result = await helper.RotateSecretAsync("ApiKey", () => Task.FromResult("rotated-value"));

        result.Should().Be("rotated-value");
        provider.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSecretCreationDateAsync_ShouldReturnNull()
    {
        SecretRotationHelper helper = CreateHelper();

        DateTime? date = await helper.GetSecretCreationDateAsync("ApiKey");

        date.Should().BeNull();
    }

    [Fact]
    public async Task GetLastRotationDateAsync_ShouldReturnNull()
    {
        SecretRotationHelper helper = CreateHelper();

        DateTime? date = await helper.GetLastRotationDateAsync("ApiKey");

        date.Should().BeNull();
    }

    private static SecretRotationHelper CreateHelper()
    {
        return new SecretRotationHelper(new Mock<ISecretProvider>().Object);
    }
}
