//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Extensions
{
    public static class GuidExtensions
    {
        public static Guid SafeNewGuid(this Guid oid)
        {
            return oid == Guid.Empty ? Guid.NewGuid() : oid;
        }

        public static Guid ToGuid(this string oid)
        {
            return Guid.TryParse(oid, out Guid id) ? id : Guid.Empty;
        }

        public static bool IsValidGuid(this string oid)
        {
            if (string.IsNullOrEmpty(oid))
            {
                return false;
            }

            return Guid.TryParse(oid, out _);
        }

        public static bool IsNullOrEmpty(this Guid? oid)
        {
            return (oid == null || oid == Guid.Empty);
        }

        public static bool IsEmpty(this Guid oid)
        {
            return (oid == Guid.Empty);
        }
    }
}
