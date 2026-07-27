# Email providers

The Email infrastructure exposes one sending contract, `IEmailService`, with SMTP, SendGrid, Azure Communication Services, and in-memory implementations. `SendAsync` and `SendBatchAsync` return `EmailSendResult`; provider and validation failures are normally represented by a failed result rather than thrown from the send operation.

## Register a provider

Choose one provider registration. Each registration installs a singleton `IEmailService`.

```csharp
using Mvp24Hours.Infrastructure.Email.Extensions;

services.AddSmtpEmailService(
    smtp =>
    {
        smtp.Host = "smtp.example.com";
        smtp.Port = 587;
        smtp.Username = configuration["Smtp:Username"];
        smtp.Password = configuration["Smtp:Password"];
        smtp.EnableStartTls = true;
    },
    email =>
    {
        email.DefaultFrom = "Mvp24Hours <noreply@example.com>";
        email.DefaultReplyTo = "support@example.com";
    });
```

Verified registrations:

- `AddEmailService(...)`: registers `InMemoryEmailProvider` by default.
- `AddEmailServiceWithProvider(factory, ...)`: registers a custom provider.
- `AddSmtpEmailService(configureSmtp, configureEmail?)`.
- `AddSendGridEmailService(configureSendGrid, configureEmail?)`; also registers `IHttpClientFactory`.
- `AddAzureCommunicationEmailService(configureAzure, configureEmail?)`; also registers `IHttpClientFactory`.
- `AddInMemoryEmailService(...)`.

The provider-specific option classes run their `Validate()` method when the singleton provider is first resolved. These registrations do not use `ValidateOnStart`. `EmailOptions.Validate()` is available to callers, but the registrations do not invoke it automatically.

## Send a message

`EmailMessage` requires at least one address across `To`, `Cc`, or `Bcc`, a non-empty `Subject`, and either `HtmlBody` or `PlainTextBody`.

```csharp
using Mvp24Hours.Infrastructure.Email.Contract;
using Mvp24Hours.Infrastructure.Email.Models;

var result = await emailService.SendAsync(new EmailMessage
{
    To = ["customer@example.com"],
    Subject = "Order received",
    PlainTextBody = "We received your order.",
    HtmlBody = "<p>We received your order.</p>"
}, cancellationToken);

if (!result.Success)
{
    logger.LogWarning("Email failed: {Error}", result.FirstError);
}
```

The base provider applies `DefaultFrom`, `DefaultReplyTo`, the subject prefix, priority, read-receipt setting, and headers without mutating the original message. It also enforces configured recipient, attachment-count, and per-attachment size limits.

## `EmailOptions`

| Property | Type | Default | Required / behavior |
|---|---|---:|---|
| `DefaultFrom` | `string?` | `null` | Not required by the option type, but a provider generally needs a message or default sender. |
| `DefaultReplyTo` | `string?` | `null` | Falls back to provider behavior when absent. |
| `DefaultSubjectPrefix` | `string?` | `null` | Prepended verbatim to every subject. |
| `DefaultPriority` | `EmailPriority` | `Normal` | Used when the message priority is `Normal`. |
| `DefaultRequestReadReceipt` | `bool` | `false` | Combined with the message value using logical OR. |
| `MaxRecipientsPerEmail` | `int?` | `null` | If set, must be greater than zero. |
| `MaxAttachmentSize` | `long?` | `26,214,400` (25 MiB) | Per attachment; if set, must be greater than zero. |
| `MaxAttachmentsPerEmail` | `int?` | `null` | If set, must be greater than zero. |
| `DefaultHeaders` | `IDictionary<string,string>` | empty | Message headers override matching defaults. |

## SMTP

`SmtpEmailProvider` uses `System.Net.Mail.SmtpClient`.

| `SmtpEmailOptions` property | Type | Default | Required / behavior |
|---|---|---:|---|
| `Host` | `string` | `""` | Required. |
| `Port` | `int` | `587` | Must be in `1..65535`. |
| `Username` | `string?` | `null` | Required when `UseDefaultCredentials` is `false`. |
| `Password` | `string?` | `null` | Required when `UseDefaultCredentials` is `false`. |
| `EnableSsl` | `bool` | `false` | Assigned to `SmtpClient.EnableSsl`. |
| `EnableStartTls` | `bool` | `true` | Informational in the current implementation; it is not mapped to a separate `SmtpClient` setting. |
| `Timeout` | `int` | `30000` ms | Must be greater than zero. |
| `ServerCertificateValidationCallback` | `RemoteCertificateValidationCallback?` | `null` | Retained for API compatibility but not applied; see below. |
| `UseDefaultCredentials` | `bool` | `false` | Uses the current default credentials and makes username/password optional. |

