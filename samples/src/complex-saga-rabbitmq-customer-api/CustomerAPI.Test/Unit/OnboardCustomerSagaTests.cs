using CustomerAPI.Application.Repositories;
using CustomerAPI.Application.Sagas;
using CustomerAPI.Application.Sagas.Steps;
using CustomerAPI.Domain.Repositories;
using CustomerAPI.Domain.Sagas;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.Cqrs.Saga;

namespace CustomerAPI.Test.Unit;

[Trait("Category", "Unit")]
public class OnboardCustomerSagaTests
{
    private static IServiceProvider CreateServiceProvider(InMemoryCustomerRepository? repository = null)
    {
        repository ??= new InMemoryCustomerRepository();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICustomerRepository>(repository);
        services.AddTransient<CreateCustomerStep>();
        services.AddTransient<ReserveWelcomeGiftStep>();
        services.AddTransient<SendWelcomeEmailStep>();

        services.AddSagaOrchestration(options =>
        {
            options.UseInMemoryStateStore();
            options.RegisterSagasFromAssemblyContaining<OnboardCustomerSaga>();
            options.DisableBackgroundService();
        });

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task OnboardCustomerSaga_WhenGiftSucceeds_Completes()
    {
        var repository = new InMemoryCustomerRepository();
        IServiceProvider provider = CreateServiceProvider(repository);
        OnboardCustomerSaga saga = provider.GetRequiredService<OnboardCustomerSaga>();

        var data = new OnboardCustomerData
        {
            Name = "Margaret Hamilton",
            Email = "margaret@example.com",
            SimulateGiftFailure = false
        };

        SagaResult result = await saga.StartAsync(data);

        result.IsSuccess.Should().BeTrue();
        data.CustomerId.Should().NotBeNull();
        data.WelcomeGiftCode.Should().NotBeNullOrWhiteSpace();
        data.WelcomeEmailSent.Should().BeTrue();

        var customers = await repository.GetAllAsync();
        customers.Should().ContainSingle();
    }

    [Fact]
    public async Task OnboardCustomerSaga_WhenGiftFails_Compensates()
    {
        var repository = new InMemoryCustomerRepository();
        IServiceProvider provider = CreateServiceProvider(repository);
        OnboardCustomerSaga saga = provider.GetRequiredService<OnboardCustomerSaga>();

        var data = new OnboardCustomerData
        {
            Name = "Margaret Hamilton",
            Email = "margaret@example.com",
            SimulateGiftFailure = true
        };

        SagaResult result = await saga.StartAsync(data);

        result.IsSuccess.Should().BeFalse();
        result.WasCompensated.Should().BeTrue();
        saga.Status.Should().Be(SagaStatus.Compensated);

        var customers = await repository.GetAllAsync();
        customers.Should().BeEmpty();
    }
}
