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
public class OperationActionAsync(Action<IPipelineMessage> action, Action<IPipelineMessage>? rollbackAction = null, bool isRequired = false) : IOperationAsync
{
    private readonly Action<IPipelineMessage> _action = action;
    private readonly Action<IPipelineMessage>? _rollbackAction = rollbackAction;
    private readonly bool _isRequired = isRequired;

    public virtual bool IsRequired => _isRequired;

    public OperationActionAsync(Action<IPipelineMessage> action, bool isRequired)
        : this(action, null, isRequired)
    {
    }

    public virtual async Task ExecuteAsync(IPipelineMessage input)
    {
        _action?.Invoke(input);
        await Task.CompletedTask;
    }

    public virtual async Task RollbackAsync(IPipelineMessage input)
    {
        _rollbackAction?.Invoke(input);
        await Task.CompletedTask;
    }
}
