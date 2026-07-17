//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading.Tasks;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;

namespace Mvp24Hours.Infrastructure.Pipe.Operations
{
    /// <summary>  
    /// Action operation
    /// </summary>
    public class OperationActionAsync : IOperationAsync
    {
        private readonly Action<IPipelineMessage> _action;
        private readonly Action<IPipelineMessage>? _rollbackAction;
        private readonly bool _isRequired;

        public virtual bool IsRequired => this._isRequired;

        public OperationActionAsync(Action<IPipelineMessage> action, Action<IPipelineMessage>? rollbackAction = null, bool isRequired = false)
        {
            _action = action;
            _rollbackAction = rollbackAction;
            _isRequired = isRequired;
        }

        public OperationActionAsync(Action<IPipelineMessage> action, bool isRequired)
            : this(action, null, isRequired)
        {
        }

        public virtual async Task ExecuteAsync(IPipelineMessage input)
        {
            this._action?.Invoke(input);
            await Task.CompletedTask;
        }

        public virtual async Task RollbackAsync(IPipelineMessage input)
        {
            this._rollbackAction?.Invoke(input);
            await Task.CompletedTask;
        }
    }
}
