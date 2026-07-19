//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Net;
using Mvp24Hours.Infrastructure.Helpers;
using Polly;

namespace Mvp24Hours.Infrastructure.Test.Helpers;

[Trait("Category", "Unit")]
public class HttpPolicyHelperTest
{
#pragma warning disable CS0618 // Type or member is obsolete
    [Fact]
    public void GetRetryPolicy_ShouldReturnNonNullPolicy()
    {
        // Act
        IAsyncPolicy<HttpResponseMessage> policy =
            HttpPolicyHelper.GetRetryPolicy(HttpStatusCode.TooManyRequests);

        // Assert
        policy.Should().NotBeNull();
    }

    [Fact]
    public void GetTimeoutPolicy_ShouldReturnNonNullPolicy()
    {
        // Act
        IAsyncPolicy<HttpResponseMessage> policy = HttpPolicyHelper.GetTimeoutPolicy();

        // Assert
        policy.Should().NotBeNull();
    }

    [Fact]
    public void GetCircuitBreakerPolicy_ShouldReturnNonNullPolicy()
    {
        // Act
        IAsyncPolicy<HttpResponseMessage> policy =
            HttpPolicyHelper.GetCircuitBreakerPolicy(HttpStatusCode.ServiceUnavailable);

        // Assert
        policy.Should().NotBeNull();
    }
#pragma warning restore CS0618 // Type or member is obsolete
}
