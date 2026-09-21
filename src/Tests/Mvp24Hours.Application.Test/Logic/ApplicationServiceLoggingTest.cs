//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
using AutoMapper;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Application.Logic;
using Mvp24Hours.Application.Test.Support;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Application.Test.Logic;

/// <summary>
/// Covers the single logging convention shared by every application service base (task 8.1):
/// <c>ILogger</c> flows in through the constructor, is exposed by the non-nullable
/// <c>Logger</c> property, and falls back to <c>NullLogger</c> when omitted.
/// </summary>
[Trait("Category", "Unit")]
public class ApplicationServiceLoggingTest
{
    #region [ Service_WithoutLogger_UsesNullLoggerAndDoesNotThrow ]

    [Fact]
    public void Service_WithoutLogger_UsesNullLoggerAndDoesNotThrow()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupListAny(repo, true);
        IMapper mapper = ApplicationTestHelpers.CreateAppEntityMapper();

        var applicationService = new LoggingProbeApplicationService(uow.Object, null);
        var queryService = new LoggingProbeQueryService(uow.Object, null);
        var commandService = new LoggingProbeCommandService(uow.Object, null);
        var withDto = new LoggingProbeServiceWithDto(uow.Object, mapper, null);
        var withSeparateDtos = new LoggingProbeServiceWithSeparateDtos(uow.Object, mapper, null);

        applicationService.LoggerForTest.Should().BeSameAs(NullLogger.Instance);
        queryService.LoggerForTest.Should().BeSameAs(NullLogger.Instance);
        commandService.LoggerForTest.Should().BeSameAs(NullLogger.Instance);
        withDto.LoggerForTest.Should().BeSameAs(NullLogger.Instance);
        withSeparateDtos.LoggerForTest.Should().BeSameAs(NullLogger.Instance);

        new LoggingProbeRepositoryService(uow.Object, null).LoggerForTest
            .Should().BeSameAs(NullLogger<RepositoryService<AppTestEntity, IUnitOfWork>>.Instance);
        new LoggingProbePagingService(uow.Object, null).LoggerForTest
            .Should().BeSameAs(NullLogger<RepositoryService<AppTestEntity, IUnitOfWork>>.Instance);

