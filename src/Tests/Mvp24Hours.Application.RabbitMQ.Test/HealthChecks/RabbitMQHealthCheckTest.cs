using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.HealthChecks;
using RabbitMQ.Client;

namespace Mvp24Hours.Application.RabbitMQ.Test.HealthChecks;

[Trait("Category", "Unit")]
public class RabbitMQHealthCheckTest
{
    [Fact]
    public void Constructor_WithNullConnection_ShouldThrow()
    {
        Action act = () => new RabbitMQHealthCheck(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnected_ShouldReturnHealthyWithChannelData()
    {
        Mock<IMvpRabbitMQConnection> connection = RabbitMQTestHelpers.CreateMockConnection(isConnected: true);
        var healthCheck = new RabbitMQHealthCheck(connection.Object);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey("channelIsOpen");
        result.Data.Should().ContainKey("host");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDisconnectedButReconnects_ShouldReturnHealthy()
    {
        Mock<IMvpRabbitMQConnection> connection = RabbitMQTestHelpers.CreateMockConnection(isConnected: false);
        connection.Setup(c => c.TryConnect()).Returns(true);
        var healthCheck = new RabbitMQHealthCheck(connection.Object);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("reconnect");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDisconnectedAndCannotReconnect_ShouldReturnUnhealthyWithRetryCount()
    {
        Mock<IMvpRabbitMQConnection> connection = RabbitMQTestHelpers.CreateMockConnection(isConnected: false);
        connection.Setup(c => c.TryConnect()).Returns(false);
        var healthCheck = new RabbitMQHealthCheck(connection.Object);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Data.Should().ContainKey("retryCount");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCreateModelThrows_ShouldReturnUnhealthyWithExceptionData()
    {
        var connection = new Mock<IMvpRabbitMQConnection>();
        connection.Setup(c => c.IsConnected).Returns(true);
        connection.Setup(c => c.CreateModel()).Throws(new InvalidOperationException("channel down"));
        var healthCheck = new RabbitMQHealthCheck(connection.Object);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Exception.Should().BeOfType<InvalidOperationException>();
        result.Data.Should().ContainKey("error");
        result.Data["error"].Should().Be("channel down");
        result.Data.Should().ContainKey("exceptionType");
        result.Data["exceptionType"].Should().Be(nameof(InvalidOperationException));
    }

    [Fact]
    public async Task CheckHealthAsync_WhenIsConnectedThrows_ShouldReturnUnhealthy()
    {
        var connection = new Mock<IMvpRabbitMQConnection>();
        connection.Setup(c => c.IsConnected).Throws(new TimeoutException("connection check timed out"));
        var healthCheck = new RabbitMQHealthCheck(connection.Object);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Data["exceptionType"].Should().Be(nameof(TimeoutException));
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnected_ShouldDisposeChannel()
    {
        var channelMock = new Mock<IModel>();
        channelMock.Setup(c => c.IsOpen).Returns(true);
        var connection = new Mock<IMvpRabbitMQConnection>();
        connection.Setup(c => c.IsConnected).Returns(true);
        connection.Setup(c => c.CreateModel()).Returns(channelMock.Object);

        var healthCheck = new RabbitMQHealthCheck(connection.Object);
        await healthCheck.CheckHealthAsync(new HealthCheckContext());

        channelMock.Verify(c => c.Dispose(), Times.Once);
    }
}
