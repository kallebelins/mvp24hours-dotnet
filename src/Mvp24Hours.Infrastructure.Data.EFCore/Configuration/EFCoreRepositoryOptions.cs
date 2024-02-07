//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Helpers;
using System;
using System.Transactions;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Configuration
{
    [Serializable]
    public sealed class EFCoreRepositoryOptions
    {
        public int MaxQtyByQueryPage { get; set; } = ContantsHelper.Data.MaxQtyByQueryPage;
        public IsolationLevel? TransactionIsolationLevel { get; set; }
    }
}
