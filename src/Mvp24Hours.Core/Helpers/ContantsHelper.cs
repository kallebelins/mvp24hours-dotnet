//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Core.Helpers;

/// <summary>
/// Obsolete shim kept for backward compatibility. Use <see cref="ConstantsHelper"/>.
/// </summary>
[Obsolete("Renamed to ConstantsHelper (typo fix). Will be removed in v12.")]
public static class ContantsHelper
{
    /// <summary>
    /// Obsolete shim kept for backward compatibility. Use <see cref="ConstantsHelper.Data"/>.
    /// </summary>
    [Obsolete("Renamed to ConstantsHelper.Data. Will be removed in v12.")]
    public static class Data
    {
        /// <summary>
        /// Obsolete shim kept for backward compatibility.
        /// Use <see cref="ConstantsHelper.Data.MaxQtyByQueryPage"/>.
        /// </summary>
        public const int MaxQtyByQueryPage = ConstantsHelper.Data.MaxQtyByQueryPage;
    }
}
