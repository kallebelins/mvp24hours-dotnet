//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Mvp24Hours.Extensions
{
    public static partial class ConvertExtensions
    {

        public static string ToBase64(this string text)
        {
            return ToBase64(text, Encoding.UTF8);
        }

        public static string ToBase64(this string text, Encoding encoding)
        {
            byte[] textAsBytes = encoding.GetBytes(text);
            return Convert.ToBase64String(textAsBytes);
        }

        public static string From64(this string text)
        {
            return From64(text, Encoding.UTF8);
        }

        public static string From64(this string text, Encoding encoding)
        {
            byte[] textAsBytes = Convert.FromBase64String(text);
            return encoding.GetString(textAsBytes);

        }

        public static T ToEnum<T>(this string value, T defaultValue = default) where T : Enum
        {
            return Enum.TryParse(typeof(T), value, true, out object result) ? (T)result : defaultValue;
        }

        public static int? ToInt(this string value, int? defaultValue = null)
        {
            if (!value.HasValue())
            {
                return defaultValue;
            }

            return int.TryParse(value, out int result) ? result : defaultValue;
        }

        public static long? ToLong(this string value, long? defaultValue = null)
        {
            if (!value.HasValue())
            {
                return defaultValue;
            }

            return long.TryParse(value, out long result) ? result : defaultValue;
        }

        public static bool? ToBoolean(this string value, bool? defaultValue = null)
        {
            if (!value.HasValue())
            {
                return defaultValue;
            }

            return bool.TryParse(value, out bool result) ? result : defaultValue;
        }

        public static decimal? ToDecimal(this string value, decimal? defaultValue = null)
        {
            if (!value.HasValue())
            {
                return defaultValue;
            }

            return decimal.TryParse(value, out decimal result) ? result : defaultValue;
        }

        public static DateTime? ToDateTime(this string value, DateTime? defaultValue = null, CultureInfo info = null)
        {
            if (!value.HasValue())
            {
                return defaultValue;
            }

            return DateTime.TryParse(value, info ?? new CultureInfo("en-US"), out DateTime result) ? result : defaultValue;
        }

        public static string NullSafe(this string target)
        {
            return (target ?? string.Empty).Trim();
        }

        public static string Replace(this string target, List<string> oldValues, string newValue)
        {
            oldValues.ForEach(oldValue => target = target.Replace(oldValue, newValue));
            return target;
        }

        public static string OnlyNumbers(this string str)
        {
            return OnlyNumbersRegex().Replace(str.NullSafe(), "");
        }

        public static string OnlyNumbersLetters(this string str)
        {
            return OnlyNumbersLettersRegex().Replace(str.NullSafe(), "");
        }

        public static string OnlyLetters(this string str)
        {
            return OnlyLettersRegex().Replace(str.NullSafe(), "");
        }

        public static string RemoveDiacritics(this string input)
        {
            string stFormD = input.NullSafe().Normalize(NormalizationForm.FormD);
            int len = stFormD.Length;
            StringBuilder sb = new();
            for (int i = 0; i < len; i++)
            {
                System.Globalization.UnicodeCategory uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(stFormD[i]);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(stFormD[i]);
                }
            }
            return (sb.ToString().Normalize(NormalizationForm.FormC));
        }

        public static string ReplaceSpecialChar(this string target)
        {
            return target.NullSafe().RemoveDiacritics();
        }

        public static string GetSHA256Hash(this string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str.NullSafe());
            byte[] hash = SHA256.HashData(bytes);
            return Convert.ToBase64String(hash);
        }

        public static string GetSHA512Hash(this string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str.NullSafe());
            byte[] hash = SHA512.HashData(bytes);
            return Convert.ToBase64String(hash);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Info Code Smell", "S1133:Deprecated code should be removed", Justification = "Maintain library evolution reference.")]
        [Obsolete("Use the GetSHA512Hash method.")]
        public static string GetHash(string str)
        {
            return GetSHA512Hash(str);
        }

        public static string ZipBase64(this string str)
        {
            return System.Convert.ToBase64String(str.NullSafe().ZipByte());
        }

        public static byte[] ZipByte(this string str)
        {
            using MemoryStream memory = new();
            DeflateStream gzip = new(memory, CompressionMode.Compress);
            using (StreamWriter writer = new(gzip, System.Text.Encoding.UTF8))
            {
                writer.Write(str.NullSafe());
            }
            byte[] btComp = memory.ToArray();
            return btComp;
        }

        public static string UnZipBase64(this string str)
        {
            byte[] bty = System.Convert.FromBase64String(str.NullSafe());
            return bty.UnZip();
        }

        public static string UnZip(this byte[] bty)
        {
            using MemoryStream inputStream = new(bty);
            DeflateStream gzip = new(inputStream, CompressionMode.Decompress);
            using StreamReader reader = new(gzip, System.Text.Encoding.UTF8);
            return reader.ReadToEnd();
        }

        [GeneratedRegex(@"[^\d]*")]
        private static partial Regex OnlyNumbersRegex();

        [GeneratedRegex(@"[^\dA-Za-z]*")]
        private static partial Regex OnlyNumbersLettersRegex();

        [GeneratedRegex(@"[^A-Za-z]*")]
        private static partial Regex OnlyLettersRegex();
    }
}
