//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;

namespace Mvp24Hours.Infrastructure.Pipe.Operations.Custom
{
    /// <summary>  
    /// Abstraction for conditional operation
    /// </summary>
    public abstract class OperationConditional : OperationBase
    {
        public override void Execute(IPipelineMessage input)
        {
            if (Condition(input))
            {
                TrueResult(input);
            }
            else
            {
                FalseResult(input);
            }
        }

        public abstract bool Condition(IPipelineMessage input);
        public virtual void TrueResult(IPipelineMessage input) { }
        public virtual void FalseResult(IPipelineMessage input) { }
    }
}
