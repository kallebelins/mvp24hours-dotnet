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
public class RequestClientAdditionalCoverageTest
{
    private sealed class ConsumerCapture
    {
        public EventingBasicConsumer? Consumer { get; set; }
    }

    [Fact]
    public async Task GetResponseAsync_WhenDeserializeReturnsNull_ShouldReturnFailure()
    {
        var serializer = new Mock<IMessageSerializer>();
        serializer.Setup(s => s.ContentType).Returns("application/json");
        serializer.Setup(s => s.Serialize(It.IsAny<TestOrderCommand>())).Returns([]);
        serializer.Setup(s => s.Deserialize<TestOrderResponse>(It.IsAny<byte[]>())).Returns((TestOrderResponse?)null);

        var capture = new ConsumerCapture();
        Mock<IModel> channelMock = CreateChannelMock(capture);
        Mock<IMvpRabbitMQConnection> connectionMock = CreateConnectionMock(channelMock);

        var client = new RequestClient<TestOrderCommand, TestOrderResponse>(
            connectionMock.Object,
            serializer.Object,
            Options.Create(new RequestClientOptions { TimeoutMilliseconds = 500 }));

        Task<Response<TestOrderResponse>> responseTask = client.GetResponseAsync(
            new TestOrderCommand { Action = "null-response" },
            TimeSpan.FromSeconds(1));

        await WaitForPublishAndSimulateResponse(channelMock, capture.Consumer!, serializer.Object, []);

        Response<TestOrderResponse> response = await responseTask;

        response.IsSuccess.Should().BeFalse();
        response.Status.Should().Be(ResponseStatus.Failed);
    }

    [Fact]
    public async Task GetResponseAsync_WhenResponseHasEmptyCorrelationId_ShouldTimeout()
    {
        var serializer = new JsonMessageSerializer();
        var capture = new ConsumerCapture();
        Mock<IModel> channelMock = CreateChannelMock(capture);
        Mock<IMvpRabbitMQConnection> connectionMock = CreateConnectionMock(channelMock);

        var client = new RequestClient<TestOrderCommand, TestOrderResponse>(
            connectionMock.Object,
            serializer,
            Options.Create(new RequestClientOptions { TimeoutMilliseconds = 100 }));

        Task<Response<TestOrderResponse>> responseTask = client.GetResponseAsync(
            new TestOrderCommand { Action = "empty-corr" },
            TimeSpan.FromMilliseconds(100));

        await RabbitMQTestHelpers.WaitUntilAsync(
            () => channelMock.Invocations.Any(i => i.Method.Name == nameof(IModel.BasicPublish)),
            TimeSpan.FromSeconds(2));

        var propertiesMock = new Mock<IBasicProperties>();
        propertiesMock.SetupAllProperties();
        propertiesMock.Setup(p => p.CorrelationId).Returns(string.Empty);
        capture.Consumer!.HandleBasicDeliver(
            "consumer-tag",
            1,
            false,
            "requests",
            "order",
            propertiesMock.Object,
            serializer.Serialize(new TestOrderResponse { Success = true }));

        Response<TestOrderResponse> response = await responseTask;

        response.Status.Should().Be(ResponseStatus.Timeout);
    }

    [Fact]
    public async Task GetResponseAsync_WhenPublishThrows_ShouldReturnFailure()
    {
        var serializer = new JsonMessageSerializer();
        var capture = new ConsumerCapture();
        Mock<IModel> channelMock = CreateChannelMock(capture);
        channelMock.Setup(c => c.BasicPublish(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<IBasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>()))
            .Throws(new InvalidOperationException("publish failed"));

        Mock<IMvpRabbitMQConnection> connectionMock = CreateConnectionMock(channelMock);

        var client = new RequestClient<TestOrderCommand, TestOrderResponse>(
            connectionMock.Object,
            serializer);

        Response<TestOrderResponse> response = await client.GetResponseAsync(new TestOrderCommand { Action = "fail-publish" });

        response.IsSuccess.Should().BeFalse();
        response.Status.Should().Be(ResponseStatus.Failed);
        response.ErrorMessage.Should().Contain("publish failed");
    }

