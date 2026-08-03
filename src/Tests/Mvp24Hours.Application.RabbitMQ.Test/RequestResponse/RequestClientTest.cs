using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.RequestResponse;
using Mvp24Hours.Infrastructure.RabbitMQ.Serialization;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mvp24Hours.Application.RabbitMQ.Test.RequestResponse;

[Trait("Category", "Unit")]
public class RequestClientTest
{
    private sealed class ConsumerCapture
    {
        public EventingBasicConsumer? Consumer { get; set; }
    }

    [Fact]
    public async Task GetResponseAsync_WhenResponseReceived_ShouldReturnSuccess()
    {
        var serializer = new JsonMessageSerializer();
        var capture = new ConsumerCapture();
        Mock<IModel> channelMock = CreateChannelMock(capture);
        Mock<IMvpRabbitMQConnection> connectionMock = CreateConnectionMock(channelMock);

        var client = new RequestClient<TestOrderCommand, TestOrderResponse>(
            connectionMock.Object,
            serializer,
            Options.Create(new RequestClientOptions { Exchange = "requests", RoutingKey = "order" }),
            NullLogger<RequestClient<TestOrderCommand, TestOrderResponse>>.Instance);

        Task<Response<TestOrderResponse>> responseTask = client.GetResponseAsync(
            new TestOrderCommand { Action = "create" },
            TimeSpan.FromSeconds(2));

        await WaitForPublishAndSimulateResponse(channelMock, capture.Consumer!, serializer,
            new TestOrderResponse { Success = true, Message = "created" });

        Response<TestOrderResponse> response = await responseTask;

        response.IsSuccess.Should().BeTrue();
        response.Message!.Success.Should().BeTrue();
        response.Status.Should().Be(ResponseStatus.Success);
        channelMock.Verify(c => c.BasicPublish(
            "requests",
            "order",
            false,
            It.IsAny<IBasicProperties>(),
            It.IsAny<ReadOnlyMemory<byte>>()), Times.Once);
    }

    [Fact]
    public async Task GetResponseAsync_WhenTimeout_ShouldReturnTimeoutStatus()
    {
        var serializer = new JsonMessageSerializer();
        var capture = new ConsumerCapture();
        Mock<IModel> channelMock = CreateChannelMock(capture);
        Mock<IMvpRabbitMQConnection> connectionMock = CreateConnectionMock(channelMock);

        var client = new RequestClient<TestOrderCommand, TestOrderResponse>(
            connectionMock.Object,
            serializer,
            Options.Create(new RequestClientOptions { TimeoutMilliseconds = 50 }));

        Response<TestOrderResponse> response = await client.GetResponseAsync(
            new TestOrderCommand { Action = "slow" },
            TimeSpan.FromMilliseconds(50));

        response.IsSuccess.Should().BeFalse();
        response.Status.Should().Be(ResponseStatus.Timeout);
    }

    [Fact]
    public async Task GetResponseAsync_WhenCancelled_ShouldReturnCancelledStatus()
    {
        var serializer = new JsonMessageSerializer();
        var capture = new ConsumerCapture();
        Mock<IModel> channelMock = CreateChannelMock(capture);
        Mock<IMvpRabbitMQConnection> connectionMock = CreateConnectionMock(channelMock);

        var client = new RequestClient<TestOrderCommand, TestOrderResponse>(
            connectionMock.Object,
            serializer);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Response<TestOrderResponse> response = await client.GetResponseAsync(
            new TestOrderCommand { Action = "cancel" },
            TimeSpan.FromSeconds(1),
            cts.Token);

        response.IsSuccess.Should().BeFalse();
        response.Status.Should().Be(ResponseStatus.Cancelled);
    }

