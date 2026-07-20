using Mvp24Hours.Application.Logic.Pagination;
using Mvp24Hours.Extensions;

namespace Mvp24Hours.Application.Test.Extensions;

[Trait("Category", "Unit")]
public class PaginationServiceCollectionExtensionsTest
{
    [Fact]
    public void AddMvp24HoursPagination_ShouldRegisterDefaultOptions()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursPagination();
        ServiceProvider provider = services.BuildServiceProvider();

        PaginationOptions options = provider.GetRequiredService<PaginationOptions>();
        options.DefaultPageSize.Should().Be(PaginationHelper.DefaultPageSize);
        options.MaxPageSize.Should().Be(PaginationHelper.MaxPageSize);
    }

    [Fact]
    public void AddMvp24HoursPagination_WithConfigure_ShouldApplyOptions()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursPagination(options =>
        {
            options.DefaultPageSize = 25;
            options.MaxPageSize = 200;
            options.ValidateParameters = false;
        });
        ServiceProvider provider = services.BuildServiceProvider();

        PaginationOptions options = provider.GetRequiredService<PaginationOptions>();
        options.DefaultPageSize.Should().Be(25);
        options.MaxPageSize.Should().Be(200);
        options.ValidateParameters.Should().BeFalse();
    }
}