        applicationService.ListAny().Data.Should().BeTrue();
        queryService.ListAny().Data.Should().BeTrue();
        withDto.ListAny().Data.Should().BeTrue();
        withSeparateDtos.ListAny().Data.Should().BeTrue();
        commandService.Add(new AppTestEntity { Name = "no-logger" }).Data.Should().Be(1);
    }

    [Fact]
    public async Task ServiceAsync_WithoutLogger_UsesNullLoggerAndDoesNotThrow()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupListAny(repo, true);
        IMapper mapper = ApplicationTestHelpers.CreateAppEntityMapper();

        var applicationService = new LoggingProbeApplicationServiceAsync(uow.Object, null);
        var queryService = new LoggingProbeQueryServiceAsync(uow.Object, null);
        var commandService = new LoggingProbeCommandServiceAsync(uow.Object, null);
        var withDto = new LoggingProbeServiceWithDtoAsync(uow.Object, mapper, null);
        var withSeparateDtos = new LoggingProbeServiceWithSeparateDtosAsync(uow.Object, mapper, null);

        applicationService.LoggerForTest.Should().BeSameAs(NullLogger.Instance);
        queryService.LoggerForTest.Should().BeSameAs(NullLogger.Instance);
        commandService.LoggerForTest.Should().BeSameAs(NullLogger.Instance);
        withDto.LoggerForTest.Should().BeSameAs(NullLogger.Instance);
        withSeparateDtos.LoggerForTest.Should().BeSameAs(NullLogger.Instance);

        new LoggingProbeRepositoryServiceAsync(uow.Object, null).LoggerForTest
            .Should().BeSameAs(NullLogger<RepositoryServiceAsync<AppTestEntity, IUnitOfWorkAsync>>.Instance);
        new LoggingProbePagingServiceAsync(uow.Object, null).LoggerForTest
            .Should().BeSameAs(NullLogger<RepositoryServiceAsync<AppTestEntity, IUnitOfWorkAsync>>.Instance);

        (await applicationService.ListAnyAsync()).Data.Should().BeTrue();
        (await queryService.ListAnyAsync()).Data.Should().BeTrue();
        (await withDto.ListAnyAsync()).Data.Should().BeTrue();
        (await withSeparateDtos.ListAnyAsync()).Data.Should().BeTrue();
        (await commandService.AddAsync(new AppTestEntity { Name = "no-logger" })).Data.Should().Be(1);
    }

    [Fact]
    public void BulkService_WithoutLogger_UsesNullLogger()
    {
        (Mock<IUnitOfWorkAsync> uow, _) = ApplicationTestHelpers.CreateRepositoryMocks<TestEntity>();
        IBulkOperationsAsync<TestEntity> bulk = new Mock<IBulkOperationsAsync<TestEntity>>().Object;
        IMapper mapper = ApplicationTestHelpers.CreateTestMapper();

        new LoggingProbeBulkService(uow.Object, bulk, null).LoggerForTest.Should().BeSameAs(NullLogger.Instance);
        new LoggingProbeBulkDtoService(uow.Object, bulk, mapper, null).LoggerForTest.Should().BeSameAs(NullLogger.Instance);
        new LoggingProbeBulkSeparateDtosService(uow.Object, bulk, mapper, null).LoggerForTest.Should().BeSameAs(NullLogger.Instance);
    }

    #endregion

    #region [ Service_WithLogger_WritesDebugEntry ]

    [Fact]
    public void Service_WithLogger_WritesDebugEntry()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupListAny(repo, true);
        IMapper mapper = ApplicationTestHelpers.CreateAppEntityMapper();

        AssertWritesDebug(logger => new LoggingProbeApplicationService(uow.Object, logger), s => s.ListAny(), s => s.LoggerForTest);
        AssertWritesDebug(logger => new LoggingProbeQueryService(uow.Object, logger), s => s.ListAny(), s => s.LoggerForTest);
        AssertWritesDebug(logger => new LoggingProbeCommandService(uow.Object, logger), s => s.Add(new AppTestEntity { Name = "logged" }), s => s.LoggerForTest);
        AssertWritesDebug(logger => new LoggingProbeServiceWithDto(uow.Object, mapper, logger), s => s.ListAny(), s => s.LoggerForTest);
        AssertWritesDebug(logger => new LoggingProbeServiceWithSeparateDtos(uow.Object, mapper, logger), s => s.ListAny(), s => s.LoggerForTest);

        // RepositoryService and its paging subclass take a categorized ILogger<T>.
        var repositoryLogger = new CollectingLogger<RepositoryService<AppTestEntity, IUnitOfWork>>();
        var repositoryService = new LoggingProbeRepositoryService(uow.Object, repositoryLogger);
        repositoryService.LoggerForTest.Should().BeSameAs(repositoryLogger);
        repositoryService.ListAny();
        repositoryLogger.Entries.Should().Contain(e => e.Level == LogLevel.Debug && e.Message.Contains("Executing ListAny"));

        var pagingLogger = new CollectingLogger<RepositoryPagingService<AppTestEntity, IUnitOfWork>>();
        var pagingService = new LoggingProbePagingService(uow.Object, pagingLogger);
        pagingService.LoggerForTest.Should().BeSameAs(pagingLogger);
        pagingService.ListAny();
        pagingLogger.Entries.Should().Contain(e => e.Level == LogLevel.Debug && e.Message.Contains("Executing ListAny"));
    }

    [Fact]
    public async Task ServiceAsync_WithLogger_WritesDebugEntry()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupListAny(repo, true);
        IMapper mapper = ApplicationTestHelpers.CreateAppEntityMapper();

        await AssertWritesDebugAsync(logger => new LoggingProbeApplicationServiceAsync(uow.Object, logger), s => s.ListAnyAsync(), s => s.LoggerForTest);
        await AssertWritesDebugAsync(logger => new LoggingProbeQueryServiceAsync(uow.Object, logger), s => s.ListAnyAsync(), s => s.LoggerForTest);
        await AssertWritesDebugAsync(logger => new LoggingProbeCommandServiceAsync(uow.Object, logger), s => s.AddAsync(new AppTestEntity { Name = "logged" }), s => s.LoggerForTest);
        await AssertWritesDebugAsync(logger => new LoggingProbeServiceWithDtoAsync(uow.Object, mapper, logger), s => s.ListAnyAsync(), s => s.LoggerForTest);
        await AssertWritesDebugAsync(logger => new LoggingProbeServiceWithSeparateDtosAsync(uow.Object, mapper, logger), s => s.ListAnyAsync(), s => s.LoggerForTest);

        var repositoryLogger = new CollectingLogger<RepositoryServiceAsync<AppTestEntity, IUnitOfWorkAsync>>();
        var repositoryService = new LoggingProbeRepositoryServiceAsync(uow.Object, repositoryLogger);
        repositoryService.LoggerForTest.Should().BeSameAs(repositoryLogger);
        await repositoryService.ListAnyAsync();
        repositoryLogger.Entries.Should().Contain(e => e.Level == LogLevel.Debug && e.Message.Contains("Executing ListAnyAsync"));

        var pagingLogger = new CollectingLogger<RepositoryPagingServiceAsync<AppTestEntity, IUnitOfWorkAsync>>();
        var pagingService = new LoggingProbePagingServiceAsync(uow.Object, pagingLogger);
        pagingService.LoggerForTest.Should().BeSameAs(pagingLogger);
        await pagingService.ListAnyAsync();
        pagingLogger.Entries.Should().Contain(e => e.Level == LogLevel.Debug && e.Message.Contains("Executing ListAnyAsync"));
    }

    [Fact]
    public async Task BulkService_WithLogger_WritesDebugEntry()
    {
        (Mock<IUnitOfWorkAsync> uow, _) = ApplicationTestHelpers.CreateRepositoryMocks<TestEntity>();
        IBulkOperationsAsync<TestEntity> bulk = new Mock<IBulkOperationsAsync<TestEntity>>().Object;
        IMapper mapper = ApplicationTestHelpers.CreateTestMapper();

        var logger = new CollectingLogger();
        var service = new LoggingProbeBulkService(uow.Object, bulk, logger);
        service.LoggerForTest.Should().BeSameAs(logger);

        await service.BulkAddAsync([]);

        logger.Entries.Should().Contain(e => e.Level == LogLevel.Debug && e.Message.Contains("bulkaddasync-start"));

        new LoggingProbeBulkDtoService(uow.Object, bulk, mapper, logger).LoggerForTest.Should().BeSameAs(logger);
        new LoggingProbeBulkSeparateDtosService(uow.Object, bulk, mapper, logger).LoggerForTest.Should().BeSameAs(logger);
    }

    #endregion

    #region [ Helpers ]

    private static void AssertWritesDebug<TService>(
        Func<ILogger, TService> factory,
        Action<TService> exercise,
        Func<TService, ILogger> loggerAccessor)
    {
        var logger = new CollectingLogger();
        TService service = factory(logger);

        loggerAccessor(service).Should().BeSameAs(logger, "the constructor logger must reach the Logger property");

        exercise(service);

        logger.Entries.Should().Contain(
            e => e.Level == LogLevel.Debug,
            $"{typeof(TService).Name} should emit a Debug entry for the executed operation");
    }

    private static async Task AssertWritesDebugAsync<TService>(
        Func<ILogger, TService> factory,
        Func<TService, Task> exercise,
        Func<TService, ILogger> loggerAccessor)
    {
        var logger = new CollectingLogger();
        TService service = factory(logger);

        loggerAccessor(service).Should().BeSameAs(logger, "the constructor logger must reach the Logger property");

        await exercise(service);

        logger.Entries.Should().Contain(
            e => e.Level == LogLevel.Debug,
            $"{typeof(TService).Name} should emit a Debug entry for the executed operation");
    }

    #endregion
}

