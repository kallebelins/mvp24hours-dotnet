//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Email.Contract;
using Mvp24Hours.Infrastructure.Email.Models;
using Mvp24Hours.Infrastructure.Email.Options;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.Email.Models;

[Trait("Category", "Unit")]
public class EmailMessageTest
{
    [Fact]
    public void Constructor_ShouldInitializeCollections()
    {
        var message = new EmailMessage();

        message.To.Should().NotBeNull().And.BeEmpty();
        message.Cc.Should().NotBeNull().And.BeEmpty();
        message.Bcc.Should().NotBeNull().And.BeEmpty();
        message.Attachments.Should().NotBeNull().And.BeEmpty();
        message.EmbeddedImages.Should().NotBeNull().And.BeEmpty();
        message.Headers.Should().NotBeNull().And.BeEmpty();
        message.Priority.Should().Be(EmailPriority.Normal);
        message.RequestReadReceipt.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidMessage_ShouldReturnEmptyErrors()
    {
        EmailMessage message = EmailTestHelpers.CreateValidMessage();

        message.Validate().Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithoutRecipients_ShouldReturnError()
    {
        var message = new EmailMessage
        {
            Subject = "Subject",
            PlainTextBody = "Body"
        };

        message.Validate().Should().Contain("At least one recipient (To, Cc, or Bcc) must be specified.");
    }

    [Fact]
    public void Validate_WithoutSubject_ShouldReturnError()
    {
        var message = new EmailMessage
        {
            To = ["user@example.com"],
            PlainTextBody = "Body"
        };

        message.Validate().Should().Contain("Subject is required.");
    }

    [Fact]
    public void Validate_WithoutBody_ShouldReturnError()
    {
        var message = new EmailMessage
        {
            To = ["user@example.com"],
            Subject = "Subject"
        };

        message.Validate().Should().Contain("At least one body format (HtmlBody or PlainTextBody) must be specified.");
    }

    [Fact]
    public void Validate_WithBccOnly_ShouldBeValid()
    {
        var message = new EmailMessage
        {
            Bcc = ["hidden@example.com"],
            Subject = "Subject",
            HtmlBody = "<p>Hi</p>"
        };

        message.Validate().Should().BeEmpty();
    }

    [Fact]
    public void HasRecipients_HasBody_HasAttachments_ShouldReflectState()
    {
        var message = new EmailMessage
        {
            Subject = "Subject",
            PlainTextBody = "Body"
        };

        message.HasRecipients.Should().BeFalse();
        message.HasBody.Should().BeTrue();
        message.HasAttachments.Should().BeFalse();

        message.To.Add("user@example.com");
        message.Attachments.Add(new EmailAttachment("a.txt", "data"u8.ToArray(), "text/plain"));

        message.HasRecipients.Should().BeTrue();
        message.HasAttachments.Should().BeTrue();
    }

    [Fact]
    public void Properties_ShouldRoundTripValues()
    {
        var message = new EmailMessage
        {
            To = ["to@example.com"],
            Cc = ["cc@example.com"],
            Bcc = ["bcc@example.com"],
            From = "from@example.com",
            ReplyTo = "reply@example.com",
            Subject = "Subject",
            HtmlBody = "<p>Html</p>",
            PlainTextBody = "Text",
            Priority = EmailPriority.High,
            RequestReadReceipt = true
        };
        message.Headers["X-Test"] = "1";

        message.To.Should().ContainSingle("to@example.com");
        message.Cc.Should().ContainSingle("cc@example.com");
        message.Bcc.Should().ContainSingle("bcc@example.com");
        message.From.Should().Be("from@example.com");
        message.ReplyTo.Should().Be("reply@example.com");
        message.Subject.Should().Be("Subject");
        message.HtmlBody.Should().Be("<p>Html</p>");
        message.PlainTextBody.Should().Be("Text");
        message.Priority.Should().Be(EmailPriority.High);
        message.RequestReadReceipt.Should().BeTrue();
        message.Headers["X-Test"].Should().Be("1");
        message.Attachments.Should().BeAssignableTo<IList<IEmailAttachment>>();
    }
}
