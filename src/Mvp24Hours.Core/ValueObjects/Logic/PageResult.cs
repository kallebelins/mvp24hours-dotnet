//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Mvp24Hours.Core.ValueObjects.Logic
{
    /// <summary>
    /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IPageResult"/>
    /// </summary>
    [DataContract, Serializable]
    public class PageResult(int limit, int offset, int count) : BaseVO, IPageResult
    {
        #region [ Properties ]
        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IPageResult.Limit"/>
        /// </summary>
        [DataMember]
        public int Limit { get; } = limit;
        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IPageResult.Offset"/>
        /// </summary>
        [DataMember]
        public int Offset { get; } = offset;
        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IPageResult.Count"/>
        /// </summary>
        [DataMember]
        public int Count { get; } = count;
        #endregion

        #region [ Methods ]
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Limit;
            yield return Offset;
            yield return Count;
        }
        #endregion
    }
}
