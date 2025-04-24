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
    public interface IOperationAsync
    {
        /// <summary>  
        /// Perform an operation
        /// </summary>
        Task ExecuteAsync(IPipelineMessage input);
        /// <summary>
        /// Perform rollback when an exception is thrown or a error occured
        /// </summary>        
        Task RollbackAsync(IPipelineMessage input);
        /// <summary>
        /// Indicates whether operation is mandatory (even with failure)
        /// </summary>
        public bool IsRequired { get; }

    }
}
