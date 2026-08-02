//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mvp24Hours.Infrastructure.Observability;
using Mvp24Hours.Infrastructure.Observability.Contract;

namespace Mvp24Hours.Infrastructure.Test.Support;

internal static class ObservabilityTestHelpers
{
    public static Mock<ISubsystemDiagnosticsProvider> CreateProviderMock(
        string subsystemName,
        SubsystemHealth health = SubsystemHealth.Healthy,
        Dictionary<string, object>? metrics = null,
        IReadOnlyList<ErrorInfo>? errors = null,
        bool throwOnDiagnostics = false,
        bool throwOnMetrics = false,
        bool throwOnErrors = false,
        Exception? diagnosticsException = null,
        Exception? metricsException = null,
        Exception? errorsException = null)
    {
        var mock = new Mock<ISubsystemDiagnosticsProvider>();
        mock.Setup(p => p.SubsystemName).Returns(subsystemName);

        if (throwOnDiagnostics)
        {
            mock.Setup(p => p.GetDiagnosticsAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(diagnosticsException ?? new InvalidOperationException("diagnostics failed"));
        }
        else
        {
            mock.Setup(p => p.GetDiagnosticsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SubsystemDiagnostics
                {
                    SubsystemName = subsystemName,
                    Health = health
                });
        }

        if (throwOnMetrics)
        {
            mock.Setup(p => p.GetMetricsAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(metricsException ?? new InvalidOperationException("metrics failed"));
        }
        else
        {
            mock.Setup(p => p.GetMetricsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(metrics ?? new Dictionary<string, object> { ["count"] = 1 });
        }

        if (throwOnErrors)
        {
            mock.Setup(p => p.GetRecentErrorsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(errorsException ?? new InvalidOperationException("errors failed"));
        }
        else
        {
            mock.Setup(p => p.GetRecentErrorsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(errors ?? []);
        }

        return mock;
    }

    public static InfrastructureDiagnostics CreateDiagnostics(
        IEnumerable<ISubsystemDiagnosticsProvider>? providers = null,
        ILogger<InfrastructureDiagnostics>? logger = null)
    {
        return new InfrastructureDiagnostics(
            providers ?? [],
            logger ?? NullLogger<InfrastructureDiagnostics>.Instance);
    }
}
