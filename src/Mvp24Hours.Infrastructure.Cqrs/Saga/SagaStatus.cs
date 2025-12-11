//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Saga;

/// <summary>
/// Represents the current status of a saga.
/// </summary>
public enum SagaStatus
{
    /// <summary>
    /// The saga has been created but not yet started.
    /// </summary>
    NotStarted = 0,

    /// <summary>
    /// The saga is currently executing its steps.
    /// </summary>
    Running = 1,

    /// <summary>
    /// The saga has completed all steps successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// The saga has failed and needs compensation.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// The saga is currently executing compensation steps.
    /// </summary>
    Compensating = 4,

    /// <summary>
    /// The saga has successfully completed all compensation steps.
    /// </summary>
    Compensated = 5,

    /// <summary>
    /// The saga has been partially compensated (some compensation steps failed).
    /// </summary>
    PartiallyCompensated = 6,

    /// <summary>
    /// The saga has timed out.
    /// </summary>
    TimedOut = 7,

    /// <summary>
    /// The saga has been explicitly cancelled.
    /// </summary>
    Cancelled = 8,

    /// <summary>
    /// The saga is suspended and waiting for external input.
    /// </summary>
    Suspended = 9
}

