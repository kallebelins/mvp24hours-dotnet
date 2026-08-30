using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Filters;

namespace Mvp24Hours.Application.RabbitMQ.Test.Pipeline;

[Trait("Category", "Unit")]
public class ValidationFiltersCoverageTest
{
    private static Mock<IValidator<TestOrderEvent>> CreateValidator(bool isValid, string property = "Name", string message = "Name is required")
    {
        var validator = new Mock<IValidator<TestOrderEvent>>();
        ValidationResult result = isValid
            ? new ValidationResult()
            : new ValidationResult([new ValidationFailure(property, message)]);
        validator.Setup(v => v.ValidateAsync(It.IsAny<TestOrderEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return validator;
    }

    private static IServiceProvider CreateProviderWithValidator(Mock<IValidator<TestOrderEvent>>? validator)
    {
        var services = new ServiceCollection();
        if (validator != null)
        {
            services.AddSingleton(validator.Object);
        }
        return services.BuildServiceProvider();
    }

    #region [ ValidationConsumeFilter ]

    [Fact]
    public async Task ConsumeFilter_WithoutRegisteredValidator_ShouldPassThrough()
    {
        var filter = new ValidationConsumeFilter();
        IServiceProvider provider = CreateProviderWithValidator(null);
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(
            new TestOrderEvent(),
            b => b.WithServiceProvider(provider));
        bool called = false;

        await filter.ConsumeAsync(context, (_, _) =>
        {
            called = true;
            return Task.CompletedTask;
        });

        called.Should().BeTrue();
        context.Items.Should().NotContainKey("ValidationFailed");
    }

    [Fact]
    public async Task ConsumeFilter_WithValidMessage_ShouldPassThrough()
    {
        var filter = new ValidationConsumeFilter();
        IServiceProvider provider = CreateProviderWithValidator(CreateValidator(isValid: true));
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(
            new TestOrderEvent(),
            b => b.WithServiceProvider(provider));
        bool called = false;

        await filter.ConsumeAsync(context, (_, _) =>
        {
            called = true;
            return Task.CompletedTask;
        });

        called.Should().BeTrue();
    }

    [Fact]
    public async Task ConsumeFilter_WithInvalidMessageAndThrowOnFailure_ShouldThrowMessageValidationException()
    {
        var filter = new ValidationConsumeFilter(
            options: Options.Create(new ValidationFilterOptions { ThrowOnValidationFailure = true }));
        IServiceProvider provider = CreateProviderWithValidator(CreateValidator(isValid: false));
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(
            new TestOrderEvent(),
            b => b.WithServiceProvider(provider));

        Func<Task> act = () => filter.ConsumeAsync(context, (_, _) => Task.CompletedTask);

        await act.Should().ThrowAsync<MessageValidationException>();
    }

    [Fact]
    public async Task ConsumeFilter_WithInvalidMessageAndSendToDeadLetter_ShouldSkipRemainingFiltersAndSetDeadLetter()
    {
        var filter = new ValidationConsumeFilter(
            options: Options.Create(new ValidationFilterOptions { SendInvalidToDeadLetter = true }));
        IServiceProvider provider = CreateProviderWithValidator(CreateValidator(isValid: false));
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(
            new TestOrderEvent(),
            b => b.WithServiceProvider(provider));
        bool called = false;

        await filter.ConsumeAsync(context, (_, _) =>
        {
            called = true;
            return Task.CompletedTask;
        });

        called.Should().BeFalse();
        context.ShouldSendToDeadLetter.Should().BeTrue();
        context.DeadLetterReason.Should().Contain("Validation failed");
        context.ShouldSkipRemainingFilters.Should().BeTrue();
    }

    [Fact]
    public async Task ConsumeFilter_WithInvalidMessageAndSkipInvalidMessages_ShouldSkipWithoutDeadLetter()
    {
        var filter = new ValidationConsumeFilter(
            options: Options.Create(new ValidationFilterOptions
            {
                SendInvalidToDeadLetter = false,
                SkipInvalidMessages = true
            }));
        IServiceProvider provider = CreateProviderWithValidator(CreateValidator(isValid: false));
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(
            new TestOrderEvent(),
            b => b.WithServiceProvider(provider));
        bool called = false;

        await filter.ConsumeAsync(context, (_, _) =>
        {
            called = true;
            return Task.CompletedTask;
        });

        called.Should().BeFalse();
        context.ShouldSendToDeadLetter.Should().BeFalse();
        context.ShouldSkipRemainingFilters.Should().BeTrue();
    }

    [Fact]
    public async Task ConsumeFilter_WithInvalidMessageAndNoFailureActionConfigured_ShouldContinueToNext()
    {
        var filter = new ValidationConsumeFilter(
            options: Options.Create(new ValidationFilterOptions
            {
                SendInvalidToDeadLetter = false,
                SkipInvalidMessages = false,
                ThrowOnValidationFailure = false
            }));
        IServiceProvider provider = CreateProviderWithValidator(CreateValidator(isValid: false));
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(
            new TestOrderEvent(),
            b => b.WithServiceProvider(provider));
        bool called = false;

        await filter.ConsumeAsync(context, (_, _) =>
        {
            called = true;
            return Task.CompletedTask;
        });

        called.Should().BeTrue();
        context.Items["ValidationFailed"].Should().Be(true);
    }

    #endregion

    #region [ ValidationPublishFilter ]

    [Fact]
    public async Task PublishFilter_WithoutRegisteredValidator_ShouldPassThrough()
    {
        var filter = new ValidationPublishFilter();
        IServiceProvider provider = CreateProviderWithValidator(null);
        PublishFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreatePublishFilterContext(
            new TestOrderEvent(), serviceProvider: provider);
        bool called = false;

        await filter.PublishAsync(context, (_, _) =>
        {
            called = true;
            return Task.CompletedTask;
        });

        called.Should().BeTrue();
    }

    [Fact]
    public async Task PublishFilter_WithValidMessage_ShouldPassThrough()
    {
        var filter = new ValidationPublishFilter();
        IServiceProvider provider = CreateProviderWithValidator(CreateValidator(isValid: true));
        PublishFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreatePublishFilterContext(
            new TestOrderEvent(), serviceProvider: provider);
        bool called = false;

        await filter.PublishAsync(context, (_, _) =>
        {
            called = true;
            return Task.CompletedTask;
        });

        called.Should().BeTrue();
    }

    [Fact]
    public async Task PublishFilter_WithInvalidMessageAndThrowOnFailure_ShouldThrow()
    {
        var filter = new ValidationPublishFilter(
            options: Options.Create(new ValidationFilterOptions { ThrowOnValidationFailure = true }));
        IServiceProvider provider = CreateProviderWithValidator(CreateValidator(isValid: false));
        PublishFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreatePublishFilterContext(
            new TestOrderEvent(), serviceProvider: provider);

        Func<Task> act = () => filter.PublishAsync(context, (_, _) => Task.CompletedTask);

        await act.Should().ThrowAsync<MessageValidationException>();
    }

    [Fact]
    public async Task PublishFilter_WithInvalidMessageAndCancelInvalidPublish_ShouldCancelPublish()
    {
        var filter = new ValidationPublishFilter(
            options: Options.Create(new ValidationFilterOptions { CancelInvalidPublish = true }));
        IServiceProvider provider = CreateProviderWithValidator(CreateValidator(isValid: false));
        PublishFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreatePublishFilterContext(
            new TestOrderEvent(), serviceProvider: provider);
        bool called = false;

        await filter.PublishAsync(context, (_, _) =>
        {
            called = true;
            return Task.CompletedTask;
        });

        called.Should().BeFalse();
        context.ShouldCancelPublish.Should().BeTrue();
        context.CancellationReason.Should().Contain("Validation failed");
    }

    [Fact]
    public async Task PublishFilter_WithInvalidMessageAndCancelDisabled_ShouldContinueToNext()
    {
        var filter = new ValidationPublishFilter(
            options: Options.Create(new ValidationFilterOptions { CancelInvalidPublish = false }));
        IServiceProvider provider = CreateProviderWithValidator(CreateValidator(isValid: false));
        PublishFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreatePublishFilterContext(
            new TestOrderEvent(), serviceProvider: provider);
        bool called = false;

        await filter.PublishAsync(context, (_, _) =>
        {
            called = true;
            return Task.CompletedTask;
        });

        called.Should().BeTrue();
        context.ShouldCancelPublish.Should().BeFalse();
    }

    #endregion
}
