using Cronos;
using Mvp24Hours.Infrastructure.CronJob.Scheduling;

namespace Mvp24Hours.Infrastructure.CronJob.Test.Scheduling;

[Trait("Category", "Unit")]
public class CronExpressionParserTest
{
    [Theory]
    [InlineData("* * * * *", CronExpressionFormat.Standard)]
    [InlineData("* * * * * *", CronExpressionFormat.WithSeconds)]
    public void DetectFormat_ShouldIdentifyFieldCount(string expression, CronExpressionFormat expected)
    {
        CronExpressionParser.DetectFormat(expression).Should().Be(expected);
    }

    [Fact]
    public void DetectFormat_ShouldReturnStandard_WhenEmpty()
    {
        CronExpressionParser.DetectFormat("").Should().Be(CronExpressionFormat.Standard);
    }

    [Fact]
    public void Parse_ShouldParseStandardExpression()
    {
        CronExpression expression = CronExpressionParser.Parse("0 * * * *");

        expression.Should().NotBeNull();
    }

    [Fact]
    public void Parse_ShouldParseWithSecondsFormat()
    {
        CronExpression expression = CronExpressionParser.Parse("*/30 * * * * *", CronExpressionFormat.WithSeconds);

        expression.Should().NotBeNull();
    }

    [Fact]
    public void Parse_ShouldThrow_WhenExpressionNull()
    {
        Action act = () => CronExpressionParser.Parse(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TryParse_ShouldReturnFalse_ForInvalidExpression()
    {
        bool result = CronExpressionParser.TryParse("not valid", out CronExpression? parsed);

        result.Should().BeFalse();
        parsed.Should().BeNull();
    }

    [Fact]
    public void TryParse_ShouldReturnTrue_ForValidExpression()
    {
        bool result = CronExpressionParser.TryParse("0 0 * * *", out CronExpression? parsed);

        result.Should().BeTrue();
        parsed.Should().NotBeNull();
    }

    [Fact]
    public void IsValid_ShouldValidateExpression()
    {
        CronExpressionParser.IsValid("0 0 * * *").Should().BeTrue();
        CronExpressionParser.IsValid("bad cron").Should().BeFalse();
    }

    [Fact]
    public void GetNextOccurrence_ShouldReturnFutureDate()
    {
        DateTimeOffset from = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        DateTimeOffset? next = CronExpressionParser.GetNextOccurrence("0 0 * * *", from, TimeZoneInfo.Utc);

        next.Should().NotBeNull();
        next!.Value.Should().BeAfter(from);
    }

    [Theory]
    [InlineData("* * * * *", "Every minute")]
    [InlineData("*/5 * * * *", "Every 5 minutes")]
    [InlineData("0 * * * *", "Every hour")]
    [InlineData("0 0 * * *", "Daily at midnight")]
    [InlineData("* * * * * *", "Every second")]
    [InlineData("*/10 * * * * *", "Every 10 seconds")]
    public void Describe_ShouldReturnHumanReadableText(string expression, string expectedFragment)
    {
        CronExpressionParser.Describe(expression).Should().Contain(expectedFragment);
    }

    [Fact]
    public void Describe_ShouldReturnImmediateRun_WhenEmpty()
    {
        CronExpressionParser.Describe("").Should().Be("Run once immediately");
    }

    [Fact]
    public void Describe_ShouldReturnInvalid_ForTooFewFields()
    {
        CronExpressionParser.Describe("* *").Should().Be("Invalid expression");
    }
}
