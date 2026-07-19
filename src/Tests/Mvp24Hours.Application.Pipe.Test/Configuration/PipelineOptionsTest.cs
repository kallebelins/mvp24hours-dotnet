//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Configuration;
using Xunit.Priority;

namespace Mvp24Hours.Application.Pipe.Test.Configuration;

[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
[Trait("Category", "Unit")]
public class PipelineOptionsTest
{
    [Fact, Priority(1)]
    public void PipelineOptions_DefaultValues_ShouldBeCorrect()
    {
        var options = new PipelineOptions();

        options.IsBreakOnFail.Should().BeFalse();
        options.ForceRollbackOnFalure.Should().BeFalse();
        options.AllowPropagateException.Should().BeFalse();
        options.DefaultOperationTimeout.Should().BeNull();
        options.ValidateBeforeExecute.Should().BeFalse();
        options.MaxOperations.Should().Be(1000);
        options.UseMiddleware.Should().BeFalse();
        options.ExceptionMapper.Should().BeNull();
        options.Validator.Should().BeNull();
    }

    [Fact, Priority(2)]
    public void PipelineOptions_SetValues_ShouldPersist()
    {
        var options = new PipelineOptions
        {
            IsBreakOnFail = true,
            ForceRollbackOnFalure = true,
            AllowPropagateException = true,
            DefaultOperationTimeout = TimeSpan.FromSeconds(30),
            ValidateBeforeExecute = true,
            MaxOperations = 500,
            UseMiddleware = true
        };

        options.IsBreakOnFail.Should().BeTrue();
        options.ForceRollbackOnFalure.Should().BeTrue();
        options.AllowPropagateException.Should().BeTrue();
        options.DefaultOperationTimeout.Should().Be(TimeSpan.FromSeconds(30));
        options.ValidateBeforeExecute.Should().BeTrue();
        options.MaxOperations.Should().Be(500);
        options.UseMiddleware.Should().BeTrue();
    }

    [Fact, Priority(3)]
    public void AddMvp24HoursPipeline_Default_ShouldRegisterIPipeline()
    {
        var services = new ServiceCollection();
        services.AddMvp24HoursPipeline();

        ServiceProvider sp = services.BuildServiceProvider();
        IPipeline? pipeline = sp.GetService<IPipeline>();

        pipeline.Should().NotBeNull();
        pipeline.Should().BeOfType<Pipeline>();
    }

    [Fact, Priority(4)]
    public void AddMvp24HoursPipeline_WithOptions_ShouldConfigureOptions()
    {
        var services = new ServiceCollection();
        services.AddMvp24HoursPipeline(opt =>
        {
            opt.IsBreakOnFail = true;
            opt.ForceRollbackOnFalure = true;
        });

        ServiceProvider sp = services.BuildServiceProvider();
        PipelineOptions? options = sp.GetService<IOptions<PipelineOptions>>()?.Value;

        options.Should().NotBeNull();
        options!.IsBreakOnFail.Should().BeTrue();
        options.ForceRollbackOnFalure.Should().BeTrue();
    }

    [Fact, Priority(5)]
    public void AddMvp24HoursPipeline_WithFactory_ShouldUseCustomFactory()
    {
        bool factoryCalled = false;
        var services = new ServiceCollection();
        services.AddMvp24HoursPipeline(factory: sp =>
        {
            factoryCalled = true;
            return new Pipeline(sp);
        });

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        _ = serviceProvider.GetRequiredService<IPipeline>();

        factoryCalled.Should().BeTrue();
    }

    [Fact, Priority(6)]
    public void AddMvp24HoursPipelineAsync_Default_ShouldRegisterIPipelineAsync()
    {
        var services = new ServiceCollection();
        services.AddMvp24HoursPipelineAsync();

        ServiceProvider sp = services.BuildServiceProvider();
        IPipelineAsync? pipeline = sp.GetService<IPipelineAsync>();

        pipeline.Should().NotBeNull();
        pipeline.Should().BeOfType<PipelineAsync>();
    }

    [Fact, Priority(7)]
    public void AddMvp24HoursPipeline_Singleton_ShouldBeSameInstance()
    {
        var services = new ServiceCollection();
        services.AddMvp24HoursPipeline(lifetime: ServiceLifetime.Singleton);

        ServiceProvider sp = services.BuildServiceProvider();
        IPipeline p1 = sp.GetRequiredService<IPipeline>();
        IPipeline p2 = sp.GetRequiredService<IPipeline>();

        p1.Should().BeSameAs(p2);
    }

    [Fact, Priority(8)]
    public void AddMvp24HoursPipeline_Scoped_ShouldBeSameInSameScope()
    {
        var services = new ServiceCollection();
        services.AddMvp24HoursPipeline();

        ServiceProvider sp = services.BuildServiceProvider();
        using IServiceScope scope = sp.CreateScope();

        IPipeline p1 = scope.ServiceProvider.GetRequiredService<IPipeline>();
        IPipeline p2 = scope.ServiceProvider.GetRequiredService<IPipeline>();

        p1.Should().BeSameAs(p2);
    }

    [Fact, Priority(9)]
    public void AddPipelineExceptionMapper_ShouldRegisterMapper()
    {
        var services = new ServiceCollection();
        services.AddPipelineExceptionMapper();

        ServiceProvider sp = services.BuildServiceProvider();
        IPipelineExceptionMapper? mapper = sp.GetService<IPipelineExceptionMapper>();

        mapper.Should().NotBeNull();
    }

    [Fact, Priority(10)]
    public void AddPipelineValidator_ShouldRegisterValidator()
    {
        var services = new ServiceCollection();
        services.AddPipelineValidator();

        ServiceProvider sp = services.BuildServiceProvider();
        IPipelineValidator? validator = sp.GetService<IPipelineValidator>();

        validator.Should().NotBeNull();
    }
}
