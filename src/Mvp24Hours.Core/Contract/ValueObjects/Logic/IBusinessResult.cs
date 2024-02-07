//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Generic;

namespace Mvp24Hours.Core.Contract.ValueObjects.Logic
{
    /// <summary>
    /// Business object used to encapsulate response to requests
    /// </summary>
    public interface IBusinessResult<T>
    {
        /// <summary>
        /// Encapsulated model data list
        /// </summary>
        T Data { get; }
        /// <summary>
        /// Business messages for user feedback
        /// </summary>
        IReadOnlyCollection<IMessageResult> Messages { get; }
        /// <summary>
        /// Indicates if you have an error message
        /// </summary>
        bool HasErrors { get; }
        /// <summary>
        /// Transaction reference token
        /// </summary>
        string Token { get; }
        /// <summary>
        /// Defines token only if token is empty or null
        /// </summary>
        void SetToken(string token);
    }
}
