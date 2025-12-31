//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Threading;

namespace Mvp24Hours.Infrastructure.CronJob.Context
{
    /// <summary>
    /// Interface for accessing the current CronJob execution context.
    /// </summary>
    public interface ICronJobContextAccessor
    {
        /// <summary>
        /// Gets the current CronJob execution context.
        /// Returns null if not in a CronJob execution context.
        /// </summary>
        ICronJobContext? Context { get; }
    }

    /// <summary>
    /// Default implementation of <see cref="ICronJobContextAccessor"/> using AsyncLocal storage.
    /// Thread-safe and async-aware context storage.
    /// </summary>
    public sealed class CronJobContextAccessor : ICronJobContextAccessor
    {
        private static readonly AsyncLocal<CronJobContextHolder> _contextCurrent = new();

        /// <inheritdoc />
        public ICronJobContext? Context
        {
            get => _contextCurrent.Value?.Context;
            internal set
            {
                var holder = _contextCurrent.Value;
                if (holder != null)
                {
                    holder.Context = null;
                }

                if (value != null)
                {
                    _contextCurrent.Value = new CronJobContextHolder { Context = value };
                }
            }
        }

        /// <summary>
        /// Sets the current context. For internal use only.
        /// </summary>
        internal static void SetContext(ICronJobContext? context)
        {
            var holder = _contextCurrent.Value;
            if (holder != null)
            {
                holder.Context = null;
            }

            if (context != null)
            {
                _contextCurrent.Value = new CronJobContextHolder { Context = context };
            }
        }

        /// <summary>
        /// Gets the current context statically. For internal use only.
        /// </summary>
        internal static ICronJobContext? GetContext() => _contextCurrent.Value?.Context;

        /// <summary>
        /// Clears the current context. For internal use only.
        /// </summary>
        internal static void ClearContext()
        {
            var holder = _contextCurrent.Value;
            if (holder != null)
            {
                holder.Context = null;
            }
        }

        private sealed class CronJobContextHolder
        {
            public ICronJobContext? Context { get; set; }
        }
    }
}

