namespace Mvp24Hours.Core.Test.Helpers;

/// <summary>
/// Testes unitários para StringHelper (métodos existentes).
/// </summary>
public class StringHelperTest
{
    #region [ GenerateKey Tests ]

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    public void GenerateKey_ReturnsStringOfCorrectLength(int length)
    {
        // Act
        var result = StringHelper.GenerateKey(length);

        // Assert
        result.Should().HaveLength(length);
        result.Should().MatchRegex("^[A-Z0-9]+$");
    }

    [Fact]
    public void GenerateKey_WithZeroLength_ReturnsEmpty()
    {
        // Act
        var result = StringHelper.GenerateKey(0);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GenerateKey_MultipleCallsProduceDifferentResults()
    {
        // Act
        var result1 = StringHelper.GenerateKey(20);
        var result2 = StringHelper.GenerateKey(20);
        var result3 = StringHelper.GenerateKey(20);

        // Assert
        result1.Should().NotBe(result2);
        result2.Should().NotBe(result3);
        result1.Should().NotBe(result3);
    }

    [Fact]
    public void GenerateKey_OnlyUpperCaseAndNumbers()
    {
        // Act
        var result = StringHelper.GenerateKey(100);

        // Assert
        result.Should().MatchRegex("^[A-Z0-9]+$");
        result.Should().NotContainAny("a", "b", "c", "z"); // Sem letras minúsculas
    }

    [Fact]
    public void GenerateKey_ThreadSafety_MultipleThreadsGeneratingKeys()
    {
        // Arrange
        var results = new System.Collections.Concurrent.ConcurrentBag<string>();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var key = StringHelper.GenerateKey(20);
                results.Add(key);
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        results.Should().HaveCount(100);
        results.Distinct().Should().HaveCount(100); // Todos únicos (alta probabilidade)
    }

    [Fact]
    public void GenerateKey_WithLargeLength_ShouldPerformEfficiently()
    {
        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = StringHelper.GenerateKey(10000);
        stopwatch.Stop();

        // Assert
        result.Should().HaveLength(10000);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
    }

    [Fact]
    public void GenerateKey_ContainsOnlyValidCharacters()
    {
        // Arrange
        var validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        // Act
        var result = StringHelper.GenerateKey(1000);

        // Assert
        foreach (var ch in result)
        {
            validChars.Should().Contain(ch.ToString());
        }
    }

    [Fact]
    public void GenerateKey_Distribution_ContainsVariety()
    {
        // Arrange
        var keys = new List<string>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            keys.Add(StringHelper.GenerateKey(50));
        }

        var allChars = string.Join("", keys);

        // Assert - Deve conter variedade de caracteres
        allChars.Should().Contain("A");
        allChars.Should().Contain("Z");
        allChars.Should().Contain("0");
        allChars.Should().Contain("9");
    }

    #endregion
}