/// <summary>
/// Minimal <see cref="ILogger"/> that records every entry, so tests can assert that a
/// service actually uses the injected logger instead of a hardcoded null sink.
/// </summary>
internal class CollectingLogger : ILogger
{
    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        return NoopScope.Instance;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Entries.Add((logLevel, formatter(state, exception)));
    }

    private sealed class NoopScope : IDisposable
    {
        internal static readonly NoopScope Instance = new();

        public void Dispose()
        {
            // no-op
        }
    }
}

internal sealed class CollectingLogger<TCategoryName> : CollectingLogger, ILogger<TCategoryName>
{
}

#region [ Probes ]

internal sealed class LoggingProbeApplicationService(IUnitOfWork unitOfWork, ILogger? logger)
    : ApplicationServiceBase<AppTestEntity, IUnitOfWork>(unitOfWork, null, logger)
{
    public ILogger LoggerForTest => Logger;
}

internal sealed class LoggingProbeQueryService(IUnitOfWork unitOfWork, ILogger? logger)
    : QueryServiceBase<AppTestEntity, IUnitOfWork>(unitOfWork, logger)
{
    public ILogger LoggerForTest => Logger;
}

internal sealed class LoggingProbeCommandService(IUnitOfWork unitOfWork, ILogger? logger)
    : CommandServiceBase<AppTestEntity, IUnitOfWork>(unitOfWork, null, logger)
{
    public ILogger LoggerForTest => Logger;
}

internal sealed class LoggingProbeServiceWithDto(IUnitOfWork unitOfWork, IMapper mapper, ILogger? logger)
    : ApplicationServiceBaseWithDto<AppTestEntity, AppTestEntityDto, IUnitOfWork>(unitOfWork, mapper, null, null, logger)
{
    public ILogger LoggerForTest => Logger;
}

internal sealed class LoggingProbeServiceWithSeparateDtos(IUnitOfWork unitOfWork, IMapper mapper, ILogger? logger)
    : ApplicationServiceBaseWithSeparateDtos<AppTestEntity, AppTestEntityDto, AppTestCreateDto, AppTestUpdateDto>(
        unitOfWork, mapper, null, null, null, logger)
{
    public ILogger LoggerForTest => Logger;
}

