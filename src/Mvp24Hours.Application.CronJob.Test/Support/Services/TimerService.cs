using System;
using System.Collections.Generic;

namespace Mvp24Hours.Application.CronJob.Test.Support.Services
{
    public class TimerService
    {
        public DateTime StartTime { get; set; }
        public int CurrentCounter { get; set; } = 0; 
        public Dictionary<int, DateTime> Counters { get; set; } = new Dictionary<int, DateTime>();

        public void Start()
        {
            StartTime = DateTime.Now;
        }

        public void CountTime()
        {
            CurrentCounter++;
            Counters.Add(CurrentCounter, DateTime.Now);
        }

        public int GetTimeAvgBetweenCounters()
        {
            var countersSize = Counters.Count;
            var countersTimeSum = 0;
            var lastDate = DateTime.MinValue;
            
            foreach (var counter in Counters)
            {
                if (countersTimeSum == 0)
                {
                    countersTimeSum = (StartTime - counter.Value).Seconds;
                    lastDate = counter.Value;
                    continue;
                }

                countersTimeSum += (lastDate - counter.Value).Seconds;
                lastDate = counter.Value;
            }

            double timeDiffInMinutes = countersTimeSum / 60;
            var avg = (int) Math.Ceiling(timeDiffInMinutes / countersSize);

            return avg;
        }
    }
}
