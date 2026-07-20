using FluentValidation;
using Mvp24Hours.Application.Extensions;
using Mvp24Hours.Application.Logic.Validation;
using Mvp24Hours.Application.Test.Support;

namespace Mvp24Hours.Application.Test.Extensions;

[Trait("Category", "Unit")]
public class ValidationServiceCollectionExtensionsTest
{
    [Fact]
    public void AddValidationService_ShouldRegisterValidationService()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<AppTestEntity>, AppTestEntityValidator>();

        services.AddValidationService<AppTestEntity>();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IValidationService<AppTestEntity>>().Should().NotBeNull();
        provider.GetRequiredService<ICascadeValidator<AppTestEntity>>().Should().NotBeNull();
    }

    [Fact]
    public void AddValidationServices_ShouldRegisterForValidatorTypes()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<AppTestEntity>, AppTestEntityValidator>();

        services.AddValidationServices([typeof(AppTestEntityValidator).Assembly]);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IValidationService<AppTestEntity>>().Should().NotBeNull();
    }

    [Fact]
    public void AddValidationServicesFromAssemblyContaining_ShouldRegisterServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<AppTestEntityDto>, AppTestEntityDtoValidator>();

        services.AddValidationServicesFromAssemblyContaining<AppTestEntityDtoValidator>();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IValidationService<AppTestEntityDto>>().Should().NotBeNull();
    }

    [Fact]
    public void AddValidationPipeline_ShouldRegisterPipeline()
    {
        var services = new ServiceCollection();

        services.AddValidationPipeline<AppTestEntity>(builder => builder.AddStep(new NullCheckValidationStep<AppTestEntity>()));
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IValidationPipeline<AppTestEntity>>().Should().NotBeNull();
    }

    [Fact]
    public void AddDefaultValidationPipeline_ShouldRegisterPipeline()
    {
        var services = new ServiceCollection();

        services.AddDefaultValidationPipeline<AppTestEntity>();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IValidationPipeline<AppTestEntity>>().Should().NotBeNull();
    }

    [Fact]
    public void AddValidationSteps_ShouldRegisterAllStepTypes()
    {
        var services = new ServiceCollection();

        services.AddValidationSteps<AppTestEntity>();

        services.Should().Contain(d => d.ServiceType == typeof(NullCheckValidationStep<AppTestEntity>));
        services.Should().Contain(d => d.ServiceType == typeof(FluentValidationStep<AppTestEntity>));
        services.Should().Contain(d => d.ServiceType == typeof(DataAnnotationValidationStep<AppTestEntity>));
        services.Should().Contain(d => d.ServiceType == typeof(CascadeValidationStep<AppTestEntity>));
    }

    [Fact]
    public void AddMvp24HoursValidation_ShouldRegisterValidatorsAndServices()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursValidation([typeof(AppTestEntityValidator).Assembly]);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IValidator<AppTestEntity>>().Should().NotBeNull();
        provider.GetRequiredService<IValidationService<AppTestEntity>>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursValidationFromAssemblyContaining_ShouldRegisterServices()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursValidationFromAssemblyContaining<AppTestEntityValidator>();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IValidationService<AppTestEntity>>().Should().NotBeNull();
    }
}