internal sealed class LoggingProbeRepositoryService(IUnitOfWork unitOfWork, ILogger<RepositoryService<AppTestEntity, IUnitOfWork>>? logger)
    : RepositoryService<AppTestEntity, IUnitOfWork>(unitOfWork, null, logger)
{
    public ILogger LoggerForTest => Logger;
}

internal sealed class LoggingProbePagingService(IUnitOfWork unitOfWork, ILogger<RepositoryPagingService<AppTestEntity, IUnitOfWork>>? logger)
    : RepositoryPagingService<AppTestEntity, IUnitOfWork>(unitOfWork, null, logger)
{
    public ILogger LoggerForTest => Logger;
}

internal sealed class LoggingProbeApplicationServiceAsync(IUnitOfWorkAsync unitOfWork, ILogger? logger)
    : ApplicationServiceBaseAsync<AppTestEntity, IUnitOfWorkAsync>(unitOfWork, null, logger)
{
    public ILogger LoggerForTest => Logger;
}

internal sealed class LoggingProbeQueryServiceAsync(IUnitOfWorkAsync unitOfWork, ILogger? logger)
    : QueryServiceBaseAsync<AppTestEntity, IUnitOfWorkAsync>(unitOfWork, logger)
{
    public ILogger LoggerForTest => Logger;
}

internal sealed class LoggingProbeCommandServiceAsync(IUnitOfWorkAsync unitOfWork, ILogger? logger)
    : CommandServiceBaseAsync<AppTestEntity, IUnitOfWorkAsync>(unitOfWork, null, logger)
{
    public ILogger LoggerForTest => Logger;
}

internal sealed class LoggingProbeServiceWithDtoAsync(IUnitOfWorkAsync unitOfWork, IMapper mapper, ILogger? logger)
    : ApplicationServiceBaseWithDtoAsync<AppTestEntity, AppTestEntityDto, IUnitOfWorkAsync>(unitOfWork, mapper, null, null, logger)
{
    public ILogger LoggerForTest => Logger;
}

internal sealed class LoggingProbeServiceWithSeparateDtosAsync(IUnitOfWorkAsync unitOfWork, IMapper mapper, ILogger? logger)
    : ApplicationServiceBaseWithSeparateDtosAsync<AppTestEntity, AppTestEntityDto, AppTestCreateDto, AppTestUpdateDto>(
        unitOfWork, mapper, null, null, null, logger)
{
    public ILogger LoggerForTest => Logger;
}

internal sealed class LoggingProbeRepositoryServiceAsync(IUnitOfWorkAsync unitOfWork, ILogger<RepositoryServiceAsync<AppTestEntity, IUnitOfWorkAsync>>? logger)
    : RepositoryServiceAsync<AppTestEntity, IUnitOfWorkAsync>(unitOfWork, null, logger)
{
    public ILogger LoggerForTest => Logger;
}

internal sealed class LoggingProbePagingServiceAsync(IUnitOfWorkAsync unitOfWork, ILogger<RepositoryPagingServiceAsync<AppTestEntity, IUnitOfWorkAsync>>? logger)
    : RepositoryPagingServiceAsync<AppTestEntity, IUnitOfWorkAsync>(unitOfWork, null, logger)
{
    public ILogger LoggerForTest => Logger;
}

internal sealed class LoggingProbeBulkService(IUnitOfWorkAsync unitOfWork, IBulkOperationsAsync<TestEntity> bulkOperations, ILogger? logger)
    : BulkCommandServiceBaseAsync<TestEntity, IUnitOfWorkAsync>(unitOfWork, bulkOperations, null, logger)
{
    public ILogger LoggerForTest => Logger;
}

internal sealed class LoggingProbeBulkDtoService(IUnitOfWorkAsync unitOfWork, IBulkOperationsAsync<TestEntity> bulkOperations, IMapper mapper, ILogger? logger)
    : BulkCommandServiceWithDtoBaseAsync<TestEntity, AppTestEntityDto, IUnitOfWorkAsync>(unitOfWork, bulkOperations, mapper, null, null, logger)
{
    public ILogger LoggerForTest => Logger;
}

internal sealed class LoggingProbeBulkSeparateDtosService(IUnitOfWorkAsync unitOfWork, IBulkOperationsAsync<TestEntity> bulkOperations, IMapper mapper, ILogger? logger)
    : BulkCommandServiceWithSeparateDtosBaseAsync<TestEntity, AppTestCreateDto, AppTestUpdateDto, IUnitOfWorkAsync>(
        unitOfWork, bulkOperations, mapper, null, null, null, logger)
{
    public ILogger LoggerForTest => Logger;
}

#endregion
