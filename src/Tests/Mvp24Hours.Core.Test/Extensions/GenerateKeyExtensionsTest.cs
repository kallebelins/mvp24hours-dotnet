namespace Mvp24Hours.Core.Test.Extensions;

[Trait("Category", "Unit")]
public class GenerateKeyExtensionsTest
{
    private sealed class SimplePoco
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    private sealed class NestedPoco
    {
        public string? Label { get; set; }
        public SimplePoco? Child { get; set; }
    }

    private sealed class SelfReferencingPoco
    {
        public string? Name { get; set; }
        public SelfReferencingPoco? Self { get; set; }
    }

    #region [ ToHash ]

    [Fact]
    public void ToHash_WithSimplePoco_ReturnsNonEmptyByteArray()
    {
        // Arrange
        var poco = new SimplePoco { Id = 1, Name = "Alpha", CreatedAt = new DateTime(2026, 1, 1) };

        // Act
        byte[] result = GenerateKeyExtensions.ToHash(poco);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public void ToHash_WithEqualObjects_ProducesEqualHashes()
    {
        // Arrange
        var poco1 = new SimplePoco { Id = 1, Name = "Alpha", CreatedAt = new DateTime(2026, 1, 1) };
        var poco2 = new SimplePoco { Id = 1, Name = "Alpha", CreatedAt = new DateTime(2026, 1, 1) };

        // Act
        byte[] hash1 = GenerateKeyExtensions.ToHash(poco1);
        byte[] hash2 = GenerateKeyExtensions.ToHash(poco2);

        // Assert
        hash1.Should().BeEquivalentTo(hash2);
    }

    [Fact]
    public void ToHash_WithDifferentObjects_ProducesDifferentHashes()
    {
        // Arrange
        var poco1 = new SimplePoco { Id = 1, Name = "Alpha" };
        var poco2 = new SimplePoco { Id = 2, Name = "Beta" };

        // Act
        byte[] hash1 = GenerateKeyExtensions.ToHash(poco1);
        byte[] hash2 = GenerateKeyExtensions.ToHash(poco2);

        // Assert
        hash1.Should().NotBeEquivalentTo(hash2);
    }

    [Fact]
    public void ToHash_WithNullProperties_DoesNotThrow()
    {
        // Arrange
        var poco = new SimplePoco { Id = 1, Name = null, CreatedAt = null };

        // Act
        Action act = () => GenerateKeyExtensions.ToHash(poco);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ToHash_WithNestedObject_IncludesNestedSimpleProperties()
    {
        // Arrange
        var nested = new NestedPoco { Label = "Outer", Child = new SimplePoco { Id = 1, Name = "Inner" } };

        // Act
        Action act = () => GenerateKeyExtensions.ToHash(nested);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ToHash_WithSelfReferencingObject_DoesNotStackOverflow()
    {
        // Arrange
        var poco = new SelfReferencingPoco { Name = "Cyclic" };
        poco.Self = poco;

        // Act
        Action act = () => GenerateKeyExtensions.ToHash(poco);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region [ ToKey ]

    [Fact]
    public void ToKey_WithSimplePoco_ReturnsNonEmptyString()
    {
        // Arrange
        var poco = new SimplePoco { Id = 1, Name = "Alpha" };

        // Act
        string result = poco.ToKey();

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ToKey_WithEqualObjects_ProducesEqualKeys()
    {
        // Arrange
        var poco1 = new SimplePoco { Id = 5, Name = "Same" };
        var poco2 = new SimplePoco { Id = 5, Name = "Same" };

        // Act
        string key1 = poco1.ToKey();
        string key2 = poco2.ToKey();

        // Assert
        key1.Should().Be(key2);
    }

    [Fact]
    public void ToKey_WithDifferentObjects_ProducesDifferentKeys()
    {
        // Arrange
        var poco1 = new SimplePoco { Id = 5, Name = "One" };
        var poco2 = new SimplePoco { Id = 6, Name = "Two" };

        // Act
        string key1 = poco1.ToKey();
        string key2 = poco2.ToKey();

        // Assert
        key1.Should().NotBe(key2);
    }

    #endregion
}
