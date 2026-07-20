using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Infrastructure.Data.EFCore.Cqrs;
using Mvp24Hours.Infrastructure.Data.EFCore.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Testing;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Extensions;

[Trait("Category", "Unit")]
public class EFCoreCqrsIntegrationExtensionsTest
{
    [Fact]
    public void AddMvp24HoursNoOpEventDispatcher_ShouldResolveNoOpDispatcher()
    {
        var services = new ServiceCollection();
        services.AddMvp24HoursNoOpEventDispatcher();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        IDomainEventDispatcherEFCore dispatcher =
            scope.ServiceProvider.GetRequiredService<IDomainEventDispatcherEFCore>();

        dispatcher.Should().BeOfType<NoOpDomainEventDispatcher>();
    }

    [Fact]
    public void AddMvp24HoursUnitOfWorkWithEvents_ShouldResolveUnitOfWork()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursInMemoryDbContext<TestDbContext>($"UowEvents_{Guid.NewGuid():N}");
        services.AddMvp24HoursNoOpEventDispatcher();
        services.AddMvp24HoursUnitOfWorkWithEvents();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IUnitOfWorkWithEventsAsync>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursRepositoryWithEvents_ShouldResolveRepositoryAndUow()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursInMemoryDbContext<TestDbContext>($"RepoEvents_{Guid.NewGuid():N}");
        services.AddMvp24HoursNoOpEventDispatcher();
        services.AddMvp24HoursRepositoryWithEvents();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IUnitOfWorkWithEventsAsync>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestEntity>>().Should().NotBeNull();
    }

    [Fact]
    public void AddDomainEventInterceptor_ShouldAddInterceptorToOptions()
    {
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"Interceptor_{Guid.NewGuid():N}");

        optionsBuilder.AddDomainEventInterceptor(new NoOpDomainEventDispatcher());

        using var context = new TestDbContext(optionsBuilder.Options);
        context.Should().NotBeNull();
        context.Database.IsInMemory().Should().BeTrue();
    }

    [Fact]
    public void AddMvp24HoursDomainEventDispatcher_WithDelegate_ShouldResolveAdapter()
    {
        var services = new ServiceCollection();
        services.AddMvp24HoursDomainEventDispatcher(async (_, _) => await Task.CompletedTask);

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IDomainEventDispatcherEFCore>()
            .Should().BeOfType<DomainEventDispatcherAdapter>();
    }

    [Fact]
    public void AddMvp24HoursEFCoreCqrs_ShouldRegisterContextAndUow()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursEFCoreCqrs<TestDbContext>(
            (_, options) => options.UseInMemoryDatabase($"EfCqrs_{Guid.NewGuid():N}"),
            cqrs =>
            {
                cqrs.UseDomainEventInterceptor = true;
                cqrs.UseUnitOfWorkWithEvents = true;
                cqrs.UseNoOpDispatcherAsFallback = true;
            });

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<TestDbContext>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<DbContext>().Should().BeOfType<TestDbContext>();
        scope.ServiceProvider.GetRequiredService<IUnitOfWorkWithEventsAsync>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IDomainEventDispatcherEFCore>()
            .Should().BeOfType<NoOpDomainEventDispatcher>();
    }
}
