using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using MessagePack;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Caching.Providers;

namespace Mvp24Hours.Infrastructure.Caching.Test.Support;

internal static class CacheTestHelpers
{
    internal static MemoryCacheProvider CreateMemoryProvider(IMemoryCache? cache = null)
    {
        return new MemoryCacheProvider(cache ?? new MemoryCache(new MemoryCacheOptions()));
    }

    internal static DistributedCacheProvider CreateDistributedProvider(IDistributedCache? cache = null)
    {
        cache ??= new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        return new DistributedCacheProvider(cache);
    }

#pragma warning disable CS0618
    internal static MultiLevelCache CreateMultiLevelCache(
        ICacheProvider? l1 = null,
        ICacheProvider? l2 = null,
        ICacheSynchronizer? synchronizer = null)
    {
        return new MultiLevelCache(
            l1 ?? CreateMemoryProvider(),
            l2 ?? CreateDistributedProvider(),
            synchronizer);
    }
#pragma warning restore CS0618
}

[MessagePackObject(AllowPrivate = true)]
internal sealed class MessagePackCacheItem
{
    [Key(0)]
    public int Id { get; set; }

    [Key(1)]
    public string Name { get; set; } = string.Empty;
}

public sealed class CacheRepositoryEntity : IEntityBase
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public object? EntityKey => Id;
}

internal sealed class TestDbCommand : DbCommand
{
    private string _commandText = string.Empty;

    [AllowNull]
    public override string CommandText
    {
        get => _commandText;
        set => _commandText = value ?? string.Empty;
    }

    public override int CommandTimeout { get; set; }

    public override CommandType CommandType { get; set; } = CommandType.Text;

    public override bool DesignTimeVisible { get; set; }

    public override UpdateRowSource UpdatedRowSource { get; set; }

    protected override DbConnection? DbConnection { get; set; }

    protected override DbParameterCollection DbParameterCollection { get; } = new TestDbParameterCollection();

    protected override DbTransaction? DbTransaction { get; set; }

    public override void Cancel()
    {
    }

    public override int ExecuteNonQuery()
    {
        return 0;
    }

    public override object? ExecuteScalar()
    {
        return null;
    }

    public override void Prepare()
    {
    }

    protected override DbParameter CreateDbParameter()
    {
        return new TestDbParameter();
    }

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        throw new NotSupportedException();
    }

    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(ExecuteNonQuery());
    }
}

internal sealed class TestDbParameter : DbParameter
{
    private string _parameterName = string.Empty;
    private string _sourceColumn = string.Empty;

    public override DbType DbType { get; set; } = DbType.String;

    public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;

    public override bool IsNullable { get; set; } = true;

    [AllowNull]
    public override string ParameterName
    {
        get => _parameterName;
        set => _parameterName = value ?? string.Empty;
    }

    public override int Size { get; set; }

    [AllowNull]
    public override string SourceColumn
    {
        get => _sourceColumn;
        set => _sourceColumn = value ?? string.Empty;
    }

    public override bool SourceColumnNullMapping { get; set; }

    public override object? Value { get; set; }

    public override void ResetDbType()
    {
    }
}

internal sealed class TestDbParameterCollection : DbParameterCollection
{
    private readonly List<DbParameter> _parameters = [];

    public override int Count => _parameters.Count;

    public override object SyncRoot { get; } = new();

    public override int Add(object value)
    {
        _parameters.Add((DbParameter)value);
        return _parameters.Count - 1;
    }

    public override void AddRange(Array values)
    {
        foreach (object value in values)
        {
            Add(value);
        }
    }

    public override void Clear()
    {
        _parameters.Clear();
    }

    public override bool Contains(object value)
    {
        return _parameters.Contains((DbParameter)value);
    }

    public override bool Contains(string value)
    {
        return _parameters.Any(p => p.ParameterName == value);
    }

    public override void CopyTo(Array array, int index)
    {
        _parameters.CopyTo((DbParameter[])array, index);
    }

    public override System.Collections.IEnumerator GetEnumerator()
    {
        return _parameters.GetEnumerator();
    }

    public override int IndexOf(object value)
    {
        return _parameters.IndexOf((DbParameter)value);
    }

    public override int IndexOf(string parameterName)
    {
        return _parameters.FindIndex(p => p.ParameterName == parameterName);
    }

    public override void Insert(int index, object value)
    {
        _parameters.Insert(index, (DbParameter)value);
    }

    public override void Remove(object value)
    {
        _parameters.Remove((DbParameter)value);
    }

    public override void RemoveAt(int index)
    {
        _parameters.RemoveAt(index);
    }

    public override void RemoveAt(string parameterName)
    {
        int index = IndexOf(parameterName);
        if (index >= 0)
        {
            RemoveAt(index);
        }
    }

    protected override DbParameter GetParameter(int index)
    {
        return _parameters[index];
    }

    protected override DbParameter GetParameter(string parameterName)
    {
        return _parameters.First(p => p.ParameterName == parameterName);
    }

    protected override void SetParameter(int index, DbParameter value)
    {
        _parameters[index] = value;
    }

    protected override void SetParameter(string parameterName, DbParameter value)
    {
        int index = IndexOf(parameterName);
        if (index >= 0)
        {
            _parameters[index] = value;
        }
    }
}

internal sealed class FakeRedisDatabase
{
    public List<(string Channel, string Message)> PublishedMessages { get; } = [];

    public Task<long> PublishAsync(string channel, string message)
    {
        PublishedMessages.Add((channel, message));
        return Task.FromResult(1L);
    }
}

internal sealed class FakeRedisConnection
{
    public bool IsConnected { get; set; } = true;

    private readonly FakeRedisDatabase _database = new();

    public FakeRedisDatabase GetDatabase(int db = -1)
    {
        return _database;
    }

    public FakeRedisDatabase GetDatabase()
    {
        return _database;
    }
}
