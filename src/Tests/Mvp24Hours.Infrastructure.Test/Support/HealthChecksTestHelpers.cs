//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Net;
using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mvp24Hours.Infrastructure.DistributedLocking.Contract;
using Mvp24Hours.Infrastructure.DistributedLocking.Options;
using Mvp24Hours.Infrastructure.DistributedLocking.Results;
using Mvp24Hours.Infrastructure.Email.Contract;
using Mvp24Hours.Infrastructure.Email.Models;
using Mvp24Hours.Infrastructure.Email.Results;
using Mvp24Hours.Infrastructure.FileStorage.Contract;
using Mvp24Hours.Infrastructure.FileStorage.Providers;
using Mvp24Hours.Infrastructure.FileStorage.Results;
using Mvp24Hours.Infrastructure.Http.Contract;
using Mvp24Hours.Infrastructure.Sms.Contract;
using Mvp24Hours.Infrastructure.Sms.Models;
using Mvp24Hours.Infrastructure.Sms.Results;

namespace Mvp24Hours.Infrastructure.Test.Support;

internal static class HealthChecksTestHelpers
{
    public static HealthCheckContext CreateContext(string name = "test")
    {
        return new HealthCheckContext
        {
            Registration = new HealthCheckRegistration(
                name,
                _ => throw new NotSupportedException("Factory not used in unit tests."),
                failureStatus: HealthStatus.Unhealthy,
                tags: null)
        };
    }

    public static ILogger<T> CreateLogger<T>()
    {
        return NullLogger<T>.Instance;
    }

    public static InMemoryFileStorageProvider CreateInMemoryStorage()
    {
        return new InMemoryFileStorageProvider(FileStorageTestHelpers.CreateOptions());
    }

    public static Mock<IFileStorage> CreateFileStorageMock(
        FileUploadResult? upload = null,
        bool exists = true,
        FileDownloadResult? download = null,
        bool deleted = true)
    {
        string content = "Health check test content";
        byte[] bytes = Encoding.UTF8.GetBytes(content);

        var mock = new Mock<IFileStorage>();
        mock.Setup(s => s.UploadAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(upload ?? FileUploadResult.Successful("health-check/test.txt"));

        mock.Setup(s => s.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(exists);

        mock.Setup(s => s.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(download ?? FileDownloadResult.Successful(bytes));

        mock.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleted);

        return mock;
    }

    public static Mock<IEmailService> CreateEmailServiceMock(EmailSendResult? result = null)
    {
        var mock = new Mock<IEmailService>();
        mock.Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result ?? EmailSendResult.Successful("msg-1"));
        return mock;
    }

    public static Mock<ISmsService> CreateSmsServiceMock(SmsSendResult? result = null)
    {
        var mock = new Mock<ISmsService>();
        mock.Setup(s => s.SendAsync(It.IsAny<SmsMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result ?? SmsSendResult.Successful("sms-1", SmsDeliveryStatus.Sent));
        return mock;
    }

    public static Mock<IDistributedLockFactory> CreateLockFactoryMock(
        LockAcquisitionResult? result = null,
        string? providerName = null,
        Exception? createException = null)
    {
        var lockMock = new Mock<IDistributedLock>();
        var handleMock = new Mock<ILockHandle>();
        handleMock.Setup(h => h.ReleaseAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        LockAcquisitionResult acquisition = result ?? LockAcquisitionResult.Acquired(handleMock.Object);

        lockMock.Setup(l => l.TryAcquireAsync(
                It.IsAny<string>(),
                It.IsAny<DistributedLockOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(acquisition);

        var factory = new Mock<IDistributedLockFactory>();
        if (createException != null)
        {
            factory.Setup(f => f.Create()).Throws(createException);
            factory.Setup(f => f.Create(It.IsAny<string>())).Throws(createException);
        }
        else if (!string.IsNullOrWhiteSpace(providerName))
        {
            factory.Setup(f => f.Create(providerName)).Returns(lockMock.Object);
            factory.Setup(f => f.Create()).Returns(lockMock.Object);
        }
        else
        {
            factory.Setup(f => f.Create()).Returns(lockMock.Object);
            factory.Setup(f => f.Create(It.IsAny<string>())).Returns(lockMock.Object);
        }

        return factory;
    }

    public static Mock<ITypedHttpClient<TApi>> CreateTypedHttpClientMock<TApi>(
        HttpResponseMessage? response = null,
        Uri? baseAddress = null,
        Exception? sendException = null)
        where TApi : class
    {
        var handler = new StubHttpMessageHandler(response ?? new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("Healthy")
        });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = baseAddress ?? new Uri("https://api.example.com/")
        };

        var mock = new Mock<ITypedHttpClient<TApi>>();
        mock.SetupGet(c => c.HttpClient).Returns(httpClient);
        mock.SetupGet(c => c.BaseAddress).Returns(httpClient.BaseAddress);
        mock.SetupGet(c => c.Timeout).Returns(TimeSpan.FromSeconds(30));

        if (sendException != null)
        {
            mock.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(sendException);
        }
        else
        {
            mock.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response ?? new HttpResponseMessage(HttpStatusCode.OK));
        }

        return mock;
    }

    private sealed class StubHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(response);
        }
    }
}