    [Fact]
    public async Task GetResponseAsync_WhenNotConnectedAndConnectFails_ShouldReturnFailure()
    {
        var serializer = new JsonMessageSerializer();
        Mock<IMvpRabbitMQConnection> connectionMock = new();
        connectionMock.Setup(c => c.IsConnected).Returns(false);
        connectionMock.Setup(c => c.TryConnect()).Returns(false);

        var client = new RequestClient<TestOrderCommand, TestOrderResponse>(
            connectionMock.Object,
            serializer);

        Response<TestOrderResponse> response = await client.GetResponseAsync(new TestOrderCommand { Action = "fail" });

        response.IsSuccess.Should().BeFalse();
        response.Status.Should().Be(ResponseStatus.Failed);
        response.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Dispose_ShouldCloseChannel()
    {
        var serializer = new JsonMessageSerializer();
        var capture = new ConsumerCapture();
        Mock<IModel> channelMock = CreateChannelMock(capture);
        Mock<IMvpRabbitMQConnection> connectionMock = CreateConnectionMock(channelMock);

        var client = new RequestClient<TestOrderCommand, TestOrderResponse>(
            connectionMock.Object,
            serializer);

        _ = await client.GetResponseAsync(new TestOrderCommand { Action = "dispose" }, TimeSpan.FromMilliseconds(10));

        client.Dispose();

        channelMock.Verify(c => c.Close(), Times.Once);
        channelMock.Verify(c => c.Dispose(), Times.Once);
    }

    [Fact]
    public void Constructor_WithNullSerializer_ShouldThrow()
    {
        Mock<IMvpRabbitMQConnection> connectionMock = RabbitMQTestHelpers.CreateMockConnection();

        Action act = () => new RequestClient<TestOrderCommand, TestOrderResponse>(connectionMock.Object, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task GetResponseAsync_WithDefaultRoutingKey_ShouldUseRequestTypeName()
    {
        var serializer = new JsonMessageSerializer();
        var capture = new ConsumerCapture();
        Mock<IModel> channelMock = CreateChannelMock(capture);
        Mock<IMvpRabbitMQConnection> connectionMock = CreateConnectionMock(channelMock);

        var client = new RequestClient<TestOrderCommand, TestOrderResponse>(
            connectionMock.Object,
            serializer,
            Options.Create(new RequestClientOptions { Exchange = "requests", RoutingKey = null }));

        Task<Response<TestOrderResponse>> responseTask = client.GetResponseAsync(
            new TestOrderCommand { Action = "route" },
            TimeSpan.FromSeconds(1));

        await WaitForPublishAndSimulateResponse(channelMock, capture.Consumer!, serializer,
            new TestOrderResponse { Success = true });

        await responseTask;

        channelMock.Verify(c => c.BasicPublish(
            "requests",
            nameof(TestOrderCommand),
            false,
            It.IsAny<IBasicProperties>(),
            It.IsAny<ReadOnlyMemory<byte>>()), Times.Once);
    }

    private static Mock<IModel> CreateChannelMock(ConsumerCapture capture)
    {
        var channelMock = new Mock<IModel>();
        var propertiesMock = new Mock<IBasicProperties>();
        propertiesMock.SetupAllProperties();

        channelMock.Setup(c => c.IsOpen).Returns(true);
        channelMock.Setup(c => c.CreateBasicProperties()).Returns(propertiesMock.Object);
        channelMock.Setup(c => c.QueueDeclare(
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<IDictionary<string, object>>()))
            .Returns(new QueueDeclareOk("reply-queue", 0, 0));
        channelMock.Setup(c => c.BasicConsume(
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<IDictionary<string, object>>(),
                It.IsAny<IBasicConsumer>()))
            .Callback<string, bool, string, bool, bool, IDictionary<string, object>, IBasicConsumer>(
                (_, _, _, _, _, _, consumer) => capture.Consumer = (EventingBasicConsumer)consumer)
            .Returns("consumer-tag");

        return channelMock;
    }

    private static Mock<IMvpRabbitMQConnection> CreateConnectionMock(Mock<IModel> channelMock)
    {
        Mock<IMvpRabbitMQConnection> connectionMock = RabbitMQTestHelpers.CreateMockConnection();
        connectionMock.Setup(c => c.CreateModel()).Returns(channelMock.Object);
        return connectionMock;
    }

    private static async Task WaitForPublishAndSimulateResponse(
        Mock<IModel> channelMock,
        EventingBasicConsumer consumer,
        JsonMessageSerializer serializer,
        TestOrderResponse response)
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            try
            {
                channelMock.Verify(c => c.BasicPublish(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<IBasicProperties>(),
                    It.IsAny<ReadOnlyMemory<byte>>()), Times.AtLeastOnce);

                IInvocation? publishInvocation = channelMock.Invocations
                    .LastOrDefault(i => i.Method.Name == nameof(IModel.BasicPublish));
                string correlationId = ((IBasicProperties)publishInvocation!.Arguments[3]).CorrelationId!;

                SimulateResponse(consumer, serializer, response, correlationId);
                return;
            }
            catch (MockException)
            {
                await Task.Delay(10);
            }
        }

        throw new InvalidOperationException("BasicPublish was not invoked.");
    }

    private static void SimulateResponse(
        EventingBasicConsumer consumer,
        JsonMessageSerializer serializer,
        TestOrderResponse response,
        string correlationId)
    {
        var propertiesMock = new Mock<IBasicProperties>();
        propertiesMock.SetupAllProperties();
        propertiesMock.Setup(p => p.CorrelationId).Returns(correlationId);

        byte[] body = serializer.Serialize(response);
        consumer.HandleBasicDeliver("consumer-tag", 1, false, "requests", "order", propertiesMock.Object, body);
    }
}
