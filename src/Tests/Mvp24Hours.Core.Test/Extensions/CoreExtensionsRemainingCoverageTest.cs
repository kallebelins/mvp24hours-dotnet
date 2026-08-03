using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Infrastructure.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Domain.Specifications;
using Mvp24Hours.Core.DTOs.Models;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Core.ValueObjects.Logic;

namespace Mvp24Hours.Core.Test.Extensions;

[Trait("Category", "Unit")]
[Collection("TelemetryHelper")]
public class CoreExtensionsRemainingCoverageTest
{
#pragma warning disable CS0618
    private sealed class CaptureTelemetryService : ITelemetryService
    {
        public List<string> Events { get; } = [];

        public void Execute(string eventName, params object[] args)
        {
            Events.Add(eventName);
        }
    }
#pragma warning restore CS0618

    public CoreExtensionsRemainingCoverageTest()
    {
#pragma warning disable CS0618
        TelemetryHelper.Clear();
#pragma warning restore CS0618
    }

    [Fact]
    public void BusinessPagingExtensions_ShouldBuildPagingResults()
    {
        var request = new PagingCriteriaRequest { Limit = 20, Offset = 2, OrderBy = ["Name"], Navigation = ["Items"] };

        IPagingCriteria criteria = request.ToPagingCriteria();
        criteria.Limit.Should().Be(20);
        criteria.Offset.Should().Be(2);

        IPagingCriteriaExpression<string> expression = request.ToPagingCriteriaExpression<string>();
        expression.Limit.Should().Be(20);

        IPagingCriteria cloned = criteria.NewCriteria(limit: 5);
        cloned.Limit.Should().Be(5);

        IPagingResult<string> paging = "data".ToBusinessPaging(
            new PageResult(1, 20, 100),
            new SummaryResult(1, 1),
            [new MessageResult("ok", MessageType.Info)],
            "token-1");

        paging.Data.Should().Be("data");
        paging.Token.Should().Be("token-1");
    }

    [Fact]
    public void BusinessExtensions_ShouldWrapPipelineMessage()
    {
        var message = new StubPipelineMessage { Token = "pipe-token" };
        message.AddContent("payload", 42);

        IBusinessResult<int> result = message.ToBusiness<int>("payload");
        result.Data.Should().Be(42);
        result.Token.Should().Be("pipe-token");
    }

    [Fact]
    public void JsonExtensions_ShouldSerializeAndValidate()
    {
        var dto = new SampleDto { Id = 1, Name = "json" };

        string json = dto.ToSerialize();
        json.Should().Contain("name");

        SampleDto? roundTrip = json.ToDeserialize<SampleDto>();
        roundTrip!.Name.Should().Be("json");

        "{\"a\":1}".IsValidJson().Should().BeTrue();
        "not-json".IsValidJson().Should().BeFalse();
        ((string?)null).ToSerialize<object?>().Should().BeEmpty();
    }

    [Fact]
    public void SpecificationPagingExtensions_ShouldConvertBetweenSpecificationAndPaging()
    {
        var spec = new NameSpecification();
        IPagingCriteriaExpression<SampleEntity> paging = spec.ToPagingCriteria(limit: 15, offset: 1);
        paging.Limit.Should().Be(10);

        var fromPaging = paging.ToSpecification(e => e.Active);
        fromPaging.Should().NotBeNull();
    }

    [Fact]
    public void TelemetryExtensions_ShouldRegisterHandlersInDi()
    {
#pragma warning disable CS0618
        var services = new ServiceCollection();
        var captured = new CaptureTelemetryService();

        services.AddMvp24HoursTelemetry(TelemetryLevels.Information, name => captured.Events.Add(name));
        services.AddMvp24HoursTelemetryFiltered("Filtered", captured);
        services.AddMvp24HoursTelemetryIgnore("Ignored");

        TelemetryHelper.Execute(TelemetryLevels.Information, "Ignored");
        TelemetryHelper.Execute(TelemetryLevels.Information, "Visible");
        TelemetryHelper.Execute(TelemetryLevels.Information, "Filtered");

        captured.Events.Should().NotContain("Ignored");
        captured.Events.Should().Contain("Visible");
        captured.Events.Should().Contain("Filtered");

        TelemetryHelper.Clear();
#pragma warning restore CS0618
    }

    [Fact]
    public void JsonHelper_DeserializeWithConverters_ShouldUseCustomConverter()
    {
        string json = "{\"value\":10}";
        SampleWithConverter? result = JsonHelper.Deserialize<SampleWithConverter>(
            json,
            new Newtonsoft.Json.Converters.StringEnumConverter());

        result.Should().NotBeNull();
    }

    [Fact]
    public void JsonBusinessResultSettings_WithCustomSettings_ShouldMergeContractResolver()
    {
        var custom = new Newtonsoft.Json.JsonSerializerSettings
        {
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Include
        };

        Newtonsoft.Json.JsonSerializerSettings settings = JsonHelper.JsonBusinessResultSettings<string>(custom);

        settings.NullValueHandling.Should().Be(Newtonsoft.Json.NullValueHandling.Include);
        settings.Converters.Should().NotBeEmpty();
    }

    private sealed class StubPipelineMessage : IPipelineMessage
    {
        private readonly Dictionary<string, object?> _content = new(StringComparer.OrdinalIgnoreCase);

        public bool IsLocked { get; private set; }

        public bool IsFaulty { get; private set; }

        public dynamic DynamicContents => _content;

        public IList<IMessageResult> Messages { get; } = [];

        public string Token { get; set; } = string.Empty;

        public void AddContent<T>(T obj)
        {
            _content[typeof(T).FullName ?? typeof(T).Name] = obj!;
        }

        public void AddContent<T>(string key, T obj)
        {
            _content[key] = obj!;
        }

        public T GetContent<T>()
        {
            return _content.Values.FirstOrDefault(v => v is T typed) is T value ? value : default!;
        }

        public T GetContent<T>(string key)
        {
            return _content.TryGetValue(key, out object? stored) && stored is T typed ? typed : default!;
        }

        public bool HasContent<T>()
        {
            return _content.Values.Any(v => v is T);
        }

        public bool HasContent(string key)
        {
            return _content.ContainsKey(key);
        }

        public IList<object> GetContentAll()
        {
            return [.. _content.Values.Where(v => v != null).Cast<object>()];
        }

        public void SetLock()
        {
            IsLocked = true;
        }

        public void SetFailure()
        {
            IsFaulty = true;
        }
    }

    private sealed class SampleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class SampleWithConverter
    {
        public int Value { get; set; }
    }

    private sealed class SampleEntity
    {
        public string Name { get; set; } = string.Empty;
        public bool Active { get; set; }
    }

    private sealed class NameSpecification : Specification<SampleEntity>
    {
        protected override System.Linq.Expressions.Expression<Func<SampleEntity, bool>> Criteria =>
            entity => entity.Active;

        public NameSpecification()
        {
            AddOrderByDescending(entity => entity.Name);
            ApplyPaging(0, 10);
        }
    }
}
