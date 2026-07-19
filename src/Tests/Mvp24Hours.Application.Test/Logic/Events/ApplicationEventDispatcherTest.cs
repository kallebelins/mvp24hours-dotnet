using Microsoft.Extensions.Options;
using Mvp24Hours.Application.Contract.Events;
using Mvp24Hours.Application.Logic.Events;
using Mvp24Hours.Application.Test.Support;

namespace Mvp24Hours.Application.Test.Logic.Events;

[Trait("Category", "Unit")]
public class ApplicationEventDispatcherTest
{
    [Fact]
    public async Task DispatchAsync_WithNullEvent_ShouldThrow()
    {
        ServiceProvider sp = ApplicationTestHelpers.CreateEventDispatcherServices();
        IApplicationEventDispatcher dispatcher = sp.GetRequiredService<IApplicationEventDispatcher>();

        Func<Task> act = async () => await dispatcher.DispatchAsync<TestApplicationEvent>(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DispatchAsync_WithNoHandlers_ShouldComplete()
    {
        ServiceProvider sp = ApplicationTestHelpers.CreateEventDispatcherServices();
        IApplicationEventDispatcher dispatcher = sp.GetRequiredService<IApplicationEventDispatcher>();

        await dispatcher.DispatchAsync(new TestApplicationEvent { Payload = "noop" });
    }

    [Fact]
    public async Task DispatchAsync_Sequential_ShouldInvokeAllHandlers()
    {
        var handler1 = new CapturingEventHandler();
        var handler2 = new CapturingEventHandler();
        ServiceProvider sp = ApplicationTestHelpers.CreateEventDispatcherServices(
            o => o.Strategy = EventDispatchStrategy.Sequential,
            handler1, handler2);
        IApplicationEventDispatcher dispatcher = sp.GetRequiredService<IApplicationEventDispatcher>();
        var @event = new TestApplicationEvent { Payload = "test" };

        await dispatcher.DispatchAsync(@event);

        handler1.Handled.Should().ContainSingle(e => e.Payload == "test");
        handler2.Handled.Should().ContainSingle();
    }

    [Fact]
    public async Task DispatchAsync_Parallel_ShouldInvokeHandlersConcurrently()
    {
        var handler = new CapturingEventHandler();
        ServiceProvider sp = ApplicationTestHelpers.CreateEventDispatcherServices(
            o => o.Strategy = EventDispatchStrategy.Parallel,
            handler);
        IApplicationEventDispatcher dispatcher = sp.GetRequiredService<IApplicationEventDispatcher>();

        await dispatcher.DispatchAsync(new TestApplicationEvent { Payload = "parallel" });

        handler.Handled.Should().HaveCount(1);
    }

    [Fact]
    public async Task DispatchAsync_WithOutbox_ShouldStoreInsteadOfDispatching()
    {
        var outbox = new InMemoryApplicationEventOutbox();
        var handler = new CapturingEventHandler();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IApplicationEventOutbox>(outbox);
        services.AddSingleton<IApplicationEventHandler<TestApplicationEvent>>(handler);
        services.AddOptions<ApplicationEventDispatcherOptions>().Configure(o => o.UseOutbox = true);
        services.AddSingleton<IApplicationEventDispatcher, ApplicationEventDispatcher>();
        ServiceProvider sp = services.BuildServiceProvider();
        IApplicationEventDispatcher dispatcher = sp.GetRequiredService<IApplicationEventDispatcher>();

        await dispatcher.DispatchAsync(new TestApplicationEvent { Payload = "outbox" });

        outbox.GetAll().Should().HaveCount(1);
        handler.Handled.Should().BeEmpty();
    }

    [Fact]
    public async Task DispatchAsync_ContinueOnError_ShouldNotThrowWhenHandlerFails()
    {
        ServiceProvider sp = ApplicationTestHelpers.CreateEventDispatcherServices(
            o =>
            {
                o.Strategy = EventDispatchStrategy.Sequential;
                o.ContinueOnError = true;
            },
            new FailingEventHandler(),
            new CapturingEventHandler());
        IApplicationEventDispatcher dispatcher = sp.GetRequiredService<IApplicationEventDispatcher>();

        Func<Task> act = async () => await dispatcher.DispatchAsync(new TestApplicationEvent());

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DispatchAsync_MultipleEvents_ShouldDispatchEach()
    {
        var handler = new CapturingEventHandler();
        ServiceProvider sp = ApplicationTestHelpers.CreateEventDispatcherServices(null, handler);
        IApplicationEventDispatcher dispatcher = sp.GetRequiredService<IApplicationEventDispatcher>();

        await dispatcher.DispatchAsync(new TestApplicationEvent { Payload = "1" });
        await dispatcher.DispatchAsync(new TestApplicationEvent { Payload = "2" });

        handler.Handled.Should().HaveCount(2);
    }
}
