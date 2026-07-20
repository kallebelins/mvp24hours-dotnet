using Mvp24Hours.Application.Contract.Resilience;
using Mvp24Hours.Application.Contract.Transaction;
using Mvp24Hours.Application.Extensions;
using Mvp24Hours.Application.Test.Support;
using Mvp24Hours.Extensions;

namespace Mvp24Hours.Application.Test.Extensions;

[Trait("Category", "Unit")]
public class TransactionServiceCollectionExtensionsTest
{
    [Fact]
    public void AddTransactionScope_ShouldRegisterFactoryAndScopes()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IUnitOfWorkAsync, MockUnitOfWorkAsync>();
        services.AddSingleton<IUnitOfWork, MockUnitOfWork>();

        services.AddTransactionScope();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ITransactionScopeFactory>().Should().NotBeNull();
        provider.GetRequiredService<ITransactionScope>().Should().NotBeNull();
        provider.GetRequiredService<ITransactionScopeSync>().Should().NotBeNull();
    }

    [Fact]
    public void AddTransactionScope_WithConfigure_ShouldRegisterOptions()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IUnitOfWorkAsync, MockUnitOfWorkAsync>();

        services.AddTransactionScope(options => options.DefaultTimeoutSeconds = 60);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<TransactionScopeOptions>().DefaultTimeoutSeconds.Should().Be(60);
    }

    [Fact]
    public void AddTransactionScope_WithNullConfigure_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddTransactionScope((Action<TransactionScopeOptions>)null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configure");
    }
}
