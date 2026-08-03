using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy;
using Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy.Contract;
using RabbitMQ.Client;

namespace Mvp24Hours.Application.RabbitMQ.Test.MultiTenancy;

[Trait("Category", "Unit")]
public class TenantDeadLetterQueueHelperTest
{
    private static TenantDeadLetterQueueHelper CreateHelper(
        TenantRabbitMQOptions? options = null,
        Mock<ITenantConnectionFactory>? factoryMock = null)
    {
        options ??= new TenantRabbitMQOptions { UseTenantSpecificDeadLetterQueues = true };
        factoryMock ??= CreateFactoryMock();

        return new TenantDeadLetterQueueHelper(
            Options.Create(options),
            factoryMock.Object);
    }

    private static Mock<ITenantConnectionFactory> CreateFactoryMock()
    {
        var channelMock = new Mock<IModel>();
        var propertiesMock = new Mock<IBasicProperties>();
        propertiesMock.SetupAllProperties();
        channelMock.Setup(c => c.CreateBasicProperties()).Returns(propertiesMock.Object);

        var factoryMock = new Mock<ITenantConnectionFactory>();
        factoryMock.Setup(f => f.GetOrCreateChannel(It.IsAny<string>())).Returns(channelMock.Object);
        return factoryMock;
    }

    [Fact]
    public void GetDeadLetterQueueName_ShouldApplyTemplate()
    {
        TenantDeadLetterQueueHelper helper = CreateHelper();

        helper.GetDeadLetterQueueName("acme").Should().Be("acme_dlq");
    }

    [Fact]
    public void GetDeadLetterExchangeName_ShouldApplyTemplate()
    {
        TenantDeadLetterQueueHelper helper = CreateHelper();

        helper.GetDeadLetterExchangeName("acme").Should().Be("acme_dlx");
    }

    [Fact]
    public void GetDeadLetterQueueName_WithNullTenantId_ShouldThrow()
    {
        TenantDeadLetterQueueHelper helper = CreateHelper();

        Action act = () => helper.GetDeadLetterQueueName(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetDeadLetterArguments_WhenDisabled_ShouldReturnEmpty()
    {
        TenantDeadLetterQueueHelper helper = CreateHelper(new TenantRabbitMQOptions
        {
            UseTenantSpecificDeadLetterQueues = false
        });

        Dictionary<string, object> args = helper.GetDeadLetterArguments("tenant-a");

        args.Should().BeEmpty();
    }

    [Fact]
    public void GetDeadLetterArguments_WhenEnabled_ShouldReturnDeadLetterKeys()
    {
        TenantDeadLetterQueueHelper helper = CreateHelper();

        Dictionary<string, object> args = helper.GetDeadLetterArguments("tenant-a");

        args.Should().ContainKey("x-dead-letter-exchange");
        args.Should().ContainKey("x-dead-letter-routing-key");
        args["x-dead-letter-exchange"].Should().Be("tenant-a_dlx");
        args["x-dead-letter-routing-key"].Should().Be("tenant-a_dlq");
    }

    [Fact]
    public void EnsureDeadLetterInfrastructure_WhenDisabled_ShouldNotCreateChannel()
    {
        Mock<ITenantConnectionFactory> factoryMock = CreateFactoryMock();
        TenantDeadLetterQueueHelper helper = CreateHelper(
            new TenantRabbitMQOptions { UseTenantSpecificDeadLetterQueues = false },
            factoryMock);

        helper.EnsureDeadLetterInfrastructure("tenant-a");

        factoryMock.Verify(f => f.GetOrCreateChannel(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void EnsureDeadLetterInfrastructure_ShouldDeclareExchangeQueueAndBind()
    {
        var channelMock = new Mock<IModel>();
        var factoryMock = new Mock<ITenantConnectionFactory>();
        factoryMock.Setup(f => f.GetOrCreateChannel("tenant-a")).Returns(channelMock.Object);

        TenantDeadLetterQueueHelper helper = CreateHelper(factoryMock: factoryMock);

        helper.EnsureDeadLetterInfrastructure("tenant-a");
        helper.EnsureDeadLetterInfrastructure("tenant-a");

        channelMock.Verify(c => c.ExchangeDeclare(
            "tenant-a_dlx", ExchangeType.Direct, true, false, null), Times.Once);
        channelMock.Verify(c => c.QueueDeclare(
            "tenant-a_dlq", true, false, false, It.IsAny<IDictionary<string, object>>()), Times.Once);
        channelMock.Verify(c => c.QueueBind(
            "tenant-a_dlq", "tenant-a_dlx", "tenant-a_dlq", null), Times.Once);
        factoryMock.Verify(f => f.GetOrCreateChannel("tenant-a"), Times.Once);
    }

    [Fact]
    public void SendToDeadLetterQueue_ShouldPublishWithReasonHeaders()
    {
        var channelMock = new Mock<IModel>();
        var propertiesMock = new Mock<IBasicProperties>();
        propertiesMock.SetupAllProperties();
        channelMock.Setup(c => c.CreateBasicProperties()).Returns(propertiesMock.Object);

        var factoryMock = new Mock<ITenantConnectionFactory>();
        factoryMock.Setup(f => f.GetOrCreateChannel("tenant-a")).Returns(channelMock.Object);

        TenantDeadLetterQueueHelper helper = CreateHelper(factoryMock: factoryMock);
        var originalProperties = new Mock<IBasicProperties>();
        originalProperties.SetupAllProperties();
        originalProperties.Object.MessageId = "msg-1";
        originalProperties.Object.CorrelationId = "corr-1";
        originalProperties.Object.ContentType = "application/json";
        originalProperties.Object.Headers = new Dictionary<string, object> { ["x-custom"] = "v" };

        helper.SendToDeadLetterQueue("tenant-a", [1, 2, 3], originalProperties.Object, "max retries");

        factoryMock.Verify(f => f.GetOrCreateChannel("tenant-a"), Times.AtLeast(2));
    }

    [Fact]
    public void SendToDeadLetterQueue_WithNullBody_ShouldThrow()
    {
        TenantDeadLetterQueueHelper helper = CreateHelper();

        Action act = () => helper.SendToDeadLetterQueue("tenant-a", null!, null, "reason");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EnsureDeadLetterInfrastructure_WhenChannelThrows_ShouldRethrow()
    {
        var factoryMock = new Mock<ITenantConnectionFactory>();
        factoryMock.Setup(f => f.GetOrCreateChannel("tenant-a"))
            .Throws(new InvalidOperationException("channel failed"));

        TenantDeadLetterQueueHelper helper = CreateHelper(factoryMock: factoryMock);

        Action act = () => helper.EnsureDeadLetterInfrastructure("tenant-a");

        act.Should().Throw<InvalidOperationException>();
    }
}
