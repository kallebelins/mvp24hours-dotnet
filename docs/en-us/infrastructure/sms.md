# SMS providers

The SMS infrastructure exposes `ISmsService` for single, batch, and MMS sends. The built-in implementations are Twilio, Azure Communication Services, and in-memory providers. Send operations return `SmsSendResult`; ordinary validation and provider failures are represented by failed results.

## Register a provider

Choose one provider registration. Each registration installs a singleton `ISmsService`.

```csharp
using Mvp24Hours.Infrastructure.Sms.Extensions;

services.AddTwilioSmsService(
    twilio =>
    {
        twilio.AccountSid = configuration["Twilio:AccountSid"]!;
        twilio.AuthToken = configuration["Twilio:AuthToken"]!;
    },
    sms =>
    {
        sms.DefaultFrom = "+15551234567";
        sms.MaxMessageLength = 160;
    });
```

Verified registrations:

- `AddSmsService(...)`: registers `InMemorySmsProvider` by default.
- `AddSmsServiceWithProvider(factory, ...)`: registers a custom provider.
- `AddTwilioSmsService(configureTwilio, configureSms?)`; also registers `IHttpClientFactory`.
- `AddAzureCommunicationSmsService(configureAzure, configureSms?)`; also registers `IHttpClientFactory`.
- `AddInMemorySmsService(...)`.
- `AddSmsTemplateService()`: registers the in-memory template service.
- `AddSmsRateLimiter(...)`: registers the in-memory rate limiter.

Twilio and Azure provider options are validated when the singleton provider is first resolved. These registrations do not use `ValidateOnStart`. `SmsOptions.Validate()` is available to callers but is not invoked automatically by registration.

## Send and validate messages

`SmsMessage` requires non-empty `To` and `Body` values.

```csharp
using Mvp24Hours.Infrastructure.Sms.Contract;
using Mvp24Hours.Infrastructure.Sms.Models;
using Mvp24Hours.Infrastructure.Sms.Results;

SmsSendResult result = await smsService.SendAsync(new SmsMessage
{
    To = "+15559876543",
    Body = "Your verification code is 4812."
}, cancellationToken);

if (!result.Success)
{
    logger.LogWarning("SMS failed: {Error}", result.FirstError);
}
```

When `ValidatePhoneNumbers` is enabled, the base provider removes spaces, hyphens, parentheses, and dots, then requires 7–15 digits with an optional leading `+`. This is basic format validation, not country-aware validation. `DefaultCountryCode` is validated as two letters but the current base provider does not prepend or translate it into a dialing prefix. Prefer E.164 input.

## `SmsOptions`

| Property | Type | Default | Required / behavior |
|---|---|---:|---|
| `DefaultFrom` | `string?` | `null` | Used when `SmsMessage.From` is absent; provider/account rules still apply. |
| `DefaultCountryCode` | `string?` | `null` | If set, `Validate()` requires two letters. It does not currently rewrite recipient numbers. |
| `MaxMessageLength` | `int?` | `null` | If set, must be greater than zero and messages over the limit fail validation. |
| `ValidatePhoneNumbers` | `bool` | `true` | Enables the basic 7–15 digit format check. |

## Twilio

| `TwilioSmsOptions` property | Type | Default | Required / behavior |
|---|---|---:|---|
| `AccountSid` | `string` | `""` | Required and must start with `AC`. |
| `AuthToken` | `string` | `""` | Required. |
| `ApiBaseUrl` | `string?` | `null` | Uses `https://api.twilio.com` when absent. |
| `ValidatePhoneNumbers` | `bool` | `false` | Present on the option type, but not consumed by the current provider. Common validation is controlled by `SmsOptions.ValidatePhoneNumbers`. |

The provider uses Twilio's REST endpoint directly through `IHttpClientFactory` and Basic authentication; it does not instantiate the official Twilio SDK client. Its HTTP client timeout is 30 seconds. Twilio response states are mapped to `Queued`, `Sent`, `Delivered`, `Failed`, `Undelivered`, or `Unknown`.

