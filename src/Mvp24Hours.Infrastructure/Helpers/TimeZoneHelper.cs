//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.Helpers
{
    /// <summary>
    /// Contains timezone functions
    /// </summary>
    public static class TimeZoneHelper
    {
        public static List<string> TimeZoneIds { get; set; } = new List<string>()
        {
            { "E. South America Standard Time" },
            { "Brazil/East" },
            { "America/Sao_Paulo" }
        };

        /// <summary>
        /// Get current date and time based on South America time zone
        /// </summary>
        public static DateTime GetTimeZoneNow()
        {
            return TimeZoneInfo.ConvertTime(DateTime.UtcNow, GetTimeZoneInfo());
        }

        /// <summary>
        /// Get date and time based on South America time zone
        /// </summary>
        public static DateTime GetTimeZone(DateTime utcDateTime, DateTimeKind? kind)
        {
            var dUtc = (kind ?? utcDateTime.Kind) switch
            {
                DateTimeKind.Utc => utcDateTime,
                DateTimeKind.Local => utcDateTime.ToUniversalTime(),
                _ => DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc),
            };
            return TimeZoneInfo.ConvertTime(dUtc, GetTimeZoneInfo());
        }

        private static TimeZoneInfo _timeZoneInfo;

        private static TimeZoneInfo GetTimeZoneInfo()
        {
            if (_timeZoneInfo != null)
            {
                return _timeZoneInfo;
            }

            _timeZoneInfo = TimeZoneInfo.GetSystemTimeZones()
                .FirstOrDefault(x => TimeZoneIds.Contains(x.Id));

            _timeZoneInfo ??= TimeZoneInfo.Local;

            return _timeZoneInfo;
        }
    }
}
