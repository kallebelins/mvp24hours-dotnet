//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Infrastructure.Pipe
{
    /// <summary>  
    /// Operations interface
    /// </summary>
    public interface IOperation
    {
        /// <summary>  
        /// Perform an operation
        /// </summary>
        void Execute(IPipelineMessage input);

        /// <summary>
        /// Perform rollback when an exception is thrown or a error occured
        /// </summary>        
        void Rollback(IPipelineMessage input);

        /// <summary>
        /// Indicates whether operation is mandatory (even with failure)
        /// </summary>
        public bool IsRequired { get; }
    }
}
