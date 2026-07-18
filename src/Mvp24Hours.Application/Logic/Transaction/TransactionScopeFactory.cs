//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Application.Contract.Transaction;
using Mvp24Hours.Core.Contract.Data;

namespace Mvp24Hours.Application.Logic.Transaction;

/// <summary>
/// Default implementation of <see cref="ITransactionScopeFactory"/>.
/// Creates transaction scopes using the registered unit of work from DI.
/// </summary>
/// <remarks>
/// <para>
/// This factory resolves unit of work instances from the DI container and
/// creates appropriately configured transaction scopes.
/// </para>
/// <para>
/// <strong>Registration:</strong>
/// <code>
/// services.AddTransactionScope();
/// // or
/// services.AddTransactionScope&lt;MyDbContext&gt;();
/// </code>
/// </para>
/// </remarks>
/// <remarks>
/// Creates a new instance of <see cref="TransactionScopeFactory"/>.
/// </remarks>
/// <param name="serviceProvider">The service provider for resolving dependencies.</param>
/// <param name="loggerFactory">Optional logger factory.</param>
public sealed class TransactionScopeFactory(
    IServiceProvider serviceProvider,
    ILoggerFactory? loggerFactory = null) : ITransactionScopeFactory
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly ILoggerFactory? _loggerFactory = loggerFactory;

    /// <inheritdoc />
    public ITransactionScope Create()
    {
        IUnitOfWorkAsync unitOfWork = _serviceProvider.GetRequiredService<IUnitOfWorkAsync>();
        ILogger<TransactionScope>? logger = _loggerFactory?.CreateLogger<TransactionScope>();
        return new TransactionScope(unitOfWork, logger);
    }

    /// <inheritdoc />
    public ITransactionScope Create<TUnitOfWork>() where TUnitOfWork : class, IUnitOfWorkAsync
    {
        TUnitOfWork unitOfWork = _serviceProvider.GetRequiredService<TUnitOfWork>();
        ILogger<TransactionScope>? logger = _loggerFactory?.CreateLogger<TransactionScope>();
        return new TransactionScope(unitOfWork, logger);
    }

    /// <inheritdoc />
    public ITransactionScopeSync CreateSync()
    {
        IUnitOfWork unitOfWork = _serviceProvider.GetRequiredService<IUnitOfWork>();
        ILogger<TransactionScopeSync>? logger = _loggerFactory?.CreateLogger<TransactionScopeSync>();
        return new TransactionScopeSync(unitOfWork, logger);
    }

    /// <inheritdoc />
    public ITransactionScopeSync CreateSync<TUnitOfWork>() where TUnitOfWork : class, IUnitOfWork
    {
        TUnitOfWork unitOfWork = _serviceProvider.GetRequiredService<TUnitOfWork>();
        ILogger<TransactionScopeSync>? logger = _loggerFactory?.CreateLogger<TransactionScopeSync>();
        return new TransactionScopeSync(unitOfWork, logger);
    }
}

