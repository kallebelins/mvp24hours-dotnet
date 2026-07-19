using Mvp24Hours.Application.Contract.Events;
using Mvp24Hours.Application.Test.Support;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;

namespace Mvp24Hours.Application.Test.Logic.Events;

[Trait("Category", "Unit")]
public class EventAwareCommandServiceBaseAsyncTest
{
    [Fact]
    public async Task AddAsync_OnSuccess_ShouldDispatchEntityCreatedEvent()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var dispatcher = new Mock<IApplicationEventDispatcher>();
        var service = new TestEventAwareCommandService(uow.Object, dispatcher.Object, new AppTestEntityValidator());
        var entity = new AppTestEntity { Name = "Created" };

        IBusinessResult<int> result = await service.AddAsync(entity);

        result.Data.Should().Be(1);
        dispatcher.Verify(
            d => d.DispatchAsync(It.IsAny<EntityCreatedEvent<AppTestEntity>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ModifyAsync_OnSuccess_ShouldDispatchEntityUpdatedEvent()
    {
        (Mock<IUnitOfWorkAsync> uow, _) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var dispatcher = new Mock<IApplicationEventDispatcher>();
        var service = new TestEventAwareCommandService(uow.Object, dispatcher.Object, new AppTestEntityValidator());

        await service.ModifyAsync(new AppTestEntity { Id = 1, Name = "Updated" });

        dispatcher.Verify(
            d => d.DispatchAsync(It.IsAny<EntityUpdatedEvent<AppTestEntity>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_OnSuccess_ShouldDispatchEntityDeletedEvent()
    {
        (Mock<IUnitOfWorkAsync> uow, _) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var dispatcher = new Mock<IApplicationEventDispatcher>();
        var service = new TestEventAwareCommandService(uow.Object, dispatcher.Object);
        var entity = new AppTestEntity { Id = 1, Name = "Deleted" };

        await service.RemoveAsync(entity);

        dispatcher.Verify(
            d => d.DispatchAsync(It.IsAny<EntityDeletedEvent<AppTestEntity>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddAsync_WithDispatchEventsDisabled_ShouldNotDispatch()
    {
        (Mock<IUnitOfWorkAsync> uow, _) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var dispatcher = new Mock<IApplicationEventDispatcher>();
        var service = new TestEventAwareCommandService(uow.Object, dispatcher.Object, new AppTestEntityValidator());
        service.SetDispatchEvents(false);

        await service.AddAsync(new AppTestEntity { Name = "Silent" });

        dispatcher.Verify(
            d => d.DispatchAsync(It.IsAny<IApplicationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddAsync_InvalidEntity_ShouldNotDispatchEvent()
    {
        (Mock<IUnitOfWorkAsync> uow, _) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var dispatcher = new Mock<IApplicationEventDispatcher>();
        var service = new TestEventAwareCommandService(uow.Object, dispatcher.Object, new AppTestEntityValidator());

        await service.AddAsync(new AppTestEntity { Name = "" });

        dispatcher.Verify(
            d => d.DispatchAsync(It.IsAny<EntityCreatedEvent<AppTestEntity>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
