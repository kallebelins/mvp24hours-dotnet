//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class BusinessEventExtensions
    {
        /// <summary>
        /// Encapsulates object for business event
        /// </summary>
        public static IBusinessEvent ToBusinessEvent(this object value, string tokenDefault = null)
        {
            return new BusinessEvent(
                data: value,
                token: tokenDefault
            );
        }
    }
}