See the [Twilio Message resource](https://www.twilio.com/docs/messaging/api/message-resource).

## Azure Communication Services SMS

```csharp
services.AddAzureCommunicationSmsService(
    azure =>
    {
        azure.ConnectionString =
            configuration["AzureCommunication:SmsConnectionString"]!;
        azure.EnableDeliveryReports = true;
    },
    sms => sms.DefaultFrom = "+15551234567");
```

| `AzureCommunicationSmsOptions` property | Type | Default | Required / behavior |
|---|---|---:|---|
| `ConnectionString` | `string` | `""` | Required. At provider construction it must contain usable `endpoint` and `accesskey` values. |
| `Endpoint` | `string?` | `null` | Optional override for the parsed endpoint. |
| `AccessKey` | `string?` | `null` | Optional override for the parsed access key. |
| `EnableDeliveryReports` | `bool` | `false` | Included in the send request. |

The provider sends signed HTTP requests through `IHttpClientFactory`; it does not instantiate the Azure Communication SMS SDK client. Its HTTP client timeout is 30 seconds. A successful request is returned with `Queued` status.

See [Azure Communication Services SMS concepts](https://learn.microsoft.com/en-us/azure/communication-services/concepts/sms/concepts).

## Templates

`AddSmsTemplateService()` registers singleton `ISmsTemplateService` as `InMemorySmsTemplateService`. It saves, retrieves, renders, lists, and deletes `SmsTemplate` instances in process memory. Placeholders use braces, for example `Welcome {Name}!`.

The implementation is not persistent or distributed; use a custom `ISmsTemplateService` for production storage.

## Rate limiting

```csharp
services.AddSmsRateLimiter(options =>
{
    options.Enabled = true;
    options.MaxMessagesPerDestination = 5;
    options.TimeWindow = TimeSpan.FromHours(1);
    options.ThrowOnExceeded = false;
});
```

| `SmsRateLimitOptions` property | Type | Default | Behavior |
|---|---|---:|---|
| `Enabled` | `bool` | `true` | Disables all checks when `false`. |
| `MaxMessagesPerDestination` | `int` | `10` | Maximum messages tracked for one destination. |
| `TimeWindow` | `TimeSpan` | 1 hour | Rolling period retained per destination. |
| `ThrowOnExceeded` | `bool` | `false` | Exposed by the option type but not read by the current in-memory limiter. |

`AddSmsRateLimiter` registers singleton `ISmsRateLimiter` as `InMemorySmsRateLimiter`. Call `IsAllowedAsync(destination)` before sending and `RecordSentAsync(destination)` after a successful send. `GetCountAsync` and `ResetAsync` support inspection and reset. Registration does not decorate `ISmsService`, so rate limiting is not automatically enforced by Twilio or Azure providers. State is local to one process; use a distributed implementation for multiple instances.

## Health check and observability

```csharp
using Mvp24Hours.Infrastructure.HealthChecks;

services.AddHealthChecks()
    .AddSmsServiceHealthCheck("sms-service", options =>
    {
        options.SendTestSms = false;
        options.TimeoutSeconds = 10;
    });
```

| `SmsServiceHealthCheckOptions` property | Type | Default |
|---|---|---:|
| `SendTestSms` | `bool` | `false` |
| `TestSmsRecipient` | `string?` | `null` (uses `+1234567890`) |
| `TestSmsBody` | `string?` | `null` (uses `Health check test`) |
| `TimeoutSeconds` | `int` | `10` |
| `DegradedThresholdMs` | `int` | `2000` |
| `FailureThresholdMs` | `int` | `10000` |
| `Tags` | `IEnumerable<string>` | `sms`, `sms-service`, `ready` |

With `SendTestSms = false`, the check returns Healthy after resolving the service; it does not verify network access, credentials, or delivery. Enabling it sends a real SMS and can incur provider charges. Result data contains response/send time and, when applicable, send success, message ID, delivery status, recipient, and error details.

Twilio, Azure, and the health check emit structured `ILogger` messages. No SMS-specific metrics or tracing instruments are currently exposed.

## Testing

```csharp
using Mvp24Hours.Infrastructure.Testing.Assertions;
using Mvp24Hours.Infrastructure.Testing.Extensions;
using Mvp24Hours.Infrastructure.Testing.Fakes;

services.AddFakeSmsService(fake =>
{
    fake.ShouldFail = false;
});

IFakeSmsService fake = serviceProvider.GetRequiredService<IFakeSmsService>();
await serviceUnderTest.SendCodeAsync(cancellationToken);

SmsAssertions.AssertSmsSentTo(fake, "+15559876543");
SmsAssertions.AssertSmsSentContaining(fake, "verification code");
SmsAssertions.AssertSmsCount(fake, 1);
```

`AddFakeSmsService` registers the same singleton as `ISmsService`, `IFakeSmsService`, and `FakeSmsService`. `AddMvpTestingInfrastructure()` also includes it. The fake records SMS and MMS messages and supports `ShouldFail`, `FailureMessage`, `SimulatedDelay`, `CustomResultFactory`, `ClearSentMessages()`, and query helpers.

Available assertions are `AssertSmsSent` (with or without a predicate), `AssertSmsCount`, `AssertSmsSentTo`, `AssertSmsSentContaining`, `AssertNoSmsSent`, and `GetLastSentSms`.

