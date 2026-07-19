//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Moq;
using Mvp24Hours.Infrastructure.DistributedLocking;
using Mvp24Hours.Infrastructure.DistributedLocking.Contract;

namespace Mvp24Hours.Infrastructure.Test.DistributedLocking;

[Trait("Category", "Unit")]
public class DistributedLockFactoryTest
{
    [Fact]
    public void Constructor_WithNullProviders_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new DistributedLockFactory(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("providers");
    }

    [Fact]
    public void Constructor_WithEmptyProviders_ShouldThrowArgumentException()
    {
        Action act = () => _ = new DistributedLockFactory([]);

        act.Should().Throw<ArgumentException>().WithParameterName("providers");
    }

    [Fact]
    public void Create_WithoutDefault_ShouldReturnFirstRegisteredProvider()
    {
        var first = new Mock<IDistributedLock>();
        var second = new Mock<IDistributedLock>();
        var factory = new DistributedLockFactory(new Dictionary<string, IDistributedLock>
        {
            ["first"] = first.Object,
            ["second"] = second.Object
        });

        factory.Create().Should().BeSameAs(first.Object);
    }

    [Fact]
    public void Create_WithDefaultProvider_ShouldReturnDefault()
    {
        var first = new Mock<IDistributedLock>();
        var second = new Mock<IDistributedLock>();
        var factory = new DistributedLockFactory(
            new Dictionary<string, IDistributedLock>
            {
                ["first"] = first.Object,
                ["second"] = second.Object
            },
            "second");

        factory.Create().Should().BeSameAs(second.Object);
    }

    [Fact]
    public void Create_ByName_ShouldBeCaseInsensitive()
    {
        var provider = new Mock<IDistributedLock>();
        var factory = new DistributedLockFactory(new Dictionary<string, IDistributedLock>
        {
            ["InMemory"] = provider.Object
        });

        factory.Create("inmemory").Should().BeSameAs(provider.Object);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ByEmptyName_ShouldThrowArgumentException(string? name)
    {
        var factory = new DistributedLockFactory(new Dictionary<string, IDistributedLock>
        {
            ["InMemory"] = new Mock<IDistributedLock>().Object
        });

        Action act = () => factory.Create(name!);

        act.Should().Throw<ArgumentException>().WithParameterName("providerName");
    }

    [Fact]
    public void Create_WithUnknownName_ShouldThrowArgumentException()
    {
        var factory = new DistributedLockFactory(new Dictionary<string, IDistributedLock>
        {
            ["InMemory"] = new Mock<IDistributedLock>().Object
        });

        Action act = () => factory.Create("Redis");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("providerName")
            .WithMessage("*InMemory*");
    }
}
