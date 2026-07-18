//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;

namespace Mvp24Hours.Infrastructure.Pipe;

/// <summary>
/// <see cref="Mvp24Hours.Core.Contract.Infrastructure.Pipe.IPipelineMessage"/>
/// </summary>
public class PipelineMessage : IPipelineMessage
{
    #region [ Ctor ]

    public PipelineMessage()
        : this(Guid.NewGuid().ToString())
    {
    }

    public PipelineMessage(params object[]? args)
        : this(Guid.NewGuid().ToString(), args)
    {
    }

    public PipelineMessage(string? token)
        : this(token, null)
    {
    }

    public PipelineMessage(string? token, params object[]? args)
    {
        _contents = [];
        Token = token ?? Guid.NewGuid().ToString();
        DynamicContents = new DynamicContents(this);

        if (args?.Length > 0)
        {
            foreach (object item in args)
            {
                AddContent(item);
            }
        }
    }

    #endregion

    #region [ Fields ]
    private readonly Dictionary<string, object> _contents;
    #endregion

    #region [ Properties ]
    public bool IsFaulty { get => field || Messages.Any(x => x.Type == Core.Enums.MessageType.Error); private set; } = false;
    public IList<IMessageResult> Messages => field ??= [];
    public string Token { get; private set; }
    public bool IsLocked { get; private set; }
    public dynamic DynamicContents { get; private set; }
    #endregion

    #region [ Methods ]
    public void AddContent<T>(T obj)
    {
        if (obj == null)
        {
            return;
        }

        AddContent<T>(obj.GetType().FullName!, obj);
    }
    public void AddContent<T>(string key, T obj)
    {
        if (obj == null)
        {
            return;
        }

        if (_contents.ContainsKey(key))
        {
            _contents[key] = obj;
        }
        else
        {
            _contents.Add(key, obj);
        }
    }
    public T GetContent<T>()
    {
        return GetContent<T>(typeof(T).FullName!);
    }
    public T GetContent<T>(string key)
    {
        if (_contents.TryGetValue(key, out object? value) && value != null)
        {
            return (T)value;
        }
        return default!;
    }
    public bool HasContent<T>()
    {
        return HasContent(typeof(T).FullName!);
    }
    public bool HasContent(string key)
    {
        return _contents.ContainsKey(key);
    }
    public IList<object> GetContentAll()
    {
        return [.. _contents.Values];
    }
    public void SetLock()
    {
        IsLocked = true;
    }
    public void SetFailure()
    {
        IsFaulty = true;
    }
    #endregion
}
