//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Data
{
    /// <summary>
    /// Contract for projection / query commands and data manipulation.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase")]
    public interface ISQLAsync
    {
        /// <summary>
        /// Execute command for data projection - query - in the database.
        /// </summary>
        /// <typeparam name="T">Entity type that contains properties that match the projected columns.</typeparam>
        /// <param name="sqlQuery">
        /// Dialect Query
        /// <example>
        /// SELECT ID, Name FROM dbo.[Client] WHERE ID = @id
        /// </example>
        /// </param>
        /// <param name="param">Parameter vector</param>
        /// <returns>
        /// List with items projected in the query
        /// </returns>
        Task<IEnumerable<T>> QueryAsync<T>(string sqlQuery, object param = null, int? commandTimeout = null, CommandType? commandType = null);
        /// <summary>
        /// Execute command for data projection - query - in the database.
        /// </summary>
        /// <typeparam name="T">Entity type that contains properties that match the projected columns.</typeparam>
        /// <param name="sqlQuery">
        /// Dialect Query
        /// <example>
        /// SELECT ID, Name FROM dbo.[Client] WHERE ID = @id
        /// </example>
        /// </param>
        /// <param name="param">Parameter vector</param>
        /// <returns>
        /// First item projected in the query
        /// </returns>
        Task<T> QueryFirstAsync<T>(string sqlQuery, object param = null, int? commandTimeout = null, CommandType? commandType = null);
        /// <summary>
        /// Execute command for data projection - query - in the database.
        /// </summary>
        /// <typeparam name="T">Entity type that contains properties that match the projected columns.</typeparam>
        /// <param name="sqlQuery">
        /// Dialect Query
        /// <example>
        /// SELECT ID, Name FROM dbo.[Client] WHERE ID = @id
        /// </example>
        /// </param>
        /// <param name="param">Parameter vector</param>
        /// <returns>
        /// First item projected in the query
        /// </returns>
        Task<T> QueryFirstOrDefaultAsync<T>(string sqlQuery, object param = null, int? commandTimeout = null, CommandType? commandType = null);
        /// <summary>
        /// Execute command for data projection - query - in the database.
        /// </summary>
        /// <typeparam name="T">Entity type that contains properties that match the projected columns.</typeparam>
        /// <param name="sqlQuery">
        /// Dialect Query
        /// <example>
        /// SELECT ID, Name FROM dbo.[Client] WHERE ID = @id
        /// </example>
        /// </param>
        /// <param name="param">Parameter vector</param>
        /// <returns>
        /// Single item projected in the query
        /// </returns>
        Task<T> QuerySingleAsync<T>(string sqlQuery, object param = null, int? commandTimeout = null, CommandType? commandType = null);
        /// <summary>
        /// Execute command for data projection - query - in the database.
        /// </summary>
        /// <typeparam name="T">Entity type that contains properties that match the projected columns.</typeparam>
        /// <param name="sqlQuery">
        /// Dialect Query
        /// <example>
        /// SELECT ID, Name FROM dbo.[Client] WHERE ID = @id
        /// </example>
        /// </param>
        /// <param name="param">Parameter vector</param>
        /// <returns>
        /// Single item projected in the query
        /// </returns>
        Task<T> QuerySingleOrDefaultAsync<T>(string sqlQuery, object param = null, int? commandTimeout = null, CommandType? commandType = null);
        /// <summary>
        /// Execute command for data projection - query - in the database.
        /// </summary>
        /// <typeparam name="T">Entity type that contains properties that match the projected columns.</typeparam>
        /// <param name="sqlQuery">
        /// Dialect Query
        /// <example>
        /// SELECT ID FROM dbo.[Client] WHERE ID = @id
        /// </example>
        /// </param>
        /// <param name="param">Parameter vector</param>
        /// <returns>
        /// Item projected in the query
        /// </returns>
        Task<T> ExecuteScalarAsync<T>(string sqlQuery, object param = null, int? commandTimeout = null, CommandType? commandType = null);
        /// <summary>
        /// Execute arbitrary commands in the database.
        /// </summary>
        /// <param name="sqlCommand">
        /// Command to execute
        /// <example>
        /// UPDATE dbo.[Customers] SET Name = \"New Name\" WHERE ID = @id
        /// </example>
        ///</param>
        /// <param name="param">Parameter vector</param>
        /// <returns>Number of records affected</returns>
        Task<int> ExecuteAsync(string sqlCommand, object param = null, int? commandTimeout = null, CommandType? commandType = null);
    }
}
