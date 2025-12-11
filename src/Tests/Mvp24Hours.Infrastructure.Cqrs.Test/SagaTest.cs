//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Mvp24Hours.Infrastructure.Cqrs.Saga;
using Xunit;
using Xunit.Priority;

namespace Mvp24Hours.Infrastructure.Cqrs.Test;

[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
public class SagaTest
{
    #region Test Data

    public class OrderSagaData
    {
        public Guid OrderId { get; set; } = Guid.NewGuid();
        public List<string> Items { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public Guid? ReservationId { get; set; }
        public Guid? PaymentId { get; set; }
        public string? TrackingNumber { get; set; }
        public List<string> ExecutedSteps { get; set; } = new();
        public List<string> CompensatedSteps { get; set; } = new();
    }

    public class ReserveStockStep : SagaStepBase<OrderSagaData>
    {
        public override string Name => "ReserveStock";
        public override int Order => 1;

        public override Task ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken = default)
        {
            data.ReservationId = Guid.NewGuid();
            data.ExecutedSteps.Add(Name);
            return Task.CompletedTask;
        }

        public override Task CompensateAsync(OrderSagaData data, CancellationToken cancellationToken = default)
        {
            data.ReservationId = null;
            data.CompensatedSteps.Add(Name);
            return Task.CompletedTask;
        }
    }

    public class ProcessPaymentStep : SagaStepBase<OrderSagaData>
    {
        public override string Name => "ProcessPayment";
        public override int Order => 2;

        public override Task ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken = default)
        {
            data.PaymentId = Guid.NewGuid();
            data.ExecutedSteps.Add(Name);
            return Task.CompletedTask;
        }

        public override Task CompensateAsync(OrderSagaData data, CancellationToken cancellationToken = default)
        {
            data.PaymentId = null;
            data.CompensatedSteps.Add(Name);
            return Task.CompletedTask;
        }
    }

    public class ShipOrderStep : SagaStepBase<OrderSagaData>
    {
        public override string Name => "ShipOrder";
        public override int Order => 3;

        public override Task ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken = default)
        {
            data.TrackingNumber = $"TRK-{Guid.NewGuid():N}".Substring(0, 15);
            data.ExecutedSteps.Add(Name);
            return Task.CompletedTask;
        }

        public override Task CompensateAsync(OrderSagaData data, CancellationToken cancellationToken = default)
        {
            data.TrackingNumber = null;
            data.CompensatedSteps.Add(Name);
            return Task.CompletedTask;
        }
    }

    public class FailingStep : SagaStepBase<OrderSagaData>
    {
        public override string Name => "FailingStep";
        public override int Order => 2;

        public override Task ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Step failed intentionally");
        }
    }

    public class FailingCompensationStep : SagaStepBase<OrderSagaData>
    {
        public override string Name => "FailingCompensation";
        public override int Order => 1;

        public override Task ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken = default)
        {
            data.ExecutedSteps.Add(Name);
            return Task.CompletedTask;
        }

        public override Task CompensateAsync(OrderSagaData data, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Compensation failed intentionally");
        }
    }

    public class TestOrderSaga : SagaBase<OrderSagaData>
    {
        public TestOrderSaga(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            ConfigureSteps(steps =>
            {
                steps.Add<ReserveStockStep>();
                steps.Add<ProcessPaymentStep>();
                steps.Add<ShipOrderStep>();
            });
            
            WithTimeout(TimeSpan.FromMinutes(5));
            WithMaxRetries(3);
        }
    }

    public class FailingSaga : SagaBase<OrderSagaData>
    {
        public FailingSaga(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            ConfigureSteps(steps =>
            {
                steps.Add<ReserveStockStep>();
                steps.Add<FailingStep>();
                steps.Add<ShipOrderStep>();
            });
        }
    }

    public class PartialCompensationSaga : SagaBase<OrderSagaData>
    {
        public PartialCompensationSaga(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            ConfigureSteps(steps =>
            {
                steps.Add<FailingCompensationStep>();
                steps.Add<ProcessPaymentStep>();
                steps.Add<FailingStep>();
            });
        }
    }

    #endregion

    private static IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ISagaStateStore, InMemorySagaStateStore>();
        services.AddScoped<ISagaOrchestrator, SagaOrchestrator>();
        services.AddTransient<TestOrderSaga>();
        services.AddTransient<FailingSaga>();
        services.AddTransient<PartialCompensationSaga>();
        services.AddTransient<ReserveStockStep>();
        services.AddTransient<ProcessPaymentStep>();
        services.AddTransient<ShipOrderStep>();
        services.AddTransient<FailingStep>();
        services.AddTransient<FailingCompensationStep>();
        
        return services.BuildServiceProvider();
    }

    #region Saga Execution Tests

    [Fact, Priority(1)]
    public async Task Saga_ExecutesAllSteps_Successfully()
    {
        // Arrange
        var provider = CreateServiceProvider();
        var saga = provider.GetRequiredService<TestOrderSaga>();
        var data = new OrderSagaData
        {
            Items = new List<string> { "Item1", "Item2" },
            TotalAmount = 100.00m
        };

        // Act
        var result = await saga.StartAsync(data);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(SagaStatus.Completed, saga.Status);
        Assert.NotNull(data.ReservationId);
        Assert.NotNull(data.PaymentId);
        Assert.NotNull(data.TrackingNumber);
        Assert.Equal(3, data.ExecutedSteps.Count);
        Assert.Contains("ReserveStock", data.ExecutedSteps);
        Assert.Contains("ProcessPayment", data.ExecutedSteps);
        Assert.Contains("ShipOrder", data.ExecutedSteps);
    }

    [Fact, Priority(2)]
    public async Task Saga_CompensatesOnFailure()
    {
        // Arrange
        var provider = CreateServiceProvider();
        var saga = provider.GetRequiredService<FailingSaga>();
        var data = new OrderSagaData();

        // Act
        var result = await saga.StartAsync(data);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.WasCompensated);
        Assert.Equal(SagaStatus.Compensated, saga.Status);
        Assert.Contains("ReserveStock", data.ExecutedSteps);
        Assert.Contains("ReserveStock", data.CompensatedSteps);
        Assert.Null(data.ReservationId); // Should be null after compensation
    }

    [Fact, Priority(3)]
    public async Task Saga_PartiallyCompensates_WhenCompensationFails()
    {
        // Arrange
        var provider = CreateServiceProvider();
        var saga = provider.GetRequiredService<PartialCompensationSaga>();
        var data = new OrderSagaData();

        // Act
        var result = await saga.StartAsync(data);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(SagaStatus.PartiallyCompensated, saga.Status);
    }

    [Fact, Priority(4)]
    public async Task Saga_ThrowsOnDoubleStart()
    {
        // Arrange
        var provider = CreateServiceProvider();
        var saga = provider.GetRequiredService<TestOrderSaga>();
        var data = new OrderSagaData();
        await saga.StartAsync(data);

        // Act & Assert
        await Assert.ThrowsAsync<SagaInvalidStateException>(() => saga.StartAsync(data));
    }

    [Fact, Priority(5)]
    public async Task Saga_StepsExecuteInOrder()
    {
        // Arrange
        var provider = CreateServiceProvider();
        var saga = provider.GetRequiredService<TestOrderSaga>();
        var data = new OrderSagaData();

        // Act
        await saga.StartAsync(data);

        // Assert
        Assert.Equal("ReserveStock", data.ExecutedSteps[0]);
        Assert.Equal("ProcessPayment", data.ExecutedSteps[1]);
        Assert.Equal("ShipOrder", data.ExecutedSteps[2]);
    }

    #endregion

    #region Saga State Store Tests

    [Fact, Priority(10)]
    public async Task StateStore_SavesAndRetrievesState()
    {
        // Arrange
        var store = new InMemorySagaStateStore();
        var state = new SagaState<OrderSagaData>
        {
            SagaId = Guid.NewGuid(),
            SagaType = "TestSaga",
            Status = SagaStatus.Running,
            Data = new OrderSagaData { TotalAmount = 150m },
            StartedAt = DateTime.UtcNow,
            CurrentStepIndex = 1,
            CurrentStepName = "ProcessPayment"
        };

        // Act
        await store.SaveAsync(state);
        var retrieved = await store.GetAsync<OrderSagaData>(state.SagaId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(state.SagaId, retrieved.SagaId);
        Assert.Equal(SagaStatus.Running, retrieved.Status);
        Assert.Equal(150m, retrieved.Data.TotalAmount);
        Assert.Equal("ProcessPayment", retrieved.CurrentStepName);
    }

    [Fact, Priority(11)]
    public async Task StateStore_UpdatesState()
    {
        // Arrange
        var store = new InMemorySagaStateStore();
        var sagaId = Guid.NewGuid();
        var state = new SagaState<OrderSagaData>
        {
            SagaId = sagaId,
            SagaType = "TestSaga",
            Status = SagaStatus.Running,
            Data = new OrderSagaData(),
            StartedAt = DateTime.UtcNow
        };
        await store.SaveAsync(state);

        // Act
        await store.UpdateAsync<OrderSagaData>(sagaId, s =>
        {
            s.Status = SagaStatus.Completed;
            s.CompletedAt = DateTime.UtcNow;
        });

        // Assert
        var updated = await store.GetAsync<OrderSagaData>(sagaId);
        Assert.NotNull(updated);
        Assert.Equal(SagaStatus.Completed, updated.Status);
        Assert.NotNull(updated.CompletedAt);
    }

    [Fact, Priority(12)]
    public async Task StateStore_GetsByStatus()
    {
        // Arrange
        var store = new InMemorySagaStateStore();
        
        await store.SaveAsync(new SagaState
        {
            SagaId = Guid.NewGuid(),
            Status = SagaStatus.Running,
            DataJson = "{}",
            StartedAt = DateTime.UtcNow
        });
        await store.SaveAsync(new SagaState
        {
            SagaId = Guid.NewGuid(),
            Status = SagaStatus.Completed,
            DataJson = "{}",
            StartedAt = DateTime.UtcNow
        });
        await store.SaveAsync(new SagaState
        {
            SagaId = Guid.NewGuid(),
            Status = SagaStatus.Running,
            DataJson = "{}",
            StartedAt = DateTime.UtcNow
        });

        // Act
        var running = await store.GetByStatusAsync(SagaStatus.Running);

        // Assert
        Assert.Equal(2, running.Count);
    }

    [Fact, Priority(13)]
    public async Task StateStore_DeletesState()
    {
        // Arrange
        var store = new InMemorySagaStateStore();
        var sagaId = Guid.NewGuid();
        await store.SaveAsync(new SagaState
        {
            SagaId = sagaId,
            Status = SagaStatus.Completed,
            DataJson = "{}",
            StartedAt = DateTime.UtcNow
        });

        // Act
        await store.DeleteAsync(sagaId);
        var retrieved = await store.GetAsync(sagaId);

        // Assert
        Assert.Null(retrieved);
    }

    [Fact, Priority(14)]
    public async Task StateStore_CleanupsExpired()
    {
        // Arrange
        var store = new InMemorySagaStateStore();
        var oldDate = DateTime.UtcNow.AddDays(-10);
        
        await store.SaveAsync(new SagaState
        {
            SagaId = Guid.NewGuid(),
            Status = SagaStatus.Completed,
            DataJson = "{}",
            StartedAt = oldDate,
            CompletedAt = oldDate
        });

        // Act
        var cleaned = await store.CleanupAsync(DateTime.UtcNow.AddDays(-5));

        // Assert
        Assert.Equal(1, cleaned);
    }

    #endregion

    #region Saga Orchestrator Tests

    [Fact, Priority(20)]
    public async Task Orchestrator_ExecutesSaga()
    {
        // Arrange
        var provider = CreateServiceProvider();
        using var scope = provider.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<ISagaOrchestrator>();
        var data = new OrderSagaData { TotalAmount = 200m };

        // Act
        var result = await orchestrator.ExecuteAsync<TestOrderSaga, OrderSagaData>(data);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.ReservationId);
        Assert.NotNull(result.Data.PaymentId);
        Assert.NotNull(result.Data.TrackingNumber);
    }

    [Fact, Priority(21)]
    public async Task Orchestrator_PersistsState()
    {
        // Arrange
        var provider = CreateServiceProvider();
        using var scope = provider.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<ISagaOrchestrator>();
        var stateStore = provider.GetRequiredService<ISagaStateStore>();
        var data = new OrderSagaData { TotalAmount = 300m };

        // Act
        var result = await orchestrator.ExecuteAsync<TestOrderSaga, OrderSagaData>(data);
        var state = await stateStore.GetAsync(result.SagaId);

        // Assert
        Assert.NotNull(state);
        Assert.Equal(SagaStatus.Completed, state.Status);
    }

    [Fact, Priority(22)]
    public async Task Orchestrator_GetsStatus()
    {
        // Arrange
        var provider = CreateServiceProvider();
        using var scope = provider.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<ISagaOrchestrator>();
        var data = new OrderSagaData();
        var result = await orchestrator.ExecuteAsync<TestOrderSaga, OrderSagaData>(data);

        // Act
        var status = await orchestrator.GetStatusAsync(result.SagaId);

        // Assert
        Assert.NotNull(status);
        Assert.Equal(SagaStatus.Completed, status.Status);
    }

    [Fact, Priority(23)]
    public async Task Orchestrator_HandlesExecutionOptions()
    {
        // Arrange
        var provider = CreateServiceProvider();
        using var scope = provider.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<ISagaOrchestrator>();
        var data = new OrderSagaData();
        var options = new SagaExecutionOptions
        {
            CorrelationId = "test-correlation-123",
            Timeout = TimeSpan.FromMinutes(10),
            MaxRetries = 5,
            Metadata = new Dictionary<string, string>
            {
                { "source", "test" }
            }
        };

        // Act
        var result = await orchestrator.ExecuteAsync<TestOrderSaga, OrderSagaData>(data, options);
        var stateStore = provider.GetRequiredService<ISagaStateStore>();
        var state = await stateStore.GetAsync<OrderSagaData>(result.SagaId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(state);
        Assert.Equal("test-correlation-123", state.CorrelationId);
    }

    [Fact, Priority(24)]
    public async Task Orchestrator_HandlesFailedSaga()
    {
        // Arrange
        var provider = CreateServiceProvider();
        using var scope = provider.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<ISagaOrchestrator>();
        var data = new OrderSagaData();

        // Act
        var result = await orchestrator.ExecuteAsync<FailingSaga, OrderSagaData>(data);

        // Assert
        Assert.False(result.IsSuccess);
        // Saga should be compensated after failure (compensation happens in saga.StartAsync)
        Assert.True(result.WasCompensated || result.Status == SagaStatus.Failed);
        Assert.NotNull(result.ErrorMessage);
    }

    #endregion

    #region Saga Step Tests

    [Fact, Priority(30)]
    public async Task SagaStep_ExecutesSuccessfully()
    {
        // Arrange
        var provider = CreateServiceProvider();
        var step = provider.GetRequiredService<ReserveStockStep>();
        var data = new OrderSagaData();

        // Act
        await step.ExecuteAsync(data);

        // Assert
        Assert.NotNull(data.ReservationId);
        Assert.Contains("ReserveStock", data.ExecutedSteps);
    }

    [Fact, Priority(31)]
    public async Task SagaStep_CompensatesSuccessfully()
    {
        // Arrange
        var provider = CreateServiceProvider();
        var step = provider.GetRequiredService<ReserveStockStep>();
        var data = new OrderSagaData { ReservationId = Guid.NewGuid() };

        // Act
        await step.CompensateAsync(data);

        // Assert
        Assert.Null(data.ReservationId);
        Assert.Contains("ReserveStock", data.CompensatedSteps);
    }

    [Fact, Priority(32)]
    public void SagaStep_HasCorrectMetadata()
    {
        // Arrange
        var provider = CreateServiceProvider();
        var step = provider.GetRequiredService<ProcessPaymentStep>();

        // Assert
        Assert.Equal("ProcessPayment", step.Name);
        Assert.Equal(2, step.Order);
        Assert.True(step.CanCompensate);
    }

    #endregion

    #region Saga Result Tests

    [Fact, Priority(40)]
    public void SagaResult_Success_HasCorrectState()
    {
        // Act
        var result = SagaResult.Success(Guid.NewGuid());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(SagaStatus.Completed, result.Status);
        Assert.Null(result.ErrorMessage);
        Assert.False(result.WasCompensated);
    }

    [Fact, Priority(41)]
    public void SagaResult_Failed_HasCorrectState()
    {
        // Act
        var result = SagaResult.Failed(Guid.NewGuid(), "Test error");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(SagaStatus.Failed, result.Status);
        Assert.Equal("Test error", result.ErrorMessage);
        Assert.False(result.WasCompensated);
    }

    [Fact, Priority(42)]
    public void SagaResult_Compensated_HasCorrectState()
    {
        // Act
        var result = SagaResult.Compensated(Guid.NewGuid(), "Compensated after failure");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(SagaStatus.Compensated, result.Status);
        Assert.True(result.WasCompensated);
    }

    [Fact, Priority(43)]
    public void SagaResult_Generic_Success()
    {
        // Arrange
        var data = new OrderSagaData { TotalAmount = 100m };

        // Act
        var result = SagaResult<OrderSagaData>.Success(Guid.NewGuid(), data);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(100m, result.Data.TotalAmount);
    }

    [Fact, Priority(44)]
    public void SagaResult_ImplicitConversion()
    {
        // Arrange
        var data = new OrderSagaData();
        var typedResult = SagaResult<OrderSagaData>.Success(Guid.NewGuid(), data);

        // Act
        SagaResult result = typedResult;

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(typedResult.SagaId, result.SagaId);
    }

    #endregion

    #region Saga Exception Tests

    [Fact, Priority(50)]
    public void SagaException_HasCorrectProperties()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act
        var ex = new SagaException(sagaId, "Test error", "TEST_ERROR");

        // Assert
        Assert.Equal(sagaId, ex.SagaId);
        Assert.Equal("TEST_ERROR", ex.ErrorCode);
        Assert.Equal("Test error", ex.Message);
    }

    [Fact, Priority(51)]
    public void SagaStepException_HasStepInfo()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act
        var ex = new SagaStepException(sagaId, "ProcessPayment", 2, "Payment failed");

        // Assert
        Assert.Equal("ProcessPayment", ex.StepName);
        Assert.Equal(2, ex.StepIndex);
        Assert.Equal("SAGA_STEP_FAILED", ex.ErrorCode);
    }

    [Fact, Priority(52)]
    public void SagaNotFoundException_HasSagaId()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act
        var ex = new SagaNotFoundException(sagaId);

        // Assert
        Assert.Equal(sagaId, ex.SagaId);
        Assert.Equal("SAGA_NOT_FOUND", ex.ErrorCode);
        Assert.Contains(sagaId.ToString(), ex.Message);
    }

    [Fact, Priority(53)]
    public void SagaTimeoutException_HasTimeoutInfo()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var timeout = TimeSpan.FromMinutes(5);

        // Act
        var ex = new SagaTimeoutException(sagaId, timeout, "ProcessPayment");

        // Assert
        Assert.Equal(timeout, ex.Timeout);
        Assert.Equal("ProcessPayment", ex.StepName);
        Assert.Equal("SAGA_TIMEOUT", ex.ErrorCode);
    }

    #endregion

    #region Compensating Command Tests

    [Fact, Priority(60)]
    public void CompensatingCommand_HasRequiredProperties()
    {
        // Arrange
        var originalId = Guid.NewGuid();
        var sagaId = Guid.NewGuid();

        // Act
        var command = new TestCompensatingCommand(originalId)
        {
            Reason = "Order cancelled",
            SagaId = sagaId
        };

        // Assert
        Assert.Equal(originalId, command.OriginalCommandId);
        Assert.Equal(sagaId, command.SagaId);
        Assert.Equal("Order cancelled", command.Reason);
        Assert.True(command.CompensationInitiatedAt <= DateTime.UtcNow);
    }

    [Fact, Priority(61)]
    public void CompensationResult_Success()
    {
        // Act
        var result = CompensationResult.Success();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorMessage);
    }

    [Fact, Priority(62)]
    public void CompensationResult_Failed()
    {
        // Act
        var exception = new InvalidOperationException("Test");
        var result = CompensationResult.Failed("Compensation failed", exception);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Compensation failed", result.ErrorMessage);
        Assert.Equal(exception, result.Exception);
    }

    private record TestCompensatingCommand(Guid OriginalCommandId) : CompensatingCommand(OriginalCommandId);

    #endregion

    #region Saga DI Extension Tests

    [Fact, Priority(70)]
    public void SagaExtensions_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        
        // Act
        services.AddSagaOrchestration(options =>
        {
            options.UseInMemoryStateStore();
            options.DisableBackgroundService();
        });
        
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<ISagaStateStore>());
        Assert.NotNull(provider.GetService<ISagaOrchestrator>());
    }

    [Fact, Priority(71)]
    public void SagaExtensions_RegistersSagasFromAssembly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        
        // Act
        services.AddSagaOrchestration(options =>
        {
            options.RegisterSagasFromAssemblyContaining<SagaTest>();
            options.DisableBackgroundService();
        });
        
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<TestOrderSaga>());
    }

    #endregion
}

