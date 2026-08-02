//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Attributes;
using Xunit;

namespace Mvp24Hours.Application.MongoDb.Test.Performance;

[Trait("Category", "Unit")]
public class PerformanceAttributesTest
{
    #region [ MongoIndexType ]

    [Fact]
    public void MongoIndexType_Values_AreCorrect()
    {
        Assert.Equal(0, (int)MongoIndexType.Ascending);
        Assert.Equal(1, (int)MongoIndexType.Descending);
        Assert.Equal(2, (int)MongoIndexType.Hashed);
        Assert.Equal(3, (int)MongoIndexType.Text);
        Assert.Equal(4, (int)MongoIndexType.Geo2d);
        Assert.Equal(5, (int)MongoIndexType.Geo2dSphere);
        Assert.Equal(6, (int)MongoIndexType.Wildcard);
    }

    [Fact]
    public void MongoIndexType_HasExpectedCount()
    {
        MongoIndexType[] values = Enum.GetValues<MongoIndexType>();
        Assert.Equal(7, values.Length);
    }

    #endregion

    #region [ MongoIndexAttribute ]

    [Fact]
    public void MongoIndexAttribute_DefaultValues_AreCorrect()
    {
        var attr = new MongoIndexAttribute();

        Assert.Equal(MongoIndexType.Ascending, attr.IndexType);
        Assert.False(attr.Unique);
        Assert.False(attr.Sparse);
        Assert.True(attr.Background);
        Assert.Null(attr.Name);
        Assert.Equal(0, attr.Order);
        Assert.Null(attr.CompoundIndexGroup);
        Assert.Null(attr.PartialFilterExpression);
        Assert.Null(attr.CollationLocale);
        Assert.False(attr.CollationCaseInsensitive);
    }

    [Fact]
    public void MongoIndexAttribute_CanAssignAllProperties()
    {
        var attr = new MongoIndexAttribute
        {
            IndexType = MongoIndexType.Geo2dSphere,
            Unique = true,
            Sparse = true,
            Background = false,
            Name = "idx_location",
            Order = 2,
            CompoundIndexGroup = "group1",
            PartialFilterExpression = "{ \"active\": true }",
            CollationLocale = "pt",
            CollationCaseInsensitive = true
        };

        Assert.Equal(MongoIndexType.Geo2dSphere, attr.IndexType);
        Assert.True(attr.Unique);
        Assert.True(attr.Sparse);
        Assert.False(attr.Background);
        Assert.Equal("idx_location", attr.Name);
        Assert.Equal(2, attr.Order);
        Assert.Equal("group1", attr.CompoundIndexGroup);
        Assert.Equal("{ \"active\": true }", attr.PartialFilterExpression);
        Assert.Equal("pt", attr.CollationLocale);
        Assert.True(attr.CollationCaseInsensitive);
    }

    [Fact]
    public void MongoIndexAttribute_IsAttribute()
    {
        Assert.True(typeof(Attribute).IsAssignableFrom(typeof(MongoIndexAttribute)));
    }

    [Fact]
    public void MongoIndexAttribute_IsSealed()
    {
        Assert.True(typeof(MongoIndexAttribute).IsSealed);
    }

    [Fact]
    public void MongoIndexAttribute_AllowsOnlyOnProperty()
    {
        AttributeUsageAttribute? usageAttr = typeof(MongoIndexAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        Assert.NotNull(usageAttr);
        Assert.True((usageAttr.ValidOn & AttributeTargets.Property) != 0);
        Assert.False(usageAttr.AllowMultiple);
    }

    #endregion

    #region [ MongoTtlIndexAttribute ]

    [Fact]
    public void MongoTtlIndexAttribute_DefaultConstructor_HasDefaults()
    {
        var attr = new MongoTtlIndexAttribute();

        Assert.Equal(0, attr.ExpireAfterSeconds);
        Assert.Null(attr.Name);
        Assert.True(attr.Background);
    }

    [Fact]
    public void MongoTtlIndexAttribute_WithSeconds_SetsExpiry()
    {
        var attr = new MongoTtlIndexAttribute(86400);
        Assert.Equal(86400, attr.ExpireAfterSeconds);
    }

    [Fact]
    public void MongoTtlIndexAttribute_CanAssignName()
    {
        var attr = new MongoTtlIndexAttribute { Name = "ttl_idx" };
        Assert.Equal("ttl_idx", attr.Name);
    }

    [Fact]
    public void MongoTtlIndexAttribute_CanDisableBackground()
    {
        var attr = new MongoTtlIndexAttribute { Background = false };
        Assert.False(attr.Background);
    }

    [Fact]
    public void MongoTtlIndexAttribute_IsSealed()
    {
        Assert.True(typeof(MongoTtlIndexAttribute).IsSealed);
    }

    [Fact]
    public void MongoTtlIndexAttribute_AllowsOnlyOnProperty()
    {
        AttributeUsageAttribute? usageAttr = typeof(MongoTtlIndexAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        Assert.NotNull(usageAttr);
        Assert.True((usageAttr.ValidOn & AttributeTargets.Property) != 0);
        Assert.False(usageAttr.AllowMultiple);
    }

    #endregion

    #region [ MongoCompoundIndexAttribute ]

    [Fact]
    public void MongoCompoundIndexAttribute_DefaultConstructor_HasDefaults()
    {
        var attr = new MongoCompoundIndexAttribute();

        Assert.Equal(string.Empty, attr.Fields);
        Assert.Null(attr.Name);
        Assert.False(attr.Unique);
        Assert.False(attr.Sparse);
        Assert.True(attr.Background);
        Assert.Null(attr.PartialFilterExpression);
        Assert.Null(attr.CollationLocale);
        Assert.False(attr.CollationCaseInsensitive);
    }

    [Fact]
    public void MongoCompoundIndexAttribute_FieldsConstructor_SetsFields()
    {
        var attr = new MongoCompoundIndexAttribute("Status:1,CreatedAt:-1");
        Assert.Equal("Status:1,CreatedAt:-1", attr.Fields);
    }

    [Fact]
    public void MongoCompoundIndexAttribute_CanAssignAllProperties()
    {
        var attr = new MongoCompoundIndexAttribute("A:1,B:-1")
        {
            Name = "idx_ab",
            Unique = true,
            Sparse = true,
            Background = false,
            PartialFilterExpression = "{ \"active\": true }",
            CollationLocale = "en",
            CollationCaseInsensitive = true
        };

        Assert.Equal("A:1,B:-1", attr.Fields);
        Assert.Equal("idx_ab", attr.Name);
        Assert.True(attr.Unique);
        Assert.True(attr.Sparse);
        Assert.False(attr.Background);
        Assert.Equal("{ \"active\": true }", attr.PartialFilterExpression);
        Assert.Equal("en", attr.CollationLocale);
        Assert.True(attr.CollationCaseInsensitive);
    }

    [Fact]
    public void MongoCompoundIndexAttribute_IsSealed()
    {
        Assert.True(typeof(MongoCompoundIndexAttribute).IsSealed);
    }

    [Fact]
    public void MongoCompoundIndexAttribute_AllowsOnClass_AndMultiple()
    {
        AttributeUsageAttribute? usageAttr = typeof(MongoCompoundIndexAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        Assert.NotNull(usageAttr);
        Assert.True((usageAttr.ValidOn & AttributeTargets.Class) != 0);
        Assert.True(usageAttr.AllowMultiple);
    }

    #endregion
}
