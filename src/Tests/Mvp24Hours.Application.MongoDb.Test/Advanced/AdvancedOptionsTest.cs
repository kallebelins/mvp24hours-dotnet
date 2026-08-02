//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Driver;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.CappedCollections;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.Collation;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.Sharding;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.TextSearch;
using Xunit;
using MongoDbTimeSeries = Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.TimeSeries;

namespace Mvp24Hours.Application.MongoDb.Test.Advanced;

[Trait("Category", "Unit")]
public class AdvancedOptionsTest
{
    #region [ MongoDbTextSearchOptions ]

    [Fact]
    public void MongoDbTextSearchOptions_DefaultValues_AreCorrect()
    {
        var opts = new MongoDbTextSearchOptions();

        Assert.Null(opts.Language);
        Assert.False(opts.CaseSensitive);
        Assert.False(opts.DiacriticSensitive);
        Assert.True(opts.IncludeScore);
        Assert.Null(opts.MinScore);
        Assert.Null(opts.Limit);
        Assert.Null(opts.Skip);
        Assert.True(opts.SortByScore);
    }

    [Fact]
    public void MongoDbTextSearchOptions_CanAssignAllProperties()
    {
        var opts = new MongoDbTextSearchOptions
        {
            Language = "portuguese",
            CaseSensitive = true,
            DiacriticSensitive = true,
            IncludeScore = false,
            MinScore = 0.5,
            Limit = 20,
            Skip = 5,
            SortByScore = false
        };

        Assert.Equal("portuguese", opts.Language);
        Assert.True(opts.CaseSensitive);
        Assert.True(opts.DiacriticSensitive);
        Assert.False(opts.IncludeScore);
        Assert.Equal(0.5, opts.MinScore);
        Assert.Equal(20, opts.Limit);
        Assert.Equal(5, opts.Skip);
        Assert.False(opts.SortByScore);
    }

    #endregion

    #region [ TextSearchResult ]

    [Fact]
    public void TextSearchResult_CanAssignDocumentAndScore()
    {
        var result = new TextSearchResult<string>
        {
            Document = "hello world",
            Score = 1.5
        };

        Assert.Equal("hello world", result.Document);
        Assert.Equal(1.5, result.Score);
    }

    [Fact]
    public void TextSearchResult_WithReferenceType_WorksCorrectly()
    {
        var obj = new { Name = "Test" };
        var result = new TextSearchResult<object> { Document = obj, Score = 2.0 };

        Assert.Equal(obj, result.Document);
        Assert.Equal(2.0, result.Score);
    }

    #endregion

    #region [ TimeSeriesOptions ]

    [Fact]
    public void TimeSeriesOptions_DefaultValues_AreCorrect()
    {
        var opts = new MongoDbTimeSeries.TimeSeriesOptions();

        Assert.Equal(string.Empty, opts.TimeField);
        Assert.Null(opts.MetaField);
        Assert.Equal("seconds", opts.Granularity);
        Assert.Null(opts.BucketMaxSpanSeconds);
        Assert.Null(opts.BucketRoundingSeconds);
        Assert.Null(opts.ExpireAfter);
    }

    [Fact]
    public void TimeSeriesOptions_CanAssignAllProperties()
    {
        var expiry = TimeSpan.FromDays(30);
        var opts = new MongoDbTimeSeries.TimeSeriesOptions
        {
            TimeField = "timestamp",
            MetaField = "sensorId",
            Granularity = "minutes",
            BucketMaxSpanSeconds = 3600,
            BucketRoundingSeconds = 60,
            ExpireAfter = expiry
        };

        Assert.Equal("timestamp", opts.TimeField);
        Assert.Equal("sensorId", opts.MetaField);
        Assert.Equal("minutes", opts.Granularity);
        Assert.Equal(3600, opts.BucketMaxSpanSeconds);
        Assert.Equal(60, opts.BucketRoundingSeconds);
        Assert.Equal(expiry, opts.ExpireAfter);
    }

    [Fact]
    public void TimeSeriesGranularity_Constants_HaveCorrectValues()
    {
        Assert.Equal("seconds", MongoDbTimeSeries.TimeSeriesGranularity.Seconds);
        Assert.Equal("minutes", MongoDbTimeSeries.TimeSeriesGranularity.Minutes);
        Assert.Equal("hours", MongoDbTimeSeries.TimeSeriesGranularity.Hours);
    }

    #endregion

    #region [ CappedCollectionOptions ]

    [Fact]
    public void CappedCollectionOptions_DefaultValues_AreCorrect()
    {
        var opts = new CappedCollectionOptions();

        Assert.Equal(0, opts.MaxSizeBytes);
        Assert.Null(opts.MaxDocuments);
        Assert.True(opts.AutoIndexId);
    }

