//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Core.Helpers;

/// <summary>
/// Framework-wide default constants.
/// </summary>
public static class ConstantsHelper
{
    /// <summary>
    /// Data access defaults.
    /// </summary>
    public static class Data
    {
        /// <summary>
        /// Default page size applied when the paging criteria does not set a limit.
        /// This is a default, not a cap: <c>EFCoreRepositoryOptions.MaxQtyByQueryPage</c> /
        /// <c>MongoDbRepositoryOptions.MaxQtyByQueryPage</c> override it per provider.
        /// </summary>
        public const int MaxQtyByQueryPage = 300;
    }
}
