//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;

namespace Mvp24Hours.Infrastructure.Pipe.Operations;

/// <summary>  
/// Action operation
/// </summary>
public class OperationAction(Action<IPipelineMessage> action, Action<IPipelineMessage>? rollbackAction = null, bool isRequired = false) : IOperation
{
    private readonly Action<IPipelineMessage> _action = action;
    private readonly Action<IPipelineMessage>? _rollbackAction = rollbackAction;

    public bool IsRequired { get; } = isRequired;

    public OperationAction(Action<IPipelineMessage> action, bool isRequired)
        : this(action, null, isRequired)
    {
    }

    public virtual void Execute(IPipelineMessage input)
    {
        _action?.Invoke(input);
    }

    public virtual void Rollback(IPipelineMessage input)
    {
        _rollbackAction?.Invoke(input);
    }
}
