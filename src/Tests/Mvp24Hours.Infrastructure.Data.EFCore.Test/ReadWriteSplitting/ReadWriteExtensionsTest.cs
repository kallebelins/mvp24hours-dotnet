using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.ReadWriteSplitting;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.ReadWriteSplitting;

[Trait("Category", "Unit")]
public class ReadWriteExtensionsTest
{
    [Fact]
    public void AddMvp24HoursReadWriteSplitting_ShouldRegisterCoreServices()
    {
        var services = new ServiceCollection();
        services.AddMvp24HoursReadWriteSplitting<TestDbContext>(options =>
        {
            options.PrimaryConnectionString = "Server=primary;Database=App;";
            options.ReplicaConnectionStrings = ["Server=replica;Database=App;"];
        });

        services.Should().Contain(d => d.ServiceType == typeof(IReplicaSelector));
        services.Should().Contain(d => d.ServiceType == typeof(IConnectionResolver));
        services.Should().Contain(d => d.ServiceType == typeof(TestDbContext));
    }

    [Fact]
    public void AddMvp24HoursSimpleReadWriteSplitting_ShouldConfigureOptions()
    {
        var services = new ServiceCollection();
        services.AddMvp24HoursSimpleReadWriteSplitting<TestDbContext>("primary", "replica");

        using ServiceProvider provider = services.BuildServiceProvider();
        ReadWriteOptions options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ReadWriteOptions>>().Value;

        options.PrimaryConnectionString.Should().Be("primary");
        options.ReplicaConnectionStrings.Should().ContainSingle("replica");
    }

    [Fact]
    public void AddMvp24HoursReadWriteSplitting_ShouldReturnSameServiceCollectionForChaining()
    {
        var services = new ServiceCollection();

        IServiceCollection result = services.AddMvp24HoursReadWriteSplitting<TestDbContext>(options =>
        {
            options.PrimaryConnectionString = "primary";
            options.ReplicaConnectionStrings = ["replica"];
        });

        result.Should().BeSameAs(services);
    }
}
