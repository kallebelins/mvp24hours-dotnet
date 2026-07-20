//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.BackgroundJobs.Options;

namespace Mvp24Hours.Infrastructure.Test.BackgroundJobs.Options;

[Trait("Category", "Unit")]
public class JobOptionsTest
{
    [Fact]
    public void Default_ShouldUseExpectedValues()
    {
        JobOptions options = JobOptions.Default;

        options.MaxRetryAttempts.Should().Be(3);
        options.InitialRetryDelay.Should().Be(TimeSpan.FromSeconds(5));
        options.MaxRetryDelay.Should().Be(TimeSpan.FromHours(1));
        options.UseExponentialBackoff.Should().BeTrue();
        options.Timeout.Should().Be(TimeSpan.FromHours(1));
        options.Priority.Should().Be(JobPriority.Normal);
        options.Queue.Should().BeNull();
        options.Metadata.Should().NotBeNull().And.BeEmpty();
        options.DeleteOnSuccess.Should().BeFalse();
        options.RetentionDays.Should().Be(30);
    }

    [Fact]
    public void Validate_WithDefaultOptions_ShouldReturnEmpty()
    {
        var options = new JobOptions();

        IList<string> errors = options.Validate();

        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithNegativeRetryAttempts_ShouldReturnError()
    {
        var options = new JobOptions { MaxRetryAttempts = -1 };

        options.Validate().Should().Contain("Maximum retry attempts must be greater than or equal to zero.");
    }

    [Fact]
    public void Validate_WithInvalidRetryDelays_ShouldReturnErrors()
    {
        var options = new JobOptions
        {
            InitialRetryDelay = TimeSpan.FromSeconds(10),
            MaxRetryDelay = TimeSpan.FromSeconds(1)
        };

        IList<string> errors = options.Validate();

        errors.Should().Contain("Maximum retry delay must be greater than or equal to initial retry delay.");
    }

    [Fact]
    public void Validate_WithInvalidTimeoutAndRetention_ShouldReturnErrors()
    {
        var options = new JobOptions
        {
            Timeout = TimeSpan.Zero,
            RetentionDays = -1
        };

        IList<string> errors = options.Validate();

        errors.Should().Contain("Timeout must be greater than zero.");
        errors.Should().Contain("Retention days must be greater than or equal to zero.");
    }
}
