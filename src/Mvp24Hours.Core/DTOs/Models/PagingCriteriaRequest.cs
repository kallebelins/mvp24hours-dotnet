//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Mvp24Hours.Core.DTOs.Models
{
    /// <summary>
    /// Represents a definition for search criteria on a page (DTO)
    /// </summary>
    [DataContract, Serializable]
    public class PagingCriteriaRequest
    {
        /// <summary>
        /// Limit items on the page
        /// </summary>
        [DataMember]
        public int Limit { get; set; }
        /// <summary>
        /// Item block number or page number
        /// </summary>
        [DataMember]
        public int Offset { get; set; }
        /// <summary>
        /// Clause for sorting by field
        /// </summary>
        [DataMember]
        public List<string> OrderBy { get; set; }
        /// <summary>
        /// Related objects that will be loaded together
        /// </summary>
        [DataMember]
        public List<string> Navigation { get; set; }
    }
}
