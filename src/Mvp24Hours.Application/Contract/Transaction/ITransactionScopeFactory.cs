//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Data;

namespace Mvp24Hours.Application.Contract.Transaction
{
    /// <summary>
    /// Factory for creating transaction scope instances.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This factory supports creating transaction scopes for different unit of work types,
    /// enabling cross-repository transaction coordination.
    /// </para>
    /// </remarks>
    public interface ITransactionScopeFactory
    {
        /// <summary>
        /// Creates a new transaction scope for the default unit of work.
        /// </summary>
        /// <returns>A new transaction scope instance.</returns>
        ITransactionScope Create();

        /// <summary>
        /// Creates a new transaction scope for a specific unit of work type.
        /// </summary>
        /// <typeparam name="TUnitOfWork">The type of unit of work.</typeparam>
        /// <returns>A new transaction scope instance.</returns>
        ITransactionScope Create<TUnitOfWork>() where TUnitOfWork : class, IUnitOfWorkAsync;

        /// <summary>
        /// Creates a new synchronous transaction scope.
        /// </summary>
        /// <returns>A new synchronous transaction scope instance.</returns>
        ITransactionScopeSync CreateSync();

        /// <summary>
        /// Creates a new synchronous transaction scope for a specific unit of work type.
        /// </summary>
        /// <typeparam name="TUnitOfWork">The type of unit of work.</typeparam>
        /// <returns>A new synchronous transaction scope instance.</returns>
        ITransactionScopeSync CreateSync<TUnitOfWork>() where TUnitOfWork : class, IUnitOfWork;
    }
}

