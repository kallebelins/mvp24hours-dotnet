using System.Data.Common;
using System.Reflection;
using Moq;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Data.EFCore.Interceptors;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Interceptors;

[Trait("Category", "Unit")]
public class EfCoreCacheInterceptorTest
{
    private static ICacheProvider CreateMockCacheProvider()
    {
        var mock = new Mock<ICacheProvider>();
        mock.Setup(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        return mock.Object;
    }

    private static ICacheKeyGenerator CreateMockKeyGenerator()
    {
        var mock = new Mock<ICacheKeyGenerator>();
        mock.Setup(x => x.Generate(It.IsAny<string[]>()))
            .Returns((string[] parts) => string.Join(":", parts));
        mock.Setup(x => x.GenerateHash(It.IsAny<string>()))
            .Returns((string key) => $"hash:{key.GetHashCode()}");
        return mock.Object;
    }

    [Fact]
    public void EfCoreCacheOptions_DefaultValues_ShouldBeExpected()
    {
        var options = new EfCoreCacheOptions();

        options.DefaultCacheDurationSeconds.Should().Be(300);
        options.EnableCaching.Should().BeTrue();
        options.InvalidateOnModify.Should().BeTrue();
    }

    [Fact]
    public void Constructor_NullCacheProvider_ShouldThrow()
    {
        Action act = () => new EfCoreCacheInterceptor(null!, CreateMockKeyGenerator());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullKeyGenerator_ShouldThrow()
    {
        Action act = () => new EfCoreCacheInterceptor(CreateMockCacheProvider(), null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsSelectQuery_ShouldDetectSelectStatements()
    {
        var interceptor = new EfCoreCacheInterceptor(CreateMockCacheProvider(), CreateMockKeyGenerator());

        bool isSelect = InvokePrivate<bool>(interceptor, "IsSelectQuery", "SELECT * FROM Customers");
        bool isUpdate = InvokePrivate<bool>(interceptor, "IsSelectQuery", "UPDATE Customers SET Name = 'A'");

        isSelect.Should().BeTrue();
        isUpdate.Should().BeFalse();
    }

    [Fact]
    public void IsModificationQuery_ShouldDetectWriteStatements()
    {
        var interceptor = new EfCoreCacheInterceptor(CreateMockCacheProvider(), CreateMockKeyGenerator());

        bool isDelete = InvokePrivate<bool>(interceptor, "IsModificationQuery", "DELETE FROM Orders WHERE Id = 1");
        bool isSelect = InvokePrivate<bool>(interceptor, "IsModificationQuery", "SELECT 1");

        isDelete.Should().BeTrue();
        isSelect.Should().BeFalse();
    }

    [Fact]
    public void GenerateCacheKey_ShouldIncludeTableAndParameters()
    {
        var interceptor = new EfCoreCacheInterceptor(CreateMockCacheProvider(), CreateMockKeyGenerator());
        var command = new TestDbCommand
        {
            CommandText = "SELECT * FROM Products WHERE Id = @id"
        };
        DbParameter idParam = command.CreateParameter();
        idParam.ParameterName = "@id";
        idParam.Value = 42;
        command.Parameters.Add(idParam);

        string cacheKey = InvokePrivate<string>(interceptor, "GenerateCacheKey", command);

        cacheKey.Should().NotBeNullOrWhiteSpace();
        cacheKey.Should().Contain("Products");
    }

    [Fact]
    public void ExtractTableName_ShouldParseFromClause()
    {
        var interceptor = new EfCoreCacheInterceptor(CreateMockCacheProvider(), CreateMockKeyGenerator());

        string? table = InvokePrivate<string?>(interceptor, "ExtractTableName", "SELECT * FROM Orders WHERE Id = 1");

        table.Should().Be("Orders");
    }

    private static T InvokePrivate<T>(object instance, string methodName, params object?[] args)
    {
        MethodInfo? method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        method.Should().NotBeNull();
        object? result = method!.Invoke(instance, args);
        return (T)result!;
    }
}
