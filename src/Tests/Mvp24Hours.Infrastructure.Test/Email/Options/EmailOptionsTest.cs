//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Email.Options;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.Email.Options;

[Trait("Category", "Unit")]
public class EmailOptionsTest
{
    [Fact]
    public void Default_ShouldReturnNewInstanceWithExpectedDefaults()
    {
        EmailOptions options = EmailOptions.Default;

        options.DefaultFrom.Should().BeNull();
        options.DefaultReplyTo.Should().BeNull();
        options.DefaultSubjectPrefix.Should().BeNull();
        options.DefaultPriority.Should().Be(EmailPriority.Normal);
        options.DefaultRequestReadReceipt.Should().BeFalse();
        options.MaxRecipientsPerEmail.Should().BeNull();
        options.MaxAttachmentSize.Should().Be(25 * 1024 * 1024);
        options.MaxAttachmentsPerEmail.Should().BeNull();
        options.DefaultHeaders.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Validate_WithValidOptions_ShouldReturnEmptyErrors()
    {
        EmailOptions options = EmailTestHelpers.CreateEmailOptions();

        options.Validate().Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithNonPositiveMaxRecipients_ShouldReturnError()
    {
        EmailOptions options = EmailTestHelpers.CreateEmailOptions(maxRecipientsPerEmail: 0);

        options.Validate().Should().Contain("Maximum recipients per email must be greater than zero.");
    }

    [Fact]
    public void Validate_WithNonPositiveMaxAttachmentSize_ShouldReturnError()
    {
        EmailOptions options = EmailTestHelpers.CreateEmailOptions(maxAttachmentSize: -1);

        options.Validate().Should().Contain("Maximum attachment size must be greater than zero.");
    }

    [Fact]
    public void Validate_WithNonPositiveMaxAttachments_ShouldReturnError()
    {
        EmailOptions options = EmailTestHelpers.CreateEmailOptions(maxAttachmentsPerEmail: 0);

        options.Validate().Should().Contain("Maximum attachments per email must be greater than zero.");
    }

    [Fact]
    public void Constructor_ShouldInitializeDefaultHeaders()
    {
        var options = new EmailOptions();

        options.DefaultHeaders.Should().NotBeNull().And.BeEmpty();
        options.DefaultHeaders["X-App"] = "Test";

        options.DefaultHeaders["X-App"].Should().Be("Test");
    }
}