    [Fact]
    public void CappedCollectionOptions_CanAssignProperties()
    {
        var opts = new CappedCollectionOptions
        {
            MaxSizeBytes = 104857600,
            MaxDocuments = 1000,
            AutoIndexId = false
        };

        Assert.Equal(104857600, opts.MaxSizeBytes);
        Assert.Equal(1000, opts.MaxDocuments);
        Assert.False(opts.AutoIndexId);
    }

    [Fact]
    public void CappedCollectionOptions_MaxDocuments_CanBeNull()
    {
        var opts = new CappedCollectionOptions { MaxSizeBytes = 1024 };
        Assert.Null(opts.MaxDocuments);
    }

    #endregion

    #region [ MongoDbCollationOptions ]

    [Fact]
    public void MongoDbCollationOptions_DefaultValues_AreCorrect()
    {
        var opts = new MongoDbCollationOptions();

        Assert.Equal("en", opts.Locale);
        Assert.Null(opts.CaseLevel);
        Assert.Equal(CollationCaseFirst.Off, opts.CaseFirst);
        Assert.Equal(CollationStrength.Tertiary, opts.Strength);
        Assert.False(opts.NumericOrdering);
        Assert.Equal(CollationAlternate.NonIgnorable, opts.Alternate);
        Assert.Equal(CollationMaxVariable.Punctuation, opts.MaxVariable);
        Assert.Null(opts.Normalization);
        Assert.Null(opts.Backwards);
    }

    [Fact]
    public void MongoDbCollationOptions_ToCollation_ReturnsMongoCollation()
    {
        var opts = new MongoDbCollationOptions
        {
            Locale = "pt",
            Strength = CollationStrength.Secondary
        };

        var collation = opts.ToCollation();

        Assert.NotNull(collation);
        Assert.Equal("pt", collation.Locale);
        Assert.Equal(CollationStrength.Secondary, collation.Strength);
    }

    [Fact]
    public void CollationPresets_EnglishCaseInsensitive_HasCorrectValues()
    {
        MongoDbCollationOptions preset = CollationPresets.EnglishCaseInsensitive;

        Assert.Equal("en", preset.Locale);
        Assert.Equal(CollationStrength.Secondary, preset.Strength);
    }

    [Fact]
    public void CollationPresets_PortugueseCaseInsensitive_HasCorrectLocale()
    {
        MongoDbCollationOptions preset = CollationPresets.PortugueseCaseInsensitive;
        Assert.Equal("pt", preset.Locale);
    }

    [Fact]
    public void CollationPresets_SpanishCaseInsensitive_HasCorrectLocale()
    {
        MongoDbCollationOptions preset = CollationPresets.SpanishCaseInsensitive;
        Assert.Equal("es", preset.Locale);
    }

    [Fact]
    public void CollationPresets_NumericOrdered_HasNumericOrdering()
    {
        MongoDbCollationOptions preset = CollationPresets.NumericOrdered;
        Assert.True(preset.NumericOrdering);
    }

    [Fact]
    public void CollationPresets_SimpleBinary_HasSimpleLocale()
    {
        MongoDbCollationOptions preset = CollationPresets.SimpleBinary;
        Assert.Equal("simple", preset.Locale);
    }

    [Fact]
    public void CollationPresets_AreNewInstancesEachTime()
    {
        MongoDbCollationOptions p1 = CollationPresets.EnglishCaseInsensitive;
        MongoDbCollationOptions p2 = CollationPresets.EnglishCaseInsensitive;
        Assert.NotSame(p1, p2);
    }

    #endregion

    #region [ MongoDbShardingOptions ]

    [Fact]
    public void MongoDbShardingOptions_DefaultValues_AreCorrect()
    {
        var opts = new MongoDbShardingOptions();

        Assert.NotNull(opts.ShardKeyFields);
        Assert.Empty(opts.ShardKeyFields);
        Assert.False(opts.UseHashedShardKey);
        Assert.False(opts.UniqueShardKey);
        Assert.Null(opts.NumInitialChunks);
    }

    [Fact]
    public void ShardKeyField_Ascending_CreatesAscendingField()
    {
        var field = ShardKeyField.Ascending("userId");

        Assert.Equal("userId", field.FieldName);
        Assert.Equal(1, field.Order.ToInt32());
    }

    [Fact]
    public void ShardKeyField_Descending_CreatesDescendingField()
    {
        var field = ShardKeyField.Descending("createdAt");

        Assert.Equal("createdAt", field.FieldName);
        Assert.Equal(-1, field.Order.ToInt32());
    }

    [Fact]
    public void ShardKeyField_Hashed_CreatesHashedField()
    {
        var field = ShardKeyField.Hashed("_id");

        Assert.Equal("_id", field.FieldName);
        Assert.Equal("hashed", field.Order.AsString);
    }

    #endregion
}
