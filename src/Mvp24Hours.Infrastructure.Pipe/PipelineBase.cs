//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;

namespace Mvp24Hours.Infrastructure.Pipe;

/// <summary>
/// Defines pipeline engine base
/// </summary>
public abstract class PipelineBase(bool isBreakOnFail, bool forceRollbackOnFalure)
{
    #region [ Ctor ]
    protected PipelineBase()
        : this(false, false)
    {
    }
    #endregion

    #region [ Fields / Properties ]
    protected bool IsBreakOnFail { get; set; } = isBreakOnFail;
    public bool AllowPropagateException { get; set; }
    public bool ForceRollbackOnFalure { get; set; } = forceRollbackOnFalure;
    protected IPipelineMessage Message { get; set; } = new PipelineMessage();
    #endregion

    #region [ Methods ]

    public IPipelineMessage GetMessage()
    {
        return Message;
    }

    #endregion
}
