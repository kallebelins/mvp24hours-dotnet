using Mvp24Hours.Infrastructure.RabbitMQ;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;

namespace Mvp24Hours.Application.RabbitMQ.Test.Hosted;

public class HostedTest
{
    [Fact]
    public async Task MvpRabbitMQHostedService_StartAsync_ShouldCompleteWithoutThrowing()
    {
        var options = new RabbitMQHostedOptions
        {
            Callback = _ => { },
            DueTime = Timeout.InfiniteTimeSpan,
            Period = Timeout.InfiniteTimeSpan
        };
        var service = new MvpRabbitMQHostedService(options);

        Func<Task> act = () => service.StartAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void MvpRabbitMQHostedService_WithNullOptions_ShouldThrow()
    {
        Action act = () => new MvpRabbitMQHostedService((RabbitMQHostedOptions)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task MvpRabbitMQHostedService_StopAsync_ShouldComplete()
    {
        var options = new RabbitMQHostedOptions
        {
            Callback = _ => { },
            DueTime = Timeout.InfiniteTimeSpan,
            Period = Timeout.InfiniteTimeSpan
        };
        var service = new MvpRabbitMQHostedService(options);

        await service.StartAsync(CancellationToken.None);

        Func<Task> act = () => service.StopAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void MvpRabbitMQHostedService_ShouldMapOptions()
    {
        object state = new();
        var due = TimeSpan.FromSeconds(2);
        var period = TimeSpan.FromSeconds(10);
        var options = new RabbitMQHostedOptions
        {
            Callback = _ => { },
            State = state,
            DueTime = due,
            Period = period
        };

        var service = new MvpRabbitMQHostedService(options);

        service.Should().NotBeNull();
        options.State.Should().BeSameAs(state);
        options.DueTime.Should().Be(due);
        options.Period.Should().Be(period);
    }
}
