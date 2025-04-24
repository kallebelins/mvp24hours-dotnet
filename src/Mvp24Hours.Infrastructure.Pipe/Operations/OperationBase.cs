//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Operations
{
    /// <summary>  
    /// Abstraction of base operations
    /// </summary>
    public abstract class OperationBase : IOperation
    {
        #region [ Properties / Fields ]
        public virtual bool IsRequired => false;
        #endregion

        #region [ Methods ]
        public abstract void Execute(IPipelineMessage input);
        public virtual void Rollback(IPipelineMessage input) { }
        #endregion
    }
}
