//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using System;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Operations
{
    /// <summary>  
    /// Action operation
    /// </summary>
    public class OperationActionAsync(Action<IPipelineMessage> action, bool isRequired = false) : IOperationAsync
    {
        private readonly Action<IPipelineMessage> _action = action;
        private readonly bool _isRequired = isRequired;

        public virtual bool IsRequired => this._isRequired;

        public virtual async Task ExecuteAsync(IPipelineMessage input)
        {
            this._action?.Invoke(input);
            await Task.CompletedTask;
        }
    }
}
