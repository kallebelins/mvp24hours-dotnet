using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.DTOs;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.ValueObjects.Logic;

namespace Mvp24Hours.Core.Test.Extensions;

[Trait("Category", "Unit")]
public class BusinessAsyncExtensionsTest
{
    [Fact]
    public async Task ToBusinessAsync_FromPipelineMessage_ShouldMapDataAndMessages()
    {
        var message = new FakePipelineMessage("tok-1");
        message.AddContent("payload", 99);
        message.Messages.Add(new MessageResult("warn", MessageType.Warning));

        IBusinessResult<int> result = await Task.FromResult<IPipelineMessage>(message)
            .ToBusinessAsync<int>("payload");

        result.Data.Should().Be(99);
        result.Token.Should().Be("tok-1");
        result.Messages.Should().ContainSingle(m => m.Type == MessageType.Warning);
    }

    [Fact]
    public async Task ToBusinessAsync_FromNullPipelineMessage_ShouldReturnEmptyResult()
    {
        IBusinessResult<int> result = await Task.FromResult<IPipelineMessage>(null!)
            .ToBusinessAsync<int>(tokenDefault: "fallback");

        result.Data.Should().Be(default);
        result.Token.Should().Be("fallback");
    }

    [Fact]
    public async Task ToBusinessAsync_FromNullMessageResult_ShouldReturnEmptyResult()
    {
        IBusinessResult<string> result = await Task.FromResult<IMessageResult>(null!)
            .ToBusinessAsync<string>(tokenDefault: "t1");

        result.Token.Should().Be("t1");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task ToBusinessAsync_FromNullMessageResultList_ShouldReturnEmptyResult()
    {
        IBusinessResult<string> result = await Task.FromResult<IList<IMessageResult>>(null!)
            .ToBusinessAsync<string>(tokenDefault: "t1");

        result.Token.Should().Be("t1");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task ToBusinessAsync_FromValueWithMessage_ShouldIncludeMessage()
    {
        var msg = new MessageResult("done", MessageType.Info);

        IBusinessResult<int> result = await Task.FromResult(42)
            .ToBusinessAsync<int>((IMessageResult?)msg, tokenDefault: "tok");

        result.Data.Should().Be(42);
        result.Messages.Should().ContainSingle();
        result.Token.Should().Be("tok");
    }

    [Fact]
    public async Task ToBusinessAsync_FromValueWithDefaultMessage_ShouldUseDefaultWhenNoMessage()
    {
        var defaultMsg = new MessageResult("default", MessageType.Info);

        IBusinessResult<int> result = await Task.FromResult(7)
            .ToBusinessAsync<int>(messageResult: (IMessageResult?)null, defaultMessage: defaultMsg);

        result.Messages.Should().ContainSingle(m => m.Message == "default");
    }

    [Fact]
    public async Task ToBusinessAsync_FromNullReferenceValue_ShouldReturnEmptyResult()
    {
        IBusinessResult<string> result = await Task.FromResult<string>(null!)
            .ToBusinessAsync<string>(tokenDefault: "empty");

        result.Data.Should().BeNull();
        result.Token.Should().Be("empty");
    }

    [Fact]
    public async Task ToBusinessAsync_FromTask_ShouldReturnVoidResult()
    {
        var msg = new MessageResult("ok", MessageType.Info);

        IBusinessResult<VoidResult> result = await Task.CompletedTask.ToBusinessAsync(msg, tokenDefault: "void-tok");

        result.Messages.Should().ContainSingle();
        result.Token.Should().Be("void-tok");
    }

    [Fact]
    public async Task HasDataAsync_WithScalarData_ShouldReturnTrue()
    {
        IBusinessResult<int> business = new BusinessResult<int>(data: 10);
        bool hasData = await Task.FromResult(business).HasDataAsync<int>();

        hasData.Should().BeTrue();
    }

    [Fact]
    public async Task HasDataAsync_WithEmptyList_ShouldReturnFalse()
    {
        IBusinessResult<List<object>> business = new BusinessResult<List<object>>(data: []);
        bool hasData = await Task.FromResult(business).HasDataAsync<List<object>>();

        hasData.Should().BeFalse();
    }

    [Fact]
    public async Task GetDataValueAsync_ShouldReturnDataWhenPresent()
    {
        IBusinessResult<string> business = new BusinessResult<string>(data: "hello");
        string? value = await Task.FromResult(business).GetDataValueAsync<string>();

        value.Should().Be("hello");
    }

    [Fact]
    public async Task GetDataCountAsync_WithList_ShouldReturnCount()
    {
        IBusinessResult<List<object>> business = new BusinessResult<List<object>>(data: [1, 2, 3]);
        int count = await Task.FromResult(business).GetDataCountAsync<List<object>>();

        count.Should().Be(3);
    }

    [Fact]
    public async Task GetDataCountAsync_WithScalar_ShouldReturnOne()
    {
        IBusinessResult<int> business = new BusinessResult<int>(data: 5);
        int count = await Task.FromResult(business).GetDataCountAsync<int>();

        count.Should().Be(1);
    }

    [Fact]
    public async Task HasDataCountAsync_WithMatchingListCount_ShouldReturnTrue()
    {
        IBusinessResult<List<object>> business = new BusinessResult<List<object>>(data: [1, 2]);
        bool match = await Task.FromResult(business).HasDataCountAsync<List<object>>(2);

        match.Should().BeTrue();
    }

    [Fact]
    public async Task GetDataFirstOrDefaultAsync_WithList_ShouldReturnFirstItem()
    {
        IBusinessResult<List<object>> business = new BusinessResult<List<object>>(data: ["a", "b"]);
        object? first = await Task.FromResult(business).GetDataFirstOrDefaultAsync<List<object>>();

        first.Should().Be("a");
    }

    [Fact]
    public async Task GetDataFirstOrDefaultAsync_WithScalar_ShouldReturnData()
    {
        IBusinessResult<int> business = new BusinessResult<int>(data: 42);
        object? first = await Task.FromResult(business).GetDataFirstOrDefaultAsync<int>();

        first.Should().Be(42);
    }

    private sealed class FakePipelineMessage(string token) : IPipelineMessage
    {
        private readonly Dictionary<string, object> _contents = [];

        public bool IsFaulty { get; private set; }
        public IList<IMessageResult> Messages { get; } = [];
        public string Token { get; } = token;
        public bool IsLocked { get; private set; }
        [Obsolete("Use GetContent<T>()/AddContent<T>() for type-safe access. Will be removed in v12.")]
        public dynamic DynamicContents => throw new NotSupportedException();

        public void AddContent<T>(T obj)
        {
            AddContent(typeof(T).FullName!, obj!);
        }

        public void AddContent<T>(string key, T obj)
        {
            _contents[key] = obj!;
        }

        public T GetContent<T>()
        {
            return GetContent<T>(typeof(T).FullName!);
        }

        public T GetContent<T>(string key)
        {
            return _contents.TryGetValue(key, out object? value) ? (T)value : default!;
        }

        public bool HasContent<T>()
        {
            return _contents.ContainsKey(typeof(T).FullName!);
        }

        public bool HasContent(string key)
        {
            return _contents.ContainsKey(key);
        }

        public IList<object> GetContentAll()
        {
            return [.. _contents.Values];
        }

        public void SetFailure()
        {
            IsFaulty = true;
        }

        public void SetLock()
        {
            IsLocked = true;
        }
    }
}
