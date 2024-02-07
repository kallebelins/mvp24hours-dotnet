//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using System;

namespace Mvp24Hours.Infrastructure.Pipe.Operations
{
    /// <summary>  
    /// Action operation
    /// </summary>
    public class OperationAction(Action<IPipelineMessage> action, bool isRequired = false) : IOperation
    {
        public virtual bool IsRequired => isRequired;

        public virtual void Execute(IPipelineMessage input)
        {
            action?.Invoke(input);
        }
    }
}
