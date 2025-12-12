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
    /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IPagingCriteria"/>
    /// </summary>
    [DataContract, Serializable]
    public class PagingCriteria(
        int limit,
        int offset,
        IReadOnlyCollection<string>? orderBy = null,
        IReadOnlyCollection<string>? navigation = null) : BaseVO, IPagingCriteria
    {
        #region [ Properties ]
        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IPagingCriteria.Limit"/>
        /// </summary>
        [DataMember]
        public int Limit { get; } = limit;

        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IPagingCriteria.Offset"/>
        /// </summary>
        [DataMember]
        public int Offset { get; } = offset;

        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IPagingCriteria.OrderBy"/>
        /// </summary>
        [DataMember]
        public IReadOnlyCollection<string>? OrderBy { get; } = orderBy;

        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IPagingCriteria.Navigation"/>
        /// </summary>
        [DataMember]
        public IReadOnlyCollection<string>? Navigation { get; } = navigation;
        #endregion

        #region [ Methods ]
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Limit;
            yield return Offset;
            yield return OrderBy!;
            yield return Navigation!;
        }
        #endregion
    }
}
