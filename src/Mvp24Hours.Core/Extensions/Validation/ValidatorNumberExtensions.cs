//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
namespace Mvp24Hours.Extensions
{
    public static class ValidatorNumberExtensions
    {
        public static T? IsNullOrDefault<T>(this T? value)
            where T : struct
        {
            return value ?? default;
        }

        public static T? IsNullOrDefault<T>(this T? value, T? valueDefault)
            where T : struct => value ?? valueDefault;

        public static byte IsZeroOrDefault(this byte value, byte valueDefault)
        {
            return value == 0 ? value : valueDefault;
        }

        public static decimal IsZeroOrDefault(this decimal value, decimal valueDefault)
        {
            return value == 0 ? value : valueDefault;
        }

        public static double IsZeroOrDefault(this double value, double valueDefault)
        {
            return value == 0 ? value : valueDefault;
        }

        public static short IsZeroOrDefault(this short value, short valueDefault)
        {
            return value == 0 ? value : valueDefault;
        }

        public static int IsZeroOrDefault(this int value, int valueDefault)
        {
            return value == 0 ? value : valueDefault;
        }

        public static long IsZeroOrDefault(this long value, long valueDefault)
        {
            return value == 0 ? value : valueDefault;
        }

        public static sbyte IsZeroOrDefault(this sbyte value, sbyte valueDefault)
        {
            return value == 0 ? value : valueDefault;
        }

        public static float IsZeroOrDefault(this float value, float valueDefault)
        {
            return value == 0 ? value : valueDefault;
        }

        public static ushort IsZeroOrDefault(this ushort value, ushort valueDefault)
        {
            return value == 0 ? value : valueDefault;
        }

        public static uint IsZeroOrDefault(this uint value, uint valueDefault)
        {
            return value == 0 ? value : valueDefault;
        }

        public static ulong IsZeroOrDefault(this ulong value, ulong valueDefault)
        {
            return value == 0 ? value : valueDefault;
        }

        public static byte? IsZeroOrDefault(this byte value, byte? valueDefault)
        {
            return value == 0 ? value : valueDefault;
        }

        public static decimal? IsZeroOrDefault(this decimal value, decimal? valueDefault)
        {
            return value == 0 ? value : valueDefault;
        }

        public static double? IsZeroOrDefault(this double value, double? valueDefault)
        {
            return value == 0 ? value : valueDefault;
        }

        public static short? IsZeroOrDefault(this short value, short? valueDefault)
        {
            return value == 0 ? value : valueDefault;
        }

        public static int? IsZeroOrDefault(this int value, int? valueDefault)
        {
            return value == 0 ? value : valueDefault;
        }

        public static long? IsZeroOrDefault(this long value, long? valueDefault)
        {
            return value == 0 ? value : valueDefault;
        }

        public static sbyte? IsZeroOrDefault(this sbyte value, sbyte? valueDefault)
        {
            return value == 0 ? value : valueDefault;
        }

        public static float? IsZeroOrDefault(this float value, float? valueDefault)
        {
            return value == 0 ? value : valueDefault;
        }

        public static ushort? IsZeroOrDefault(this ushort value, ushort? valueDefault)
        {
            return value == 0 ? value : valueDefault;
        }

        public static uint? IsZeroOrDefault(this uint value, uint? valueDefault)
        {
            return value == 0 ? value : valueDefault;
        }

        public static ulong? IsZeroOrDefault(this ulong value, ulong? valueDefault)
        {
            return value == 0 ? value : valueDefault;
        }
    }
}
