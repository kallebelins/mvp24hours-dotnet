//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mvp24Hours.Infrastructure.Observability;
using Mvp24Hours.Infrastructure.Observability.Contract;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.Observability;

[Trait("Category", "Unit")]
public class InfrastructureDiagnosticsTest
{
    [Fact]
    public void Constructor_WithNullProviders_ShouldThrowArgumentNullException()
    {
        Action act = () => new InfrastructureDiagnostics(null!, NullLogger<InfrastructureDiagnostics>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("providers");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        Action act = () => new InfrastructureDiagnostics([], null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task GetHealthStatusAsync_WithEmptyProviders_ShouldReturnEmptyDictionary()
    {
        InfrastructureDiagnostics sut = ObservabilityTestHelpers.CreateDiagnostics([]);

        Dictionary<string, SubsystemHealth> result = await sut.GetHealthStatusAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAggregatedMetricsAsync_WithEmptyProviders_ShouldReturnEmptyDictionary()
    {
        InfrastructureDiagnostics sut = ObservabilityTestHelpers.CreateDiagnostics([]);

        Dictionary<string, object> result = await sut.GetAggregatedMetricsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRecentErrorsAsync_WithEmptyProviders_ShouldReturnEmptyDictionary()
    {
        InfrastructureDiagnostics sut = ObservabilityTestHelpers.CreateDiagnostics([]);

        Dictionary<string, IReadOnlyList<ErrorInfo>> result = await sut.GetRecentErrorsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetHealthStatusAsync_WithMultipleProviders_ShouldAggregateHealth()
    {
        Mock<ISubsystemDiagnosticsProvider> emailProvider = ObservabilityTestHelpers.CreateProviderMock(
            "Email",
            SubsystemHealth.Healthy);
        Mock<ISubsystemDiagnosticsProvider> smsProvider = ObservabilityTestHelpers.CreateProviderMock(
            "Sms",
            SubsystemHealth.Degraded);

        InfrastructureDiagnostics sut = ObservabilityTestHelpers.CreateDiagnostics(
            [emailProvider.Object, smsProvider.Object]);

        Dictionary<string, SubsystemHealth> result = await sut.GetHealthStatusAsync();

        result.Should().HaveCount(2);
        result["Email"].Should().Be(SubsystemHealth.Healthy);
        result["Sms"].Should().Be(SubsystemHealth.Degraded);
    }

    [Fact]
    public async Task GetSubsystemDiagnosticsAsync_WithMixedCaseName_ShouldBeCaseInsensitive()
    {
        Mock<ISubsystemDiagnosticsProvider> provider = ObservabilityTestHelpers.CreateProviderMock(
            "Email",
            SubsystemHealth.Healthy);

        InfrastructureDiagnostics sut = ObservabilityTestHelpers.CreateDiagnostics([provider.Object]);

        SubsystemDiagnostics? result = await sut.GetSubsystemDiagnosticsAsync("email");

        result.Should().NotBeNull();
        result!.SubsystemName.Should().Be("Email");
        result.Health.Should().Be(SubsystemHealth.Healthy);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSubsystemDiagnosticsAsync_WithNullOrWhitespaceName_ShouldThrowArgumentException(string? subsystemName)
    {
        InfrastructureDiagnostics sut = ObservabilityTestHelpers.CreateDiagnostics([]);

        Func<Task> act = () => sut.GetSubsystemDiagnosticsAsync(subsystemName!);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("subsystemName");
    }

    [Fact]
    public async Task GetSubsystemDiagnosticsAsync_WithUnknownSubsystem_ShouldReturnNull()
    {
        InfrastructureDiagnostics sut = ObservabilityTestHelpers.CreateDiagnostics([]);

        SubsystemDiagnostics? result = await sut.GetSubsystemDiagnosticsAsync("Unknown");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetHealthStatusAsync_WhenProviderThrowsOnDiagnostics_ShouldMarkUnhealthy()
    {
        Mock<ISubsystemDiagnosticsProvider> provider = ObservabilityTestHelpers.CreateProviderMock(
            "Email",
            throwOnDiagnostics: true);

        InfrastructureDiagnostics sut = ObservabilityTestHelpers.CreateDiagnostics([provider.Object]);

        Dictionary<string, SubsystemHealth> result = await sut.GetHealthStatusAsync();

        result.Should().ContainKey("Email");
        result["Email"].Should().Be(SubsystemHealth.Unhealthy);
    }

    [Fact]
    public async Task GetSubsystemDiagnosticsAsync_WhenProviderThrowsOnDiagnostics_ShouldReturnSyntheticUnhealthy()
    {
        var exception = new InvalidOperationException("provider unavailable");
        Mock<ISubsystemDiagnosticsProvider> provider = ObservabilityTestHelpers.CreateProviderMock(
            "Email",
            throwOnDiagnostics: true,
            diagnosticsException: exception);

        InfrastructureDiagnostics sut = ObservabilityTestHelpers.CreateDiagnostics([provider.Object]);

        SubsystemDiagnostics? result = await sut.GetSubsystemDiagnosticsAsync("Email");

        result.Should().NotBeNull();
        result!.SubsystemName.Should().Be("Email");
        result.Health.Should().Be(SubsystemHealth.Unhealthy);
        result.ErrorMessage.Should().Be(exception.Message);
    }

    [Fact]
    public async Task GetAggregatedMetricsAsync_ShouldPrefixMetricsWithSubsystemName()
    {
        Mock<ISubsystemDiagnosticsProvider> emailProvider = ObservabilityTestHelpers.CreateProviderMock(
            "Email",
            metrics: new Dictionary<string, object> { ["sent"] = 10 });
        Mock<ISubsystemDiagnosticsProvider> smsProvider = ObservabilityTestHelpers.CreateProviderMock(
            "Sms",
            metrics: new Dictionary<string, object> { ["sent"] = 5 });

        InfrastructureDiagnostics sut = ObservabilityTestHelpers.CreateDiagnostics(
            [emailProvider.Object, smsProvider.Object]);

        Dictionary<string, object> result = await sut.GetAggregatedMetricsAsync();

        result.Should().HaveCount(2);
        result["Email.sent"].Should().Be(10);
        result["Sms.sent"].Should().Be(5);
    }

    [Fact]
    public async Task GetRecentErrorsAsync_WithEmptyErrorLists_ShouldOmitSubsystems()
    {
        Mock<ISubsystemDiagnosticsProvider> emptyProvider = ObservabilityTestHelpers.CreateProviderMock(
            "Email",
            errors: []);
        Mock<ISubsystemDiagnosticsProvider> errorProvider = ObservabilityTestHelpers.CreateProviderMock(
            "Sms",
            errors:
            [
                new ErrorInfo
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    Message = "send failed",
                    ErrorType = nameof(InvalidOperationException)
                }
            ]);

        InfrastructureDiagnostics sut = ObservabilityTestHelpers.CreateDiagnostics(
            [emptyProvider.Object, errorProvider.Object]);

        Dictionary<string, IReadOnlyList<ErrorInfo>> result = await sut.GetRecentErrorsAsync();

        result.Should().ContainKey("Sms");
        result.Should().NotContainKey("Email");
        result["Sms"].Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAggregatedMetricsAsync_WhenProviderThrowsOnMetrics_ShouldSkipProvider()
    {
        Mock<ISubsystemDiagnosticsProvider> failingProvider = ObservabilityTestHelpers.CreateProviderMock(
            "Email",
            throwOnMetrics: true);
        Mock<ISubsystemDiagnosticsProvider> healthyProvider = ObservabilityTestHelpers.CreateProviderMock(
            "Sms",
            metrics: new Dictionary<string, object> { ["count"] = 3 });

        InfrastructureDiagnostics sut = ObservabilityTestHelpers.CreateDiagnostics(
            [failingProvider.Object, healthyProvider.Object]);

        Dictionary<string, object> result = await sut.GetAggregatedMetricsAsync();

        result.Should().ContainKey("Sms.count");
        result.Should().NotContainKey("Email.count");
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetRecentErrorsAsync_WhenProviderThrowsOnErrors_ShouldSkipProvider()
    {
        Mock<ISubsystemDiagnosticsProvider> failingProvider = ObservabilityTestHelpers.CreateProviderMock(
            "Email",
            throwOnErrors: true);
        Mock<ISubsystemDiagnosticsProvider> healthyProvider = ObservabilityTestHelpers.CreateProviderMock(
            "Sms",
            errors:
            [
                new ErrorInfo
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    Message = "timeout"
                }
            ]);

        InfrastructureDiagnostics sut = ObservabilityTestHelpers.CreateDiagnostics(
            [failingProvider.Object, healthyProvider.Object]);

        Dictionary<string, IReadOnlyList<ErrorInfo>> result = await sut.GetRecentErrorsAsync();

        result.Should().ContainKey("Sms");
        result.Should().NotContainKey("Email");
    }
}
