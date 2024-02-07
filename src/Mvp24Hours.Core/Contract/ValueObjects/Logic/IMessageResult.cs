//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Enums;

namespace Mvp24Hours.Core.Contract.ValueObjects.Logic
{
    /// <summary>
    /// Reply message template
    /// </summary>
    public interface IMessageResult
    {
        /// <summary>
        /// Reference key
        /// </summary>
        string Key { get; }
        /// <summary>
        /// Message to user
        /// </summary>
        string Message { get; }
        /// <summary>
        /// User feedback type categorized
        /// </summary>
        MessageType Type { get; }
        /// <summary>
        /// Customized feedback message
        /// </summary>
        string CustomType { get; }
    }
}
