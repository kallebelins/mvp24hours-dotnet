//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Core.Contract.ValueObjects.Logic
{
    /// <summary>
    /// Business object for exchanging messages between brokers
    /// </summary>
    public interface IBusinessEvent
    {
        /// <summary>
        /// Object creation date
        /// </summary>
        DateTime Created { get; set; }
        /// <summary>
        /// Encapsulated model data
        /// </summary>
        string Data { get; set; }
        /// <summary>
        /// Mapped data type
        /// </summary>
        Type DataType { get; set; }
        /// <summary>
        /// Transaction reference token
        /// </summary>
        string Token { get; set; }
        /// <summary>
        /// Get the original data
        /// </summary>
        object GetDataObject();
    }
}
