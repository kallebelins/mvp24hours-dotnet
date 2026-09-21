using Mvp24Hours.Infrastructure.Caching.KeyGenerators;

namespace Mvp24Hours.Infrastructure.Caching.Test.KeyGenerators;

[Trait("Category", "Unit")]
public class DefaultCacheKeyGeneratorTest
{
    [Fact]
    public void Constructor_Default_ShouldUseColonSeparatorAndNoPrefix()
    {
        var generator = new DefaultCacheKeyGenerator();

        generator.DefaultPrefix.Should().BeNull();
        generator.Separator.Should().Be(":");
    }

    [Fact]
    public void Constructor_NullSeparator_ShouldThrow()
    {
        Action act = () => _ = new DefaultCacheKeyGenerator(separator: null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Generate_NullParts_ShouldThrow()
    {
        var generator = new DefaultCacheKeyGenerator();

        Action act = () => generator.Generate(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_EmptyParts_ShouldThrow()
    {
        var generator = new DefaultCacheKeyGenerator();

        Action act = () => generator.Generate();

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_OnlyWhitespaceParts_ShouldThrow()
    {
        var generator = new DefaultCacheKeyGenerator();

        Action act = () => generator.Generate(" ", "", null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_WithParts_ShouldJoinWithSeparator()
    {
        var generator = new DefaultCacheKeyGenerator();

        string key = generator.Generate("Customer", "GetById", "123");

        key.Should().Be("Customer:GetById:123");
    }

    [Fact]
    public void Generate_WithPrefixAndParts_ShouldPrependPrefix()
    {
        var generator = new DefaultCacheKeyGenerator(defaultPrefix: "app");

        string key = generator.Generate("Customer", "123");

        key.Should().Be("app:Customer:123");
    }

    [Fact]
    public void Generate_WithCustomSeparator_ShouldUseSeparator()
    {
        var generator = new DefaultCacheKeyGenerator(separator: "|");

        string key = generator.Generate("Customer", "123");

        key.Should().Be("Customer|123");
    }

    [Fact]
    public void Generate_SkipsWhitespacePartsButKeepsValidOnes()
    {
        var generator = new DefaultCacheKeyGenerator();

        string key = generator.Generate("Customer", " ", "123");

        key.Should().Be("Customer:123");
    }

    [Fact]
    public void GenerateWithPrefix_NullPrefix_ShouldThrow()
    {
        var generator = new DefaultCacheKeyGenerator();

        Action act = () => generator.GenerateWithPrefix(null!, "key");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GenerateWithPrefix_EmptyPrefix_ShouldThrow()
    {
        var generator = new DefaultCacheKeyGenerator();

        Action act = () => generator.GenerateWithPrefix(" ", "key");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GenerateWithPrefix_NullKey_ShouldThrow()
    {
        var generator = new DefaultCacheKeyGenerator();

        Action act = () => generator.GenerateWithPrefix("prefix", null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GenerateWithPrefix_EmptyKey_ShouldThrow()
    {
        var generator = new DefaultCacheKeyGenerator();

        Action act = () => generator.GenerateWithPrefix("prefix", " ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GenerateWithPrefix_ValidInputs_ShouldCombineWithSeparator()
    {
        var generator = new DefaultCacheKeyGenerator(separator: "-");

        string key = generator.GenerateWithPrefix("Customer", "123");

        key.Should().Be("Customer-123");
    }

    [Fact]
    public void GenerateHash_NullKey_ShouldThrow()
    {
        var generator = new DefaultCacheKeyGenerator();

        Action act = () => generator.GenerateHash(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GenerateHash_EmptyKey_ShouldThrow()
    {
        var generator = new DefaultCacheKeyGenerator();

        Action act = () => generator.GenerateHash(" ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GenerateHash_ShouldReturnDeterministicHashWithHashPrefix()
    {
        var generator = new DefaultCacheKeyGenerator();

        string hash1 = generator.GenerateHash("some-long-key-value");
        string hash2 = generator.GenerateHash("some-long-key-value");

        hash1.Should().Be(hash2);
        hash1.Should().StartWith("hash:");
    }

    [Fact]
    public void GenerateHash_DifferentKeys_ShouldProduceDifferentHashes()
    {
        var generator = new DefaultCacheKeyGenerator();

        string hash1 = generator.GenerateHash("key-one");
        string hash2 = generator.GenerateHash("key-two");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void GenerateHash_ShouldNotContainUrlUnsafeCharacters()
    {
        var generator = new DefaultCacheKeyGenerator();

        string hash = generator.GenerateHash("a value that likely produces + / = in base64");

        hash.Should().NotContain("+");
        hash.Should().NotContain("/");
        hash.Should().NotContain("=");
    }

    [Fact]
    public void GenerateFromObject_NullPrefix_ShouldThrow()
    {
        var generator = new DefaultCacheKeyGenerator();

        Action act = () => generator.GenerateFromObject(null!, new { Id = 1 });

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GenerateFromObject_EmptyPrefix_ShouldThrow()
    {
        var generator = new DefaultCacheKeyGenerator();

        Action act = () => generator.GenerateFromObject(" ", new { Id = 1 });

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GenerateFromObject_NullObject_ShouldThrow()
    {
        var generator = new DefaultCacheKeyGenerator();

        Action act = () => generator.GenerateFromObject("prefix", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GenerateFromObject_ShouldReturnPrefixedHashedKey()
    {
        var generator = new DefaultCacheKeyGenerator();

        string key = generator.GenerateFromObject("Customer", new { Id = 1, Name = "Test" });

        key.Should().StartWith("Customer:hash:");
    }

    [Fact]
    public void GenerateFromObject_SameObjectShape_ShouldBeDeterministic()
    {
        var generator = new DefaultCacheKeyGenerator();

        string key1 = generator.GenerateFromObject("Customer", new { Id = 1, Name = "Test" });
        string key2 = generator.GenerateFromObject("Customer", new { Id = 1, Name = "Test" });

        key1.Should().Be(key2);
    }

    [Fact]
    public void GenerateFromObject_DifferentObjects_ShouldProduceDifferentKeys()
    {
        var generator = new DefaultCacheKeyGenerator();

        string key1 = generator.GenerateFromObject("Customer", new { Id = 1 });
        string key2 = generator.GenerateFromObject("Customer", new { Id = 2 });

        key1.Should().NotBe(key2);
    }

    [Fact]
    public void DefaultPrefix_CanBeChangedAfterConstruction()
    {
        var generator = new DefaultCacheKeyGenerator { DefaultPrefix = "changed" };

        string key = generator.Generate("a");

        key.Should().Be("changed:a");
    }

    [Fact]
    public void Separator_CanBeChangedAfterConstruction()
    {
        var generator = new DefaultCacheKeyGenerator { Separator = "#" };

        string key = generator.Generate("a", "b");

        key.Should().Be("a#b");
    }
}
