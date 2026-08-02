using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.RequestResponse;
using Mvp24Hours.Infrastructure.RabbitMQ.Serialization;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing;

namespace Mvp24Hours.Application.RabbitMQ.Test.RequestResponse;

public class RequestResponseTest
{
    [Fact]
    public async Task TestHarness_RequestAsync_WithRequestClient_ShouldReturnSuccessResponse()
    {
        var harness = TestHarness.Create(services => services.AddSingleton<IRequestClient<TestOrderCommand, TestOrderResponse>, FakeRequestClient>());

        Response<TestOrderResponse> response = await harness.RequestAsync<TestOrderCommand, TestOrderResponse>(
            new TestOrderCommand { Action = "create" });

        response.IsSuccess.Should().BeTrue();
        response.Message!.Success.Should().BeTrue();
    }

    [Fact]
    public void RequestClient_Options_ShouldExposeTimeout()
    {
        Mock<IMvpRabbitMQConnection> connection = RabbitMQTestHelpers.CreateMockConnection();
        var serializer = new JsonMessageSerializer();
        var client = new RequestClient<TestOrderCommand, TestOrderResponse>(
            connection.Object,
            serializer,
            Options.Create(new RequestClientOptions { TimeoutMilliseconds = 1500 }));

        client.Timeout.Should().Be(TimeSpan.FromMilliseconds(1500));
    }

    [Fact]
    public void RequestClient_Constructor_WithNullConnection_ShouldThrow()
    {
        var serializer = new JsonMessageSerializer();

        Action act = () => new RequestClient<TestOrderCommand, TestOrderResponse>(null!, serializer);

        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class FakeRequestClient : IRequestClient<TestOrderCommand, TestOrderResponse>
    {
        public TimeSpan Timeout => TimeSpan.FromSeconds(5);

        public Task<Response<TestOrderResponse>> GetResponseAsync(
            TestOrderCommand request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Response<TestOrderResponse>.Success(
                new TestOrderResponse { Success = true, Message = request.Action }));
        }

        public Task<Response<TestOrderResponse>> GetResponseAsync(
            TestOrderCommand request,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            return GetResponseAsync(request, cancellationToken);
        }
    }
}
