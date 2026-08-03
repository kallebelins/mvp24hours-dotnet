//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Email.Models;
using Mvp24Hours.Infrastructure.Email.Providers;
using Mvp24Hours.Infrastructure.Email.Results;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.Email;

[Trait("Category", "Unit")]
public class EmailProviderEdgeCasesTest
{
    [Fact]
    public async Task InMemoryEmailProvider_SendAsync_WithEmptyRecipients_ShouldFail()
    {
        InMemoryEmailProvider provider = CreateProvider();
        EmailMessage message = EmailTestHelpers.CreateValidMessage();
        message.To = [];

        EmailSendResult result = await provider.SendAsync(message);

        result.Success.Should().BeFalse();
        result.FirstError.Should().Contain("recipient");
    }

    [Fact]
    public async Task InMemoryEmailProvider_SendBatchAsync_WithEmptyCollection_ShouldReturnEmptyResults()
    {
        InMemoryEmailProvider provider = CreateProvider();

        IList<EmailSendResult> results = await provider.SendBatchAsync([]);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task InMemoryEmailProvider_SendAsync_WithMissingSubject_ShouldFail()
    {
        InMemoryEmailProvider provider = CreateProvider();
        EmailMessage message = EmailTestHelpers.CreateValidMessage();
        message.Subject = "  ";

        EmailSendResult result = await provider.SendAsync(message);

        result.Success.Should().BeFalse();
        result.FirstError.Should().Contain("Subject");
    }

    private static InMemoryEmailProvider CreateProvider()
    {
        return new InMemoryEmailProvider(EmailTestHelpers.CreateEmailOptions());
    }
}
