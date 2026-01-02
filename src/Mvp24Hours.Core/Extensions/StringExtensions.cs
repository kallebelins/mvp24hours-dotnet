//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Text.RegularExpressions;

namespace Mvp24Hours.Extensions
{
    public static class StringExtensions
    {
        public static string RegexReplace(this string source, string pattern, string replacement)
        {
            return Regex.Replace(source, pattern, replacement);
        }

        public static string ReplaceEnd(this string source, string value, string replacement)
        {
            return RegexReplace(source, $"{value}$", replacement);
        }

        public static string RemoveEnd(this string source, string value)
        {
            return ReplaceEnd(source, value, string.Empty);
        }

        public static string Truncate(this string text, int size)
        {
            string value = text ?? string.Empty;
            return value.Length > size ? value[..size] : value;
        }

        public static string Reticence(this string text, int size)
        {
            string value = text ?? string.Empty;
            return value.Length > size ? value[..size] + "..." : value;
        }

        public static string SubstringSafe(this string text, int start, int length = int.MaxValue)
        {
            if (text == null)
            {
                return string.Empty;
            }

            if (start < 0)
            {
                start = 0;
            }

            if (start >= text.Length)
            {
                return string.Empty;
            }

            // Se length é int.MaxValue ou muito grande, retornar até o final da string
            if (length == int.MaxValue || start + length > text.Length)
            {
                length = text.Length - start;
            }

            return text.Substring(start, length);
        }

        public static string SqlSafe(this string text)
        {
            var safeText = text.NullSafe();
            
            // Remove SQL comments (everything from -- to end of string)
            int commentIndex = safeText.IndexOf("--");
            if (commentIndex >= 0)
            {
                safeText = safeText.Substring(0, commentIndex);
            }
            
            // Escape single quotes
            return safeText.Replace("'", "''");
        }

        public static string Format(this string text, params object[] args)
        {
            return string.Format(text, args);
        }
    }
}
