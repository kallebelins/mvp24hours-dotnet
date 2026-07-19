using Mvp24Hours.Application.Logic.Events;
using Mvp24Hours.Application.Test.Support;

namespace Mvp24Hours.Application.Test.Logic.Events;

[Trait("Category", "Unit")]
public class MediatorApplicationEventAdapterTest
{
    [Fact]
    public async Task HandleAsync_WithoutCqrsModule_ShouldCompleteWithoutError()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        ServiceProvider sp = services.BuildServiceProvider();
        var adapter = new MediatorApplicationEventAdapter<TestApplicationEvent>(sp);

        Func<Task> act = async () => await adapter.HandleAsync(new TestApplicationEvent { Payload = "bridge" });

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void CreateAdapterType_ForApplicationEvent_ShouldReturnClosedGenericType()
    {
        Type adapterType = MediatorApplicationEventAdapterFactory.CreateAdapterType(typeof(TestApplicationEvent));

        adapterType.Should().Be(typeof(MediatorApplicationEventAdapter<TestApplicationEvent>));
    }

    [Fact]
    public void CreateAdapterType_ForNonApplicationEvent_ShouldThrow()
    {
        Func<Type> act = () => MediatorApplicationEventAdapterFactory.CreateAdapterType(typeof(string));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ApplicationEventMediatorNotification_ShouldWrapEventProperties()
    {
        var source = new TestApplicationEvent
        {
            CorrelationId = "corr-1",
            Payload = "payload"
        };

        var notification = new ApplicationEventMediatorNotification<TestApplicationEvent>(source);

        notification.Event.Payload.Should().Be("payload");
        notification.EventId.Should().Be(source.EventId);
        notification.CorrelationId.Should().Be("corr-1");
    }

    [Fact]
    public void ApplicationEventMediatorNotification_WithNullEvent_ShouldThrow()
    {
        Func<ApplicationEventMediatorNotification<TestApplicationEvent>> act =
            () => new ApplicationEventMediatorNotification<TestApplicationEvent>(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
