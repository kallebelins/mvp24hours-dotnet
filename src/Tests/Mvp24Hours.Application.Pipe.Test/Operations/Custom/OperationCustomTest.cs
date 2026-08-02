//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Operations.Custom;
using Xunit.Priority;

namespace Mvp24Hours.Application.Pipe.Test.Operations.Custom;

[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
[Trait("Category", "Unit")]
public class OperationCustomTest
{
    // ─── OperationMapper<T> ───────────────────────────────────────────────────

    [Fact, Priority(1)]
    public void OperationMapper_SingleType_ShouldAddContentByType()
    {
        var pipeline = new Pipeline();
        pipeline.Add(new SingleTypeMapper());
        pipeline.Execute();

        pipeline.GetMessage().GetContent<MappedDto>().Should().NotBeNull();
        pipeline.GetMessage().GetContent<MappedDto>().Value.Should().Be("mapped");
    }

    [Fact, Priority(2)]
    public void OperationMapper_SingleType_WithContentKey_ShouldAddContentByKey()
    {
        var pipeline = new Pipeline();
        pipeline.Add(new SingleTypeMapperWithKey());
        pipeline.Execute();

        pipeline.GetMessage().GetContent<MappedDto>("custom-key").Should().NotBeNull();
        pipeline.GetMessage().GetContent<MappedDto>("custom-key").Value.Should().Be("keyed");
    }

    [Fact, Priority(3)]
    public void OperationMapper_DualType_ShouldMapInputToOutput()
    {
        IPipelineMessage input = new PipelineMessage();
        input.AddContent("source-value");

        var pipeline = new Pipeline();
        pipeline.Add(new DualTypeMapper());
        pipeline.Execute(input);

        pipeline.GetMessage().GetContent<int>().Should().Be(12);
    }

    [Fact, Priority(4)]
    public void OperationMapper_DualType_WithSourceKey_ShouldReadFromKey()
    {
        IPipelineMessage input = new PipelineMessage();
        input.AddContent("src-key", "hello world");

        var pipeline = new Pipeline();
        pipeline.Add(new DualTypeMapperWithSourceKey());
        pipeline.Execute(input);

        pipeline.GetMessage().GetContent<int>().Should().Be(11);
    }

    // ─── OperationConditional ────────────────────────────────────────────────

    [Fact, Priority(5)]
    public void OperationConditional_TrueCondition_ShouldExecuteTrueResult()
    {
        IPipelineMessage input = new PipelineMessage();
        input.AddContent("active", true);

        var pipeline = new Pipeline();
        pipeline.Add(new ConditionalChecker());
        pipeline.Execute(input);

        pipeline.GetMessage().GetContent<string>("branch").Should().Be("true");
    }

    [Fact, Priority(6)]
    public void OperationConditional_FalseCondition_ShouldExecuteFalseResult()
    {
        var pipeline = new Pipeline();
        pipeline.Add(new ConditionalChecker());
        pipeline.Execute();

        pipeline.GetMessage().GetContent<string>("branch").Should().Be("false");
    }

    // ─── OperationValidator ──────────────────────────────────────────────────

    [Fact, Priority(7)]
    public void OperationValidator_Valid_ShouldNotLockMessage()
    {
        IPipelineMessage input = new PipelineMessage();
        input.AddContent("value", 10);

        var pipeline = new Pipeline();
        pipeline.Add(new PositiveNumberValidator());
        pipeline.Execute(input);

        pipeline.GetMessage().IsLocked.Should().BeFalse();
    }

    [Fact, Priority(8)]
    public void OperationValidator_Invalid_ShouldLockMessage()
    {
        IPipelineMessage input = new PipelineMessage();
        input.AddContent("value", -5);

        var pipeline = new Pipeline();
        pipeline.Add(new PositiveNumberValidator());
        pipeline.Execute(input);

        pipeline.GetMessage().IsLocked.Should().BeTrue();
    }

    [Fact, Priority(9)]
    public void OperationValidator_LockPreventsSubsequentOperations()
    {
        IPipelineMessage input = new PipelineMessage();
        input.AddContent("value", -1);

        bool subsequentRan = false;
        var pipeline = new Pipeline();
        pipeline.Add(new PositiveNumberValidator());
        pipeline.Add(_ => subsequentRan = true);
        pipeline.Execute(input);

        subsequentRan.Should().BeFalse();
    }

    // ─── OperationMediator ───────────────────────────────────────────────────

    [Fact, Priority(10)]
    public void OperationMediator_ShouldMapRequestCallMediatorAndMapResponse()
    {
        IPipelineMessage input = new PipelineMessage();
        input.AddContent("input-text", "hello");

        var pipeline = new Pipeline();
        pipeline.Add(new StringLengthMediator());
        pipeline.Execute(input);

        pipeline.GetMessage().GetContent<ResponseDto>().Should().NotBeNull();
        pipeline.GetMessage().GetContent<ResponseDto>().Length.Should().Be(5);
    }

    [Fact, Priority(11)]
    public void OperationMediator_WithResponseKey_ShouldStoreUnderKey()
    {
        IPipelineMessage input = new PipelineMessage();
        input.AddContent("input-text", "hi");

        var pipeline = new Pipeline();
        pipeline.Add(new KeyedMediator());
        pipeline.Execute(input);

        pipeline.GetMessage().GetContent<ResponseDto>("response-key").Should().NotBeNull();
        pipeline.GetMessage().GetContent<ResponseDto>("response-key").Length.Should().Be(2);
    }

    // ─── Async custom operations ─────────────────────────────────────────────

    [Fact, Priority(12)]
    public async Task OperationConditionalAsync_TrueCondition_ShouldExecuteTrueBranch()
    {
        IPipelineMessage input = new PipelineMessage();
        input.AddContent("active", true);

        var pipeline = new PipelineAsync();
        pipeline.Add(new ConditionalCheckerAsync());
        await pipeline.ExecuteAsync(input);

        pipeline.GetMessage().GetContent<string>("branch").Should().Be("true-async");
    }

    [Fact, Priority(13)]
    public async Task OperationValidatorAsync_Invalid_ShouldLockMessage()
    {
        IPipelineMessage input = new PipelineMessage();
        input.AddContent("value", -3);

        var pipeline = new PipelineAsync();
        pipeline.Add(new AsyncPositiveValidator());
        await pipeline.ExecuteAsync(input);

        pipeline.GetMessage().IsLocked.Should().BeTrue();
    }

    [Fact, Priority(14)]
    public async Task OperationMapperAsync_ShouldMapAndStore()
    {
        IPipelineMessage input = new PipelineMessage();
        input.AddContent("world"); // adds with key "System.String" so mapper can find it by type

        var pipeline = new PipelineAsync();
        pipeline.Add(new AsyncMapper());
        await pipeline.ExecuteAsync(input);

        pipeline.GetMessage().GetContent<int>().Should().Be(5);
    }

    // ─── Test helpers ────────────────────────────────────────────────────────

    private record MappedDto(string Value);
    private record ResponseDto(int Length);

    private class SingleTypeMapper : OperationMapper<MappedDto>
    {
        public override MappedDto Mapper(IPipelineMessage input)
        {
            return new("mapped");
        }
    }

    private class SingleTypeMapperWithKey : OperationMapper<MappedDto>
    {
        public override string? ContentKey => "custom-key";
        public override MappedDto Mapper(IPipelineMessage input)
        {
            return new("keyed");
        }
    }

    private class DualTypeMapper : OperationMapper<string, int>
    {
        public override int Mapper(string content)
        {
            return content.Length;
        }
    }

    private class DualTypeMapperWithSourceKey : OperationMapper<string, int>
    {
        public override string? SourceKey => "src-key";
        public override int Mapper(string content)
        {
            return content.Length;
        }
    }

    private class ConditionalChecker : OperationConditional
    {
        public override bool Condition(IPipelineMessage input)
        {
            return input.HasContent("active");
        }

        public override void TrueResult(IPipelineMessage input)
        {
            input.AddContent("branch", "true");
        }

        public override void FalseResult(IPipelineMessage input)
        {
            input.AddContent("branch", "false");
        }
    }

    private class PositiveNumberValidator : OperationValidator
    {
        public override bool IsValid(IPipelineMessage input)
        {
            int v = input.GetContent<int>("value");
            return v > 0;
        }
    }

    private class StringLengthMediator : OperationMediator<string, ResponseDto>
    {
        public override string MapperRequest(IPipelineMessage input)
        {
            return input.GetContent<string>("input-text");
        }

        public override void Mediator(IPipelineMessage input) { }
        public override ResponseDto MapperResponse(IPipelineMessage input)
        {
            return new(ModelRequest!.Length);
        }
    }

    private class KeyedMediator : OperationMediator<string, ResponseDto>
    {
        public override string? ResponseContentKey => "response-key";
        public override string MapperRequest(IPipelineMessage input)
        {
            return input.GetContent<string>("input-text");
        }

        public override void Mediator(IPipelineMessage input) { }
        public override ResponseDto MapperResponse(IPipelineMessage input)
        {
            return new(ModelRequest!.Length);
        }
    }

    // Async versions

    private class ConditionalCheckerAsync : Mvp24Hours.Infrastructure.Pipe.Operations.Custom.OperationConditionalAsync
    {
        public override Task<bool> ConditionAsync(IPipelineMessage input)
        {
            return Task.FromResult(input.HasContent("active"));
        }

        public override Task TrueResultAsync(IPipelineMessage input)
        {
            input.AddContent("branch", "true-async");
            return Task.CompletedTask;
        }
    }

    private class AsyncPositiveValidator : Mvp24Hours.Infrastructure.Pipe.Operations.Custom.OperationValidatorAsync
    {
        public override Task<bool> IsValid(IPipelineMessage input)
        {
            return Task.FromResult(input.GetContent<int>("value") > 0);
        }
    }

    private class AsyncMapper : Mvp24Hours.Infrastructure.Pipe.Operations.Custom.OperationMapperAsync<string, int>
    {
        public override Task<int> MapperAsync(string content)
        {
            return Task.FromResult(content.Length);
        }
    }
}
