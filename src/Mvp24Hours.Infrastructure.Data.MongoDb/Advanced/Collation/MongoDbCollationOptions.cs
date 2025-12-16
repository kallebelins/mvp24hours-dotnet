//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Driver;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.Collation
{
    /// <summary>
    /// Collation options for MongoDB queries supporting locale-aware string comparison.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Collation allows users to specify language-specific rules for string comparison,
    /// such as rules for lettercase and accent marks. Common use cases:
    /// <list type="bullet">
    ///   <item>Case-insensitive sorting and searching</item>
    ///   <item>Accent-insensitive comparisons</item>
    ///   <item>Language-specific sort order (e.g., ç in Portuguese)</item>
    ///   <item>Numeric string sorting (e.g., "2" before "10")</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class MongoDbCollationOptions
    {
        /// <summary>
        /// Gets or sets the locale.
        /// </summary>
        /// <remarks>
        /// Common locales: "en" (English), "pt" (Portuguese), "es" (Spanish),
        /// "fr" (French), "de" (German), "simple" (simple binary comparison).
        /// </remarks>
        public string Locale { get; set; } = "en";

        /// <summary>
        /// Gets or sets whether comparison is case-sensitive.
        /// </summary>
        /// <remarks>
        /// When false, "A" and "a" are considered equal.
        /// </remarks>
        public bool? CaseLevel { get; set; }

        /// <summary>
        /// Gets or sets the case ordering.
        /// </summary>
        /// <remarks>
        /// "upper" - uppercase letters sort before lowercase.
        /// "lower" - lowercase letters sort before uppercase.
        /// "off" - no specific case ordering.
        /// </remarks>
        public CollationCaseFirst CaseFirst { get; set; } = CollationCaseFirst.Off;

        /// <summary>
        /// Gets or sets the comparison strength.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        ///   <item>1 (Primary): Base character comparisons only (a vs b)</item>
        ///   <item>2 (Secondary): Includes diacritics (a vs á)</item>
        ///   <item>3 (Tertiary): Includes case (a vs A) - default</item>
        ///   <item>4 (Quaternary): Includes punctuation</item>
        ///   <item>5 (Identical): Includes everything</item>
        /// </list>
        /// </remarks>
        public CollationStrength Strength { get; set; } = CollationStrength.Tertiary;

        /// <summary>
        /// Gets or sets whether to treat numeric strings as numbers.
        /// </summary>
        /// <remarks>
        /// When true, "10" sorts after "2" (numeric ordering).
        /// When false, "10" sorts before "2" (string ordering).
        /// </remarks>
        public bool NumericOrdering { get; set; }

        /// <summary>
        /// Gets or sets the alternate handling for whitespace and punctuation.
        /// </summary>
        /// <remarks>
        /// "non-ignorable" - whitespace and punctuation are considered.
        /// "shifted" - whitespace and punctuation are considered at strength 4 only.
        /// </remarks>
        public CollationAlternate Alternate { get; set; } = CollationAlternate.NonIgnorable;

        /// <summary>
        /// Gets or sets the maximum variable characters.
        /// </summary>
        /// <remarks>
        /// Determines which characters are ignored at strength levels 1-3.
        /// "punct" - whitespace and punctuation are variable.
        /// "space" - whitespace only is variable.
        /// </remarks>
        public CollationMaxVariable MaxVariable { get; set; } = CollationMaxVariable.Punctuation;

        /// <summary>
        /// Gets or sets whether text is normalized.
        /// </summary>
        public bool? Normalization { get; set; }

        /// <summary>
        /// Gets or sets whether strings with diacritics sort from back of the string.
        /// </summary>
        /// <remarks>
        /// Useful for French language where diacritics at the end of words
        /// should be considered first for sorting.
        /// </remarks>
        public bool? Backwards { get; set; }

        /// <summary>
        /// Converts to MongoDB Collation object.
        /// </summary>
        public MongoDB.Driver.Collation ToCollation()
        {
            return new MongoDB.Driver.Collation(
                Locale,
                CaseLevel,
                CaseFirst,
                Strength,
                NumericOrdering,
                Alternate,
                MaxVariable,
                Normalization,
                Backwards);
        }
    }

    /// <summary>
    /// Predefined collation configurations for common scenarios.
    /// </summary>
    public static class CollationPresets
    {
        /// <summary>
        /// Case-insensitive collation for English.
        /// </summary>
        public static MongoDbCollationOptions EnglishCaseInsensitive => new()
        {
            Locale = "en",
            Strength = CollationStrength.Secondary
        };

        /// <summary>
        /// Case-insensitive collation for Portuguese.
        /// </summary>
        public static MongoDbCollationOptions PortugueseCaseInsensitive => new()
        {
            Locale = "pt",
            Strength = CollationStrength.Secondary
        };

        /// <summary>
        /// Case-insensitive collation for Spanish.
        /// </summary>
        public static MongoDbCollationOptions SpanishCaseInsensitive => new()
        {
            Locale = "es",
            Strength = CollationStrength.Secondary
        };

        /// <summary>
        /// Numeric string ordering (e.g., "2" before "10").
        /// </summary>
        public static MongoDbCollationOptions NumericOrdered => new()
        {
            Locale = "en",
            NumericOrdering = true
        };

        /// <summary>
        /// Case-insensitive with numeric ordering.
        /// </summary>
        public static MongoDbCollationOptions CaseInsensitiveNumeric => new()
        {
            Locale = "en",
            Strength = CollationStrength.Secondary,
            NumericOrdering = true
        };

        /// <summary>
        /// Simple binary comparison (fastest, no locale rules).
        /// </summary>
        public static MongoDbCollationOptions SimpleBinary => new()
        {
            Locale = "simple"
        };
    }
}

