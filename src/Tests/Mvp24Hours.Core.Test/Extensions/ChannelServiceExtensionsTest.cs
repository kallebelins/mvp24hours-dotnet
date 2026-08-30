using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Infrastructure.Channels;
using Mvp24Hours.Core.Extensions;

namespace Mvp24Hours.Core.Test.Extensions;

[Trait("Category", "Unit")]
public class ChannelServiceExtensionsTest
{
    #region [ AddMvpChannels ]

    [Fact]
    public void AddMvpChannels_RegistersChannelFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMvpChannels();
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        provider.GetRequiredService<IChannelFactory>().Should().NotBeNull();
    }

    #endregion

    #region [ AddBoundedChannel ]

    [Fact]
    public async Task AddBoundedChannel_WritesAndReadsThroughSameChannel()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBoundedChannel<int>(capacity: 10);
        ServiceProvider provider = services.BuildServiceProvider();
        IChannelWriter<int> writer = provider.GetRequiredService<IChannelWriter<int>>();
        IChannelReader<int> reader = provider.GetRequiredService<IChannelReader<int>>();

        // Act
        await writer.WriteAsync(42);
        int result = await reader.ReadAsync();

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void AddBoundedChannel_RegistersIChannel()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBoundedChannel<string>(capacity: 5);
        ServiceProvider provider = services.BuildServiceProvider();

        // Act
        IChannel<string> channel = provider.GetRequiredService<IChannel<string>>();

        // Assert
        channel.Should().NotBeNull();
    }

    #endregion

    #region [ AddUnboundedChannel ]

    [Fact]
    public async Task AddUnboundedChannel_WritesAndReadsThroughSameChannel()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddUnboundedChannel<string>();
        ServiceProvider provider = services.BuildServiceProvider();
        IChannelWriter<string> writer = provider.GetRequiredService<IChannelWriter<string>>();
        IChannelReader<string> reader = provider.GetRequiredService<IChannelReader<string>>();

        // Act
        await writer.WriteAsync("hello");
        string result = await reader.ReadAsync();

        // Assert
        result.Should().Be("hello");
    }

    #endregion

    #region [ AddChannel with options ]

    [Fact]
    public async Task AddChannel_WithMvpChannelOptions_ConfiguresCapacity()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddChannel<int>(new MvpChannelOptions { IsBounded = true, Capacity = 2, FullMode = BoundedChannelFullMode.Wait });
        ServiceProvider provider = services.BuildServiceProvider();
        IChannelWriter<int> writer = provider.GetRequiredService<IChannelWriter<int>>();
        IChannelReader<int> reader = provider.GetRequiredService<IChannelReader<int>>();

        // Act
        await writer.WriteAsync(1);
        await writer.WriteAsync(2);
        int first = await reader.ReadAsync();
        int second = await reader.ReadAsync();

        // Assert
        first.Should().Be(1);
        second.Should().Be(2);
    }

    [Fact]
    public async Task AddChannel_WithConfigureCallback_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddChannel<int>(options =>
        {
            options.IsBounded = true;
            options.Capacity = 3;
        });
        ServiceProvider provider = services.BuildServiceProvider();
        IChannelWriter<int> writer = provider.GetRequiredService<IChannelWriter<int>>();
        IChannelReader<int> reader = provider.GetRequiredService<IChannelReader<int>>();

        // Act
        await writer.WriteAsync(99);
        int result = await reader.ReadAsync();

        // Assert
        result.Should().Be(99);
    }

    #endregion

    #region [ AddKeyedBoundedChannel ]

    [Fact]
    public void AddKeyedBoundedChannel_RegistersChannelUnderKey()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKeyedBoundedChannel<int>("orders", capacity: 10);
        ServiceProvider provider = services.BuildServiceProvider();

        // Act
        IChannel<int> channel = provider.GetRequiredKeyedService<IChannel<int>>("orders");

        // Assert
        channel.Should().NotBeNull();
    }

    #endregion

    #region [ AddHighThroughputChannel ]

    [Fact]
    public async Task AddHighThroughputChannel_WritesAndReadsThroughSameChannel()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHighThroughputChannel<int>(capacity: 50);
        ServiceProvider provider = services.BuildServiceProvider();
        IChannelWriter<int> writer = provider.GetRequiredService<IChannelWriter<int>>();
        IChannelReader<int> reader = provider.GetRequiredService<IChannelReader<int>>();

        // Act
        await writer.WriteAsync(7);
        int result = await reader.ReadAsync();

        // Assert
        result.Should().Be(7);
    }

    #endregion

    #region [ AddDropOldestChannel ]

    [Fact]
    public async Task AddDropOldestChannel_WhenFull_DropsOldestItem()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDropOldestChannel<int>(capacity: 1);
        ServiceProvider provider = services.BuildServiceProvider();
        IChannelWriter<int> writer = provider.GetRequiredService<IChannelWriter<int>>();
        IChannelReader<int> reader = provider.GetRequiredService<IChannelReader<int>>();

        // Act
        await writer.WriteAsync(1);
        await writer.WriteAsync(2);
        int result = await reader.ReadAsync();

        // Assert - oldest (1) was dropped, only newest (2) remains
        result.Should().Be(2);
    }

    #endregion

    #region [ AddDropWriteChannel ]

    [Fact]
    public async Task AddDropWriteChannel_WhenFull_DropsNewWrite()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDropWriteChannel<int>(capacity: 1);
        ServiceProvider provider = services.BuildServiceProvider();
        IChannelWriter<int> writer = provider.GetRequiredService<IChannelWriter<int>>();
        IChannelReader<int> reader = provider.GetRequiredService<IChannelReader<int>>();

        // Act
        await writer.WriteAsync(1);
        await writer.WriteAsync(2);
        int result = await reader.ReadAsync();

        // Assert - the new write (2) was dropped, original (1) remains
        result.Should().Be(1);
    }

    #endregion
}
