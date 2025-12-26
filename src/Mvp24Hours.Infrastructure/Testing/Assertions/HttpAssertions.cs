//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
#nullable enable
using Mvp24Hours.Infrastructure.Testing.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Mvp24Hours.Infrastructure.Testing.Assertions
{
    /// <summary>
    /// Provides assertion helpers for HTTP requests and responses in tests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These helpers work with <see cref="TestHttpMessageHandler"/> to verify
    /// HTTP requests made during tests.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var handler = new TestHttpMessageHandler();
    /// // ... perform operations ...
    /// 
    /// HttpAssertions.AssertRequestMade(handler, "api/users");
    /// HttpAssertions.AssertRequestCount(handler, 1);
    /// HttpAssertions.AssertGetRequestMade(handler, "api/users/1");
    /// </code>
    /// </example>
    public static class HttpAssertions
    {
        /// <summary>
        /// Asserts that at least one request was made to the handler.
        /// </summary>
        /// <param name="handler">The test HTTP message handler.</param>
        /// <exception cref="AssertionException">Thrown when no requests were made.</exception>
        public static void AssertRequestMade(TestHttpMessageHandler handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            if (handler.RequestCount == 0)
            {
                throw new AssertionException("Expected at least one HTTP request, but none were made.");
            }
        }

        /// <summary>
        /// Asserts that a request was made to a URL containing the specified string.
        /// </summary>
        /// <param name="handler">The test HTTP message handler.</param>
        /// <param name="urlPart">The URL part to search for.</param>
        /// <exception cref="AssertionException">Thrown when no matching request was found.</exception>
        public static void AssertRequestMade(TestHttpMessageHandler handler, string urlPart)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (string.IsNullOrEmpty(urlPart)) throw new ArgumentNullException(nameof(urlPart));

            if (!handler.VerifyRequestUrl(urlPart))
            {
                var urls = string.Join(", ", handler.ReceivedRequests.Select(r => r.RequestUri));
                throw new AssertionException(
                    $"Expected a request to URL containing '{urlPart}', but no such request was found. " +
                    $"Requests made: [{urls}]");
            }
        }

        /// <summary>
        /// Asserts that exactly the specified number of requests were made.
        /// </summary>
        /// <param name="handler">The test HTTP message handler.</param>
        /// <param name="expectedCount">The expected number of requests.</param>
        /// <exception cref="AssertionException">Thrown when the count doesn't match.</exception>
        public static void AssertRequestCount(TestHttpMessageHandler handler, int expectedCount)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            if (handler.RequestCount != expectedCount)
            {
                throw new AssertionException(
                    $"Expected {expectedCount} HTTP request(s), but {handler.RequestCount} were made.");
            }
        }

        /// <summary>
        /// Asserts that a GET request was made to a URL containing the specified string.
        /// </summary>
        public static void AssertGetRequestMade(TestHttpMessageHandler handler, string urlPart)
        {
            AssertRequestWithMethodMade(handler, HttpMethod.Get, urlPart);
        }

        /// <summary>
        /// Asserts that a POST request was made to a URL containing the specified string.
        /// </summary>
        public static void AssertPostRequestMade(TestHttpMessageHandler handler, string urlPart)
        {
            AssertRequestWithMethodMade(handler, HttpMethod.Post, urlPart);
        }

        /// <summary>
        /// Asserts that a PUT request was made to a URL containing the specified string.
        /// </summary>
        public static void AssertPutRequestMade(TestHttpMessageHandler handler, string urlPart)
        {
            AssertRequestWithMethodMade(handler, HttpMethod.Put, urlPart);
        }

        /// <summary>
        /// Asserts that a DELETE request was made to a URL containing the specified string.
        /// </summary>
        public static void AssertDeleteRequestMade(TestHttpMessageHandler handler, string urlPart)
        {
            AssertRequestWithMethodMade(handler, HttpMethod.Delete, urlPart);
        }

        /// <summary>
        /// Asserts that a request with the specified method was made to a URL containing the specified string.
        /// </summary>
        public static void AssertRequestWithMethodMade(TestHttpMessageHandler handler, HttpMethod method, string urlPart)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (method == null) throw new ArgumentNullException(nameof(method));
            if (string.IsNullOrEmpty(urlPart)) throw new ArgumentNullException(nameof(urlPart));

            var found = handler.VerifyRequest(r =>
                r.Method.Equals(method.Method, StringComparison.OrdinalIgnoreCase) &&
                r.RequestUri.Contains(urlPart, StringComparison.OrdinalIgnoreCase));

            if (!found)
            {
                var requests = handler.ReceivedRequests
                    .Select(r => $"{r.Method} {r.RequestUri}")
                    .ToList();

                throw new AssertionException(
                    $"Expected a {method.Method} request to URL containing '{urlPart}', but no such request was found. " +
                    $"Requests made: [{string.Join(", ", requests)}]");
            }
        }

        /// <summary>
        /// Asserts that a request was made with the specified header.
        /// </summary>
        /// <param name="handler">The test HTTP message handler.</param>
        /// <param name="headerName">The header name.</param>
        /// <param name="headerValue">Optional header value to match.</param>
        public static void AssertRequestWithHeader(TestHttpMessageHandler handler, string headerName, string? headerValue = null)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (string.IsNullOrEmpty(headerName)) throw new ArgumentNullException(nameof(headerName));

            var found = handler.VerifyRequest(r =>
            {
                if (!r.HasHeader(headerName)) return false;
                if (headerValue == null) return true;
                return r.GetHeader(headerName)?.Contains(headerValue, StringComparison.OrdinalIgnoreCase) ?? false;
            });

            if (!found)
            {
                throw new AssertionException(
                    headerValue != null
                        ? $"Expected a request with header '{headerName}' containing '{headerValue}', but no such request was found."
                        : $"Expected a request with header '{headerName}', but no such request was found.");
            }
        }

        /// <summary>
        /// Asserts that a request was made with body containing the specified text.
        /// </summary>
        public static void AssertRequestWithBodyContaining(TestHttpMessageHandler handler, string bodyPart)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (string.IsNullOrEmpty(bodyPart)) throw new ArgumentNullException(nameof(bodyPart));

            var found = handler.VerifyRequest(r =>
                r.Body?.Contains(bodyPart, StringComparison.OrdinalIgnoreCase) ?? false);

            if (!found)
            {
                throw new AssertionException(
                    $"Expected a request with body containing '{bodyPart}', but no such request was found.");
            }
        }

        /// <summary>
        /// Asserts that no requests were made to the handler.
        /// </summary>
        public static void AssertNoRequestsMade(TestHttpMessageHandler handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            if (handler.RequestCount > 0)
            {
                var requests = handler.ReceivedRequests
                    .Select(r => $"{r.Method} {r.RequestUri}")
                    .ToList();

                throw new AssertionException(
                    $"Expected no HTTP requests, but {handler.RequestCount} were made: [{string.Join(", ", requests)}]");
            }
        }

        /// <summary>
        /// Gets the last recorded request, throwing if none exist.
        /// </summary>
        public static RecordedRequest GetLastRequest(TestHttpMessageHandler handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var requests = handler.ReceivedRequests;
            if (requests.Count == 0)
            {
                throw new AssertionException("No HTTP requests were recorded.");
            }

            return requests[requests.Count - 1];
        }

        /// <summary>
        /// Gets all recorded requests matching a predicate.
        /// </summary>
        public static IReadOnlyList<RecordedRequest> GetRequestsMatching(
            TestHttpMessageHandler handler,
            Func<RecordedRequest, bool> predicate)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return handler.GetRequests(predicate).ToList().AsReadOnly();
        }
    }
}

