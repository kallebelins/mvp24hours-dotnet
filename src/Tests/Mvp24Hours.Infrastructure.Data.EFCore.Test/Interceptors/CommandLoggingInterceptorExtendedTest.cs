using Microsoft.Extensions.Logging;
using Moq;
using Mvp24Hours.Infrastructure.Data.EFCore.Interceptors;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Interceptors;

[Trait("Category", "Unit")]
public class CommandLoggingInterceptorExtendedTest
{
    [Fact]
    public void SaveChanges_WithLogAllQueriesAndLogger_ShouldNotThrow()
    {
        Mock<ILogger> logger = new();
        logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        var interceptor = new CommandLoggingInterceptor(logger.Object, logAllQueries: true);

        using TestDbContext context = EfCoreTestHelpers.CreateContext(configure: options =>
            options.AddInterceptors(interceptor));

        context.Entities.Add(new TestEntity { Name = "LoggedAll" });
        Action act = () => context.SaveChanges();

        act.Should().NotThrow();
    }

    [Fact]
    public void SaveChanges_WithSlowQueryThresholdOnly_ShouldNotThrow()
    {
        Mock<ILogger> logger = new();
        logger.Setup(l => l.IsEnabled(LogLevel.Warning)).Returns(true);
        var interceptor = new CommandLoggingInterceptor(
            logger.Object,
            slowQueryThreshold: TimeSpan.FromHours(1),
            logAllQueries: false);

        using TestDbContext context = EfCoreTestHelpers.CreateContext(configure: options =>
            options.AddInterceptors(interceptor));

        context.Entities.Add(new TestEntity { Name = "SlowOnly" });
        Action act = () => context.SaveChanges();

        act.Should().NotThrow();
    }

    [Fact]
    public async Task SaveChangesAsync_WithLogAllQueriesAndLogger_ShouldNotThrow()
    {
        Mock<ILogger> logger = new();
        logger.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(true);
        var interceptor = new CommandLoggingInterceptor(logger.Object, logAllQueries: true);

        await using TestDbContext context = EfCoreTestHelpers.CreateContext(configure: options =>
            options.AddInterceptors(interceptor));

        context.Entities.Add(new TestEntity { Name = "AsyncLogged" });
        Func<Task> act = () => context.SaveChangesAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void SaveChanges_WithSensitiveParameterNames_ShouldNotThrow()
    {
        Mock<ILogger> logger = new();
        logger.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(true);
        var interceptor = new CommandLoggingInterceptor(
            logger.Object,
            logAllQueries: true,
            logParameters: true,
            sensitiveParameters: ["password", "token"]);

        using TestDbContext context = EfCoreTestHelpers.CreateContext(configure: options =>
            options.AddInterceptors(interceptor));

        context.Entities.Add(new TestEntity { Name = "Sensitive" });
        Action act = () => context.SaveChanges();

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithCustomOptions_ShouldCreateInstance()
    {
        var interceptor = new CommandLoggingInterceptor(
            slowQueryThreshold: TimeSpan.FromMilliseconds(500),
            logAllQueries: false,
            logParameters: false,
            sensitiveParameters: ["secret"]);

        interceptor.Should().NotBeNull();
    }
}
