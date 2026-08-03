using System.Reflection;
using Mvp24Hours.Application.Test.Support;

namespace Mvp24Hours.Application.Test.Logic.Cache;

[Trait("Category", "Unit")]
public class QueryCacheKeyGeneratorTest
{
    private readonly QueryCacheKeyGenerator _generator = new();

    [Fact]
    public void GenerateKey_FromCacheableQuery_ShouldUseQueryKey()
    {
        var query = new TestCacheableQuery { CategoryId = 7 };

        string key = _generator.GenerateKey(query);

        key.Should().Be("category_7");
    }

    [Fact]
    public void GenerateKey_FromCacheableQuery_WithNull_ShouldThrow()
    {
        Func<string> act = () => _generator.GenerateKey((TestCacheableQuery)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GenerateKey_FromMethod_ShouldIncludeEntityAndMethodName()
    {
        MethodInfo method = typeof(QueryCacheKeyGeneratorTest).GetMethod(
            nameof(SampleMethod),
            BindingFlags.NonPublic | BindingFlags.Static)!;
        object?[] parameters = [10, "abc"];

        string key = _generator.GenerateKey(method, parameters, typeof(AppTestEntity));

        key.Should().StartWith("AppTestEntity:SampleMethod:");
    }

    [Fact]
    public void GenerateKeyFromTemplate_ShouldReplaceNamedPlaceholders()
    {
        var parameters = new Dictionary<string, object?>
        {
            ["entity"] = "Product",
            ["id"] = 99
        };

        string key = _generator.GenerateKeyFromTemplate("{entity}:GetById:{id}", parameters);

        key.Should().Be("Product:GetById:99");
    }

    [Fact]
    public void GenerateRegionKey_ShouldPrefixWithRegion()
    {
        string key = _generator.GenerateRegionKey<AppTestEntity>();
        key.Should().Be("region:AppTestEntity");
    }

    [Fact]
    public void GenerateInvalidationPattern_ShouldAppendWildcard()
    {
        string pattern = _generator.GenerateInvalidationPattern(typeof(AppTestEntity), "ListAsync");
        pattern.Should().Be("AppTestEntity:ListAsync:*");
    }

    private static void SampleMethod(int id, string name) { }
}
