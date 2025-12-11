using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Mvp24Hours.Infrastructure.CronJob.Test.Support.Services
{
    /// <summary>
    /// Helper service for tracking CronJob execution times during tests.
    /// </summary>
    public class TimerService
    {
        private readonly Stopwatch _stopwatch = new();
        
        /// <summary>
        /// Gets the list of recorded execution times.
        /// </summary>
        public List<TimeSpan> Counters { get; } = new();

        /// <summary>
        /// Starts the internal stopwatch.
        /// </summary>
        public void Start()
        {
            _stopwatch.Start();
        }

        /// <summary>
        /// Records the current elapsed time.
        /// </summary>
        public void CountTime()
        {
            Counters.Add(_stopwatch.Elapsed);
        }
    }
}

