//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using FluentValidation;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Infrastructure.Cqrs.Test.Support;
using ValidationException = Mvp24Hours.Core.Exceptions.ValidationException;

namespace Mvp24Hours.Infrastructure.Cqrs.Test.Validators;

/// <summary>
/// Phase 24.3 — ValidationBehavior depth (multi-validator, error mapping, logging).
/// Maps planned Validators/* to FluentValidation + ValidationBehavior.
/// </summary>
[Trait("Category", "Unit")]
public class ValidationBehaviorTest
{
    [Fact]
    public async Task Handle_WithNoValidators_ShouldPassThrough()
    {
        // Arrange
        var behavior = new ValidationBehavior<CreateUserCommand, int>([]);
        var called = false;

        // Act
        int result = await behavior.Handle(
            new CreateUserCommand { Name = "x" },
            () => { called = true; return Task.FromResult(42); },
            CancellationToken.None);

        // Assert
        Assert.True(called);
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldCallNext()
    {
        // Arrange
        var behavior = new ValidationBehavior<CreateUserCommand, int>([new CreateUserCommandValidator()]);

        // Act
        int result = await behavior.Handle(
            new CreateUserCommand { Name = "John", Email = "john@example.com", Age = 30 },
            () => Task.FromResult(7),
            CancellationToken.None);

        // Assert
        Assert.Equal(7, result);
    }

    [Fact]
    public async Task Handle_WithInvalidRequest_ShouldThrowValidationExceptionWithCode()
    {
        // Arrange
        var behavior = new ValidationBehavior<CreateUserCommand, int>([new CreateUserCommandValidator()]);

        // Act
        ValidationException ex = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(
                new CreateUserCommand { Name = "", Email = "bad", Age = -1 },
                () => Task.FromResult(1),
                CancellationToken.None));

        // Assert
        Assert.Equal("VALIDATION_ERROR", ex.ErrorCode);
        Assert.Contains("CreateUserCommand", ex.Message);
        Assert.NotNull(ex.ValidationErrors);
        Assert.NotEmpty(ex.ValidationErrors);
    }

    [Fact]
    public async Task Handle_WithInvalidRequest_ShouldNotCallNext()
    {
        // Arrange
        var behavior = new ValidationBehavior<CreateUserCommand, int>([new CreateUserCommandValidator()]);
        var called = false;

        // Act
        await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(
                new CreateUserCommand(),
                () => { called = true; return Task.FromResult(1); },
                CancellationToken.None));

        // Assert
        Assert.False(called);
    }

    [Fact]
    public async Task Handle_ShouldMapFailuresToMessageResult()
    {
        // Arrange
        var behavior = new ValidationBehavior<CreateUserCommand, int>([new CreateUserCommandValidator()]);

        // Act
        ValidationException ex = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(
                new CreateUserCommand { Name = "", Email = "john@example.com", Age = 25 },
                () => Task.FromResult(1),
                CancellationToken.None));

        // Assert
        Assert.Contains(ex.ValidationErrors!, e =>
            e.Key == "Name" && e.Type == MessageType.Error && e.Message.Contains("required"));
    }

    [Fact]
    public async Task Handle_WithMultipleValidators_ShouldAggregateFailures()
    {
        // Arrange
        var behavior = new ValidationBehavior<CreateUserCommand, int>(
        [
            new CreateUserCommandValidator(),
            new CreateUserMustBeAdultValidator()
        ]);

        // Act
        ValidationException ex = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(
                new CreateUserCommand { Name = "Teen", Email = "teen@example.com", Age = 16 },
                () => Task.FromResult(1),
                CancellationToken.None));

        // Assert — age fails GreaterThan(0) passes but adult validator fails
        Assert.Contains(ex.ValidationErrors!, e => e.Message.Contains("at least 18"));
    }

    [Fact]
    public async Task Handle_WithNullPropertyName_ShouldFallbackToErrorCode()
    {
        // Arrange
        var behavior = new ValidationBehavior<CreateUserCommand, int>([new CreateUserNullPropertyValidator()]);

        // Act
        ValidationException ex = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(
                new CreateUserCommand { Name = "A", Email = "a@b.com", Age = 20 },
                () => Task.FromResult(1),
                CancellationToken.None));

        // Assert
        Assert.Contains(ex.ValidationErrors!, e => e.Key == "CUSTOM_CODE" && e.Message == "Custom error");
    }

    [Fact]
    public async Task Handle_OnFailure_ShouldLogWarning()
    {
        // Arrange
        var logger = new CollectingLogger<ValidationBehavior<CreateUserCommand, int>>();
        var behavior = new ValidationBehavior<CreateUserCommand, int>([new CreateUserCommandValidator()], logger);

        // Act
        await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(
                new CreateUserCommand(),
                () => Task.FromResult(1),
                CancellationToken.None));

        // Assert
        Assert.Contains(logger.Entries, e =>
            e.Level == LogLevel.Warning && e.Message.Contains("failed validation"));
    }

    [Fact]
    public async Task Handle_WithNullValidatorsEnumerable_ShouldPassThrough()
    {
        // Arrange — constructor treats null as empty
        var behavior = new ValidationBehavior<CreateUserCommand, int>(null!);

        // Act
        int result = await behavior.Handle(
            new CreateUserCommand(),
            () => Task.FromResult(99),
            CancellationToken.None);

        // Assert
        Assert.Equal(99, result);
    }

    [Fact]
    public async Task Handle_ViaMediator_ShouldThrowMappedValidationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(CreateUserCommand).Assembly);
            options.RegisterValidationBehavior = true;
        });
        services.AddTransient<IValidator<CreateUserCommand>, CreateUserCommandValidator>();
        IMediator mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // Act
        ValidationException ex = await Assert.ThrowsAsync<ValidationException>(() =>
            mediator.SendAsync(new CreateUserCommand { Name = "", Email = "x", Age = 0 }));

        // Assert
        Assert.Equal("VALIDATION_ERROR", ex.ErrorCode);
        Assert.NotEmpty(ex.ValidationErrors!);
    }

    [Fact]
    public async Task Handle_ViaMediator_WithValidRequest_ShouldReturnHandlerResult()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(CreateUserCommand).Assembly);
            options.RegisterValidationBehavior = true;
        });
        services.AddTransient<IValidator<CreateUserCommand>, CreateUserCommandValidator>();
        IMediator mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // Act
        int id = await mediator.SendAsync(new CreateUserCommand
        {
            Name = "Valid",
            Email = "valid@example.com",
            Age = 40
        });

        // Assert
        Assert.True(id > 0);
    }

    [Fact]
    public async Task Handle_WithTwoValidatorsBothFailing_ShouldIncludeErrorsFromBoth()
    {
        // Arrange
        var behavior = new ValidationBehavior<CreateUserCommand, int>(
        [
            new CreateUserCommandValidator(),
            new CreateUserNullPropertyValidator()
        ]);

        // Act
        ValidationException ex = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(
                new CreateUserCommand(),
                () => Task.FromResult(1),
                CancellationToken.None));

        // Assert
        Assert.True(ex.ValidationErrors!.Count >= 2);
        Assert.Contains(ex.ValidationErrors!, e => e.Key == "CUSTOM_CODE");
        Assert.Contains(ex.ValidationErrors!, e => e.Key == "Name");
    }
}
