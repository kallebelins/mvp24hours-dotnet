//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Entity;

namespace Mvp24Hours.Core.Contract.Data
{
    /// <summary>
    /// Design Pattern: Repository
    /// Description: Mediation between domain and data mapping layers using a collection as 
    /// an interface for accessing domain objects. (Martin Fowler)
    /// Learn more: http://martinfowler.com/eaaCatalog/repository.html
    /// </summary>
    /// <typeparam name="T">Represents an entity</typeparam>
    public interface IRepositoryAsync<T> : IQueryAsync<T>, ICommandAsync<T>, IQueryRelationAsync<T>
        where T : IEntityBase
    {
    }
}