    [Fact]
    public async Task GetResponseAsync_SecondCall_ShouldReuseOpenChannel()
    {
        var serializer = new JsonMessageSerializer();
        var capture = new ConsumerCapture();
        Mock<IModel> channelMock = CreateChannelMock(capture);
        Mock<IMvpRabbitMQConnection> connectionMock = CreateConnectionMock(channelMock);

        var client = new RequestClient<TestOrderCommand, TestOrderResponse>(
            connectionMock.Object,
            serializer,
            Options.Create(new RequestClientOptions { TimeoutMilliseconds = 500 }));

        Task<Response<TestOrderResponse>> firstTask = client.GetResponseAsync(
            new TestOrderCommand { Action = "first" },
            TimeSpan.FromSeconds(1));
        await WaitForPublishAndSimulateResponse(channelMock, capture.Consumer!, serializer,
            new TestOrderResponse { Success = true });
        await firstTask;

        Task<Response<TestOrderResponse>> secondTask = client.GetResponseAsync(
            new TestOrderCommand { Action = "second" },
            TimeSpan.FromSeconds(1));
        await WaitForPublishAndSimulateResponse(channelMock, capture.Consumer!, serializer,
            new TestOrderResponse { Success = true });
        await secondTask;

        connectionMock.Verify(c => c.CreateModel(), Times.Once);
    }

    [Fact]
    public async Task Dispose_ShouldCancelPendingRequests()
    {
        var serializer = new JsonMessageSerializer();
        var capture = new ConsumerCapture();
        Mock<IModel> channelMock = CreateChannelMock(capture);
        Mock<IMvpRabbitMQConnection> connectionMock = CreateConnectionMock(channelMock);

        var client = new RequestClient<TestOrderCommand, TestOrderResponse>(
            connectionMock.Object,
            serializer,
            Options.Create(new RequestClientOptions { TimeoutMilliseconds = 5000 }));

        Task<Response<TestOrderResponse>> responseTask = client.GetResponseAsync(
            new TestOrderCommand { Action = "pending" },
            TimeSpan.FromSeconds(5));

        await RabbitMQTestHelpers.WaitUntilAsync(
            () => channelMock.Invocations.Any(i => i.Method.Name == nameof(IModel.BasicPublish)),
            TimeSpan.FromSeconds(2));

        client.Dispose();

        Response<TestOrderResponse> response = await responseTask;
        response.Status.Should().BeOneOf(ResponseStatus.Cancelled, ResponseStatus.Timeout, ResponseStatus.Failed);
    }

    [Fact]
    public void RequestClientOptions_Defaults_ShouldHaveExpectedValues()
    {
        var options = new RequestClientOptions();

        options.TimeoutMilliseconds.Should().BeGreaterThan(0);
        options.RoutingKey.Should().BeNull();
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
        IMessageSerializer serializer,
        byte[] responseBody)
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

                SimulateResponse(consumer, serializer, responseBody, correlationId);
                return;
            }
            catch (MockException)
            {
                await Task.Delay(10);
            }
        }

        throw new InvalidOperationException("BasicPublish was not invoked.");
    }

    private static async Task WaitForPublishAndSimulateResponse(
        Mock<IModel> channelMock,
        EventingBasicConsumer consumer,
        JsonMessageSerializer serializer,
        TestOrderResponse response)
    {
        byte[] body = serializer.Serialize(response);
        await WaitForPublishAndSimulateResponse(channelMock, consumer, serializer, body);
    }

    private static void SimulateResponse(
        EventingBasicConsumer consumer,
        IMessageSerializer serializer,
        byte[] body,
        string correlationId)
    {
        var propertiesMock = new Mock<IBasicProperties>();
        propertiesMock.SetupAllProperties();
        propertiesMock.Setup(p => p.CorrelationId).Returns(correlationId);
        consumer.HandleBasicDeliver("consumer-tag", 1, false, "requests", "order", propertiesMock.Object, body);
    }

    private static void SimulateResponse(
        EventingBasicConsumer consumer,
        JsonMessageSerializer serializer,
        TestOrderResponse response,
        string correlationId)
    {
        byte[] body = serializer.Serialize(response);
        SimulateResponse(consumer, serializer, body, correlationId);
    }
}