### Certificate callback behavior in v10

Version 10 removed use of the obsolete global `ServicePointManager.ServerCertificateValidationCallback`. `System.Net.Mail.SmtpClient` has no per-client certificate-validation callback, so `SmtpEmailOptions.ServerCertificateValidationCallback` is now ignored. If configured, the provider logs a warning and certificate validation uses the operating system trust store.

Do not configure a callback that blindly returns `true`. If custom per-connection SMTP certificate validation is mandatory, use a provider/library that exposes that capability, such as MailKit.

See the [.NET `SmtpClient` notes](https://learn.microsoft.com/en-us/dotnet/api/system.net.mail.smtpclient).

## SendGrid

```csharp
services.AddSendGridEmailService(
    sendGrid =>
    {
        sendGrid.ApiKey = configuration["SendGrid:ApiKey"]!;
        sendGrid.EnableClickTracking = true;
        sendGrid.EnableOpenTracking = true;
    },
    email => email.DefaultFrom = "noreply@example.com");
```

| `SendGridEmailOptions` property | Type | Default | Required / behavior |
|---|---|---:|---|
| `ApiKey` | `string` | `""` | Required. |
| `DefaultFrom` | `string?` | `null` | Provider-level sender fallback. |
| `DefaultFromName` | `string?` | `null` | Optional display name. |
| `DefaultCategories` | `IList<string>` | empty | Added to provider requests. |
| `EnableClickTracking` | `bool` | `true` | Controls SendGrid click tracking. |
| `EnableOpenTracking` | `bool` | `true` | Controls SendGrid open tracking. |
| `ApiBaseUrl` | `string` | `https://api.sendgrid.com/v3` | Must be a non-empty absolute URI. |

The implementation calls the SendGrid v3 HTTP API through `IHttpClientFactory`; it does not instantiate the official SendGrid SDK client. See the [SendGrid Mail Send API](https://www.twilio.com/docs/sendgrid/api-reference/mail-send/mail-send).

## Azure Communication Services Email

```csharp
services.AddAzureCommunicationEmailService(
    azure =>
    {
        azure.ConnectionString =
            configuration["AzureCommunication:EmailConnectionString"]!;
        azure.EnableUserEngagementTracking = true;
    },
    email => email.DefaultFrom = "DoNotReply@example.azurecomm.net");
```

| `AzureCommunicationEmailOptions` property | Type | Default | Required / behavior |
|---|---|---:|---|
| `ConnectionString` | `string` | `""` | Required and must contain `endpoint=` and `accesskey=`. |
| `Endpoint` | `string?` | `null` | Optional absolute URI overriding the connection-string endpoint. |
| `DefaultFrom` | `string?` | `null` | Provider-level sender fallback. |
| `DefaultFromName` | `string?` | `null` | Optional display name. |
| `EnableUserEngagementTracking` | `bool` | `true` | Included in provider requests. |

The implementation sends signed HTTP requests through `IHttpClientFactory`; it does not instantiate the Azure Communication Email SDK client. See [Azure Communication Services Email](https://learn.microsoft.com/en-us/azure/communication-services/concepts/email/email-overview).

## Templates

Register Scriban (the default) or Razor:

```csharp
using Mvp24Hours.Infrastructure.Email.Extensions;

services.AddEmailTemplateRenderer(TemplateEngine.Scriban);

string body = await renderer.RenderAsync(
    "Hello {{ Name }}, order {{ OrderId }} is ready.",
    new { Name = "Alex", OrderId = 42 },
    cancellationToken);
```

`IEmailTemplateRenderer` supports rendering a string from an object or dictionary, rendering a file, and validating syntax. Scriban parse/render errors are surfaced as `TemplateRenderException`.

| `TemplateOptions` property | Type | Default | Current behavior |
|---|---|---:|---|
| `StrictMode` | `bool` | `false` | Exposed by the type but not applied by the current Scriban renderer. |
| `DefaultValueForMissingVariables` | `string?` | `null` | Exposed by the type but not applied by the current Scriban renderer. |

## Queue, bulk sending, and rate limiting

```csharp
services.AddEmailQueue(
    startProcessor: true,
    configureProcessor: options =>
    {
        options.PollInterval = TimeSpan.FromSeconds(5);
        options.MaxRetryAttempts = 3;
    });

services.AddEmailBulkSender(rateLimit =>
{
    rateLimit.MaxRequestsPerWindow = 100;
    rateLimit.WindowSize = TimeSpan.FromMinutes(1);
    rateLimit.Strategy = RateLimitStrategy.FixedWindow;
});
```

`AddEmailQueue` registers `InMemoryEmailQueue` when `useInMemory` is `true`. The hosted `EmailQueueProcessor` currently processes only that concrete queue; custom queues must provide their own processing mechanism. The in-memory queue is suitable for tests and single-process workloads, not durable or distributed delivery.

| `EmailQueueProcessorOptions` property | Type | Default | Current behavior |
|---|---|---:|---|
| `PollInterval` | `TimeSpan` | 5 seconds | Delay between polls. |
| `MaxRetryAttempts` | `int` | `3` | Used when deciding whether a failed item may retry. |
| `RetryDelay` | `TimeSpan` | 1 minute | Exposed but not applied by the current processor. |
| `MaxConcurrency` | `int` | `1` | Exposed but the current processor handles one item per poll. |

`AddEmailRateLimiter` registers a singleton `EmailRateLimiter`. `AddEmailBulkSender` registers a scoped `EmailBulkSender` and creates a private limiter only when its rate-limit callback is supplied.

| `RateLimitOptions` property | Type | Default |
|---|---|---:|
| `MaxRequestsPerWindow` | `int` | `100` |
| `WindowSize` | `TimeSpan` | 1 minute |
| `Strategy` | `RateLimitStrategy` | `FixedWindow` |

Strategies are `FixedWindow`, `SlidingWindow`, and `TokenBucket`.

| `BulkSendOptions` property | Type | Default |
|---|---|---:|
| `MaxConcurrency` | `int` | `1` |
| `DelayBetweenSends` | `TimeSpan` | `TimeSpan.Zero` |

## Health check and observability

```csharp
using Mvp24Hours.Infrastructure.HealthChecks;

services.AddHealthChecks()
    .AddEmailServiceHealthCheck("email-service", options =>
    {
        options.SendTestEmail = false;
        options.TimeoutSeconds = 10;
    });
```

| `EmailServiceHealthCheckOptions` property | Type | Default |
|---|---|---:|
| `SendTestEmail` | `bool` | `false` |
| `TestEmailRecipient` | `string?` | `null` (uses `health-check@example.com`) |
| `TestEmailSubject` | `string?` | `null` (uses `Health Check Test`) |
| `TestEmailBody` | `string?` | `null` (uses built-in text) |
| `TimeoutSeconds` | `int` | `10` |
| `DegradedThresholdMs` | `int` | `2000` |
| `FailureThresholdMs` | `int` | `10000` |
| `Tags` | `IEnumerable<string>` | `email`, `email-service`, `ready` |

With `SendTestEmail = false`, the check returns Healthy after resolving the service; it does not test network connectivity or credentials. Enabling it sends a real message and may incur cost. The result data includes response/send time and, when applicable, success, message ID, recipient, and error information.

SMTP, SendGrid, Azure, bulk sending, queue processing, and the health check emit structured `ILogger` messages. No email-specific metrics or tracing instruments are currently exposed.

## Testing

Register the purpose-built fake rather than the in-memory production provider when assertions or simulated failures are needed:

```csharp
using Mvp24Hours.Infrastructure.Testing.Assertions;
using Mvp24Hours.Infrastructure.Testing.Extensions;
using Mvp24Hours.Infrastructure.Testing.Fakes;

services.AddFakeEmailService(fake =>
{
    fake.ShouldFail = false;
});

IFakeEmailService fake = serviceProvider.GetRequiredService<IFakeEmailService>();
await serviceUnderTest.ExecuteAsync(cancellationToken);

EmailAssertions.AssertEmailSentTo(fake, "customer@example.com");
EmailAssertions.AssertEmailSentWithSubject(fake, "Order");
EmailAssertions.AssertEmailCount(fake, 1);
```

`AddFakeEmailService` registers the same singleton as `IEmailService`, `IFakeEmailService`, and `FakeEmailService`. `AddMvpTestingInfrastructure()` also includes it. The fake records sent messages and supports `ShouldFail`, `FailureMessage`, `SimulatedDelay`, `CustomResultFactory`, `ClearSentEmails()`, and query helpers.

Available assertions are `AssertEmailSent` (with or without a predicate), `AssertEmailCount`, `AssertEmailSentTo`, `AssertEmailSentWithSubject`, `AssertNoEmailsSent`, and `GetLastSentEmail`.

