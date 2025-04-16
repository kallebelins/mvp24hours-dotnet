//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using System;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace Mvp24Hours.Infrastructure.Pipe.Operations
{
    /// <summary>  
    /// Action operation
    /// </summary>
    public class OperationAction : IOperation
    {
        private readonly Action<IPipelineMessage> _action;
        private readonly Action<IPipelineMessage> _rollbackAction;
        private readonly bool _isRequired;

        public bool IsRequired => _isRequired;

        public OperationAction(Action<IPipelineMessage> action, Action<IPipelineMessage> rollbackAction = default, bool isRequired = false)
        {
            _action = action;
            _rollbackAction = rollbackAction;
            _isRequired = isRequired;
        }

        public OperationAction(Action<IPipelineMessage> action, bool isRequired)
            : this(action, default, isRequired)
        {
        }

        public virtual void Execute(IPipelineMessage input)
        {
            _action?.Invoke(input);
        }

        public virtual void Rollback(IPipelineMessage input)
        {
            this._rollbackAction?.Invoke(input);
        }
    }
}
