using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.SchemaValidation;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;
using Mvp24Hours.Infrastructure.Data.EFCore.Testing;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.SchemaValidation;

[Trait("Category", "Unit")]
public class SchemaValidationExtensionsTest
{
    [Fact]
    public void AddMvp24HoursSchemaValidator_ShouldRegisterValidator()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursInMemoryDbContext<TestDbContext>();
        services.AddMvp24HoursSchemaValidator<TestDbContext>();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        ISchemaValidator validator = scope.ServiceProvider.GetRequiredService<ISchemaValidator>();
        validator.Should().BeOfType<SchemaValidator<TestDbContext>>();
    }

    [Fact]
    public void AddMvp24HoursSchemaValidationOnStartup_ShouldRegisterHostedService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursInMemoryDbContext<TestDbContext>();
        services.AddMvp24HoursSchemaValidationOnStartup<TestDbContext>(SchemaValidationOptions.Production());

        services.Should().Contain(d =>
            d.ServiceType == typeof(IHostedService) &&
            d.ImplementationType == typeof(SchemaValidationHostedService<TestDbContext>));
    }
}
