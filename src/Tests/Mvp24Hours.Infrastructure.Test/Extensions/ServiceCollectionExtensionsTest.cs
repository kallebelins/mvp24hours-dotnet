//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;

namespace Mvp24Hours.Infrastructure.Test.Extensions;

[Trait("Category", "Unit")]
public class ServiceCollectionExtensionsTest
{
    [Fact]
    public void AddMvp24HoursMapService_WithNullAssembly_ShouldThrowArgumentNullException()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddMvp24HoursMapService(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("assemblyMap");
    }

    [Fact]
    public void AddMvp24HoursMapService_ShouldRegisterIMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursMapService(typeof(ServiceCollectionExtensionsTest).Assembly);

        ServiceProvider sp = services.BuildServiceProvider();

        sp.GetRequiredService<IMapper>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursTimeZone_WithClearList_ShouldReplaceDefaults()
    {
        List<string> originalIds = [.. TimeZoneHelper.TimeZoneIds];

        try
        {
            var services = new ServiceCollection();
            services.AddMvp24HoursTimeZone(clearList: true, "UTC");

            TimeZoneHelper.TimeZoneIds.Should().Equal("UTC");
        }
        finally
        {
            TimeZoneHelper.TimeZoneIds.Clear();
            TimeZoneHelper.TimeZoneIds.AddRange(originalIds);
        }
    }

    [Fact]
    public void AddMvp24HoursTimeZone_WithoutClearList_ShouldAppendTimeZones()
    {
        List<string> originalIds = [.. TimeZoneHelper.TimeZoneIds];

        try
        {
            var services = new ServiceCollection();
            services.AddMvp24HoursTimeZone(clearList: false, "Pacific Standard Time");

            TimeZoneHelper.TimeZoneIds.Should().Contain("Pacific Standard Time");
            TimeZoneHelper.TimeZoneIds.Should().Contain(originalIds[0]);
        }
        finally
        {
            TimeZoneHelper.TimeZoneIds.Clear();
            TimeZoneHelper.TimeZoneIds.AddRange(originalIds);
        }
    }

    [Fact]
    public void AddMvp24HoursTimeZone_WithEmptyArgs_ShouldReturnServicesUnchanged()
    {
        List<string> originalIds = [.. TimeZoneHelper.TimeZoneIds];
        var services = new ServiceCollection();

        IServiceCollection result = services.AddMvp24HoursTimeZone(clearList: true);

        result.Should().BeSameAs(services);
        TimeZoneHelper.TimeZoneIds.Should().BeEmpty();

        TimeZoneHelper.TimeZoneIds.Clear();
        TimeZoneHelper.TimeZoneIds.AddRange(originalIds);
    }
}
