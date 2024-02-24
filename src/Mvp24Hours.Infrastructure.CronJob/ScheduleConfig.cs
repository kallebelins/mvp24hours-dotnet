using Mvp24Hours.Infrastructure.CronJob.Interfaces;
using System;

namespace Mvp24Hours.Infrastructure.CronJob
{
    public class ScheduleConfig<T> : IScheduleConfig<T>
    {
        public string CronExpression { get; set; }
        public TimeZoneInfo TimeZoneInfo { get; set; }
    }
}
