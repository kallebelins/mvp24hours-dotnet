//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using AutoMapper;

namespace Mvp24Hours.Core.Contract.Mappings
{
    /// <summary>
    /// Dynamic service mapping contract
    /// </summary>
    public interface IMapFrom
    {
        /// <summary>
        /// Apply mapping
        /// </summary>
        void Mapping(Profile profile);
    }
}
