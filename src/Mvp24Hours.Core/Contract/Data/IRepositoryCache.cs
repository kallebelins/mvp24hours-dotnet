//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Core.Contract.Data
{
    /// <summary>
    /// Design Pattern: Repository
    /// Description: Mediation between domain and data mapping layers using a collection as 
    /// an interface for accessing domain objects. (Martin Fowler)
    /// Learn more: http://martinfowler.com/eaaCatalog/repository.html
    /// </summary>
    /// <typeparam name="T">Represents an model</typeparam>
    public interface IRepositoryCache<T>
    {
        /// <summary>
        /// Get model by key
        /// </summary>
        T Get(string key);
        /// <summary>
        /// Get string by key
        /// </summary>
        string GetString(string key);
        /// <summary>
        /// Register model by key
        /// </summary>
        void Set(string key, T model);
        /// <summary>
        /// Register string by key
        /// </summary>
        void SetString(string key, string value);
        /// <summary>
        /// Remove model/string by key
        /// </summary>
        void Remove(string key);
    }
}
