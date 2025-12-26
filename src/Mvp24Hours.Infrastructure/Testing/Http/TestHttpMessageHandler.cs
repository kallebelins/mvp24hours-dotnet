//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Testing.Http
{
    /// <summary>
    /// A configurable HTTP message handler for mocking HTTP requests in tests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This handler allows you to:
    /// - Define expected responses for specific request patterns
    /// - Simulate network errors and timeouts
    /// - Track all requests made during tests
    /// - Verify request content and headers
    /// </para>
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// var handler = new TestHttpMessageHandler();
    /// handler.RespondWith(HttpStatusCode.OK, new { id = 1, name = "Test" });
    /// 
    /// var client = new HttpClient(handler);
    /// var response = await client.GetAsync("https://api.example.com/users/1");
    /// 
    /// Assert.Single(handler.ReceivedRequests);
    /// </code>
    /// </para>
    /// </remarks>
    public class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly List<RequestMatcher> _requestMatchers = new();
        private readonly List<RecordedRequest> _receivedRequests = new();
        private readonly object _lock = new();

        private HttpResponseMessage _defaultResponse = new(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

        /// <summary>
        /// Gets all requests that have been received by this handler.
        /// </summary>
        public IReadOnlyList<RecordedRequest> ReceivedRequests
        {
            get
            {
                lock (_lock)
                {
                    return _receivedRequests.ToList().AsReadOnly();
                }
            }
        }

        /// <summary>
        /// Gets the count of requests received.
        /// </summary>
        public int RequestCount
        {
            get
            {
                lock (_lock)
                {
                    return _receivedRequests.Count;
                }
            }
        }

        #region Configuration Methods

        /// <summary>
        /// Configures the handler to respond with a specific HTTP status code.
        /// </summary>
        /// <param name="statusCode">The HTTP status code to return.</param>
        /// <returns>This handler for method chaining.</returns>
        public TestHttpMessageHandler RespondWith(HttpStatusCode statusCode)
        {
            _defaultResponse = new HttpResponseMessage(statusCode);
            return this;
        }

        /// <summary>
        /// Configures the handler to respond with a specific HTTP status code and content.
        /// </summary>
        /// <param name="statusCode">The HTTP status code to return.</param>
        /// <param name="content">The content to return (will be JSON serialized).</param>
        /// <returns>This handler for method chaining.</returns>
        public TestHttpMessageHandler RespondWith(HttpStatusCode statusCode, object content)
        {
            var json = JsonSerializer.Serialize(content);
            _defaultResponse = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            return this;
        }

        /// <summary>
        /// Configures the handler to respond with a specific HTTP status code and string content.
        /// </summary>
        /// <param name="statusCode">The HTTP status code to return.</param>
        /// <param name="content">The string content to return.</param>
        /// <param name="mediaType">The media type of the content.</param>
        /// <returns>This handler for method chaining.</returns>
        public TestHttpMessageHandler RespondWith(HttpStatusCode statusCode, string content, string mediaType = "application/json")
        {
            _defaultResponse = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, mediaType)
            };
            return this;
        }

        /// <summary>
        /// Configures the handler to respond with a specific HttpResponseMessage.
        /// </summary>
        /// <param name="response">The response message to return.</param>
        /// <returns>This handler for method chaining.</returns>
        public TestHttpMessageHandler RespondWith(HttpResponseMessage response)
        {
            _defaultResponse = response ?? throw new ArgumentNullException(nameof(response));
            return this;
        }

        /// <summary>
        /// Adds a request matcher for conditional responses.
        /// </summary>
        /// <param name="predicate">The predicate to match requests.</param>
        /// <param name="response">The response to return for matching requests.</param>
        /// <returns>This handler for method chaining.</returns>
        public TestHttpMessageHandler When(
            Func<HttpRequestMessage, bool> predicate,
            HttpResponseMessage response)
        {
            lock (_lock)
            {
                _requestMatchers.Add(new RequestMatcher(predicate, _ => Task.FromResult(response)));
            }
            return this;
        }

        /// <summary>
        /// Adds a request matcher for conditional responses with async response factory.
        /// </summary>
        /// <param name="predicate">The predicate to match requests.</param>
        /// <param name="responseFactory">The async factory to create responses.</param>
        /// <returns>This handler for method chaining.</returns>
        public TestHttpMessageHandler When(
            Func<HttpRequestMessage, bool> predicate,
            Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory)
        {
            lock (_lock)
            {
                _requestMatchers.Add(new RequestMatcher(predicate, responseFactory));
            }
            return this;
        }

        /// <summary>
        /// Configures a response for requests to a specific URL.
        /// </summary>
        /// <param name="url">The URL to match (partial match supported).</param>
        /// <param name="statusCode">The status code to return.</param>
        /// <param name="content">Optional content to return.</param>
        /// <returns>This handler for method chaining.</returns>
        public TestHttpMessageHandler WhenUrl(string url, HttpStatusCode statusCode, object? content = null)
        {
            var response = new HttpResponseMessage(statusCode);
            if (content != null)
            {
                var json = JsonSerializer.Serialize(content);
                response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            return When(
                req => req.RequestUri?.ToString().Contains(url, StringComparison.OrdinalIgnoreCase) ?? false,
                response);
        }

        /// <summary>
        /// Configures a response for GET requests to a specific URL.
        /// </summary>
        public TestHttpMessageHandler WhenGet(string url, HttpStatusCode statusCode, object? content = null)
        {
            return When(
                req => req.Method == HttpMethod.Get &&
                       (req.RequestUri?.ToString().Contains(url, StringComparison.OrdinalIgnoreCase) ?? false),
                CreateResponse(statusCode, content));
        }

        /// <summary>
        /// Configures a response for POST requests to a specific URL.
        /// </summary>
        public TestHttpMessageHandler WhenPost(string url, HttpStatusCode statusCode, object? content = null)
        {
            return When(
                req => req.Method == HttpMethod.Post &&
                       (req.RequestUri?.ToString().Contains(url, StringComparison.OrdinalIgnoreCase) ?? false),
                CreateResponse(statusCode, content));
        }

        /// <summary>
        /// Configures a response for PUT requests to a specific URL.
        /// </summary>
        public TestHttpMessageHandler WhenPut(string url, HttpStatusCode statusCode, object? content = null)
        {
            return When(
                req => req.Method == HttpMethod.Put &&
                       (req.RequestUri?.ToString().Contains(url, StringComparison.OrdinalIgnoreCase) ?? false),
                CreateResponse(statusCode, content));
        }

        /// <summary>
        /// Configures a response for DELETE requests to a specific URL.
        /// </summary>
        public TestHttpMessageHandler WhenDelete(string url, HttpStatusCode statusCode, object? content = null)
        {
            return When(
                req => req.Method == HttpMethod.Delete &&
                       (req.RequestUri?.ToString().Contains(url, StringComparison.OrdinalIgnoreCase) ?? false),
                CreateResponse(statusCode, content));
        }

        /// <summary>
        /// Configures the handler to throw an exception for all requests.
        /// </summary>
        /// <param name="exception">The exception to throw.</param>
        /// <returns>This handler for method chaining.</returns>
        public TestHttpMessageHandler ThrowException(Exception exception)
        {
            return When(
                _ => true,
                _ => throw exception);
        }

        /// <summary>
        /// Configures the handler to simulate a timeout.
        /// </summary>
        /// <param name="delay">The delay before throwing the timeout exception.</param>
        /// <returns>This handler for method chaining.</returns>
        public TestHttpMessageHandler SimulateTimeout(TimeSpan? delay = null)
        {
            return When(
                _ => true,
                async req =>
                {
                    if (delay.HasValue)
                    {
                        await Task.Delay(delay.Value);
                    }
                    throw new TaskCanceledException("Request timed out.");
                });
        }

        /// <summary>
        /// Configures the handler to simulate network failure.
        /// </summary>
        /// <returns>This handler for method chaining.</returns>
        public TestHttpMessageHandler SimulateNetworkFailure()
        {
            return ThrowException(new HttpRequestException("Network failure simulated."));
        }

        #endregion

        #region Verification Methods

        /// <summary>
        /// Clears all recorded requests.
        /// </summary>
        public void ClearRequests()
        {
            lock (_lock)
            {
                _receivedRequests.Clear();
            }
        }

        /// <summary>
        /// Clears all request matchers.
        /// </summary>
        public void ClearMatchers()
        {
            lock (_lock)
            {
                _requestMatchers.Clear();
            }
        }

        /// <summary>
        /// Verifies that a request with the specified predicate was received.
        /// </summary>
        /// <param name="predicate">The predicate to match requests.</param>
        /// <returns>True if a matching request was received; otherwise, false.</returns>
        public bool VerifyRequest(Func<RecordedRequest, bool> predicate)
        {
            lock (_lock)
            {
                return _receivedRequests.Any(predicate);
            }
        }

        /// <summary>
        /// Verifies that a request to the specified URL was received.
        /// </summary>
        /// <param name="url">The URL to check (partial match).</param>
        /// <returns>True if a matching request was received; otherwise, false.</returns>
        public bool VerifyRequestUrl(string url)
        {
            return VerifyRequest(r => r.RequestUri?.Contains(url, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        /// <summary>
        /// Gets all requests that match the specified predicate.
        /// </summary>
        /// <param name="predicate">The predicate to match requests.</param>
        /// <returns>The matching requests.</returns>
        public IEnumerable<RecordedRequest> GetRequests(Func<RecordedRequest, bool> predicate)
        {
            lock (_lock)
            {
                return _receivedRequests.Where(predicate).ToList();
            }
        }

        /// <summary>
        /// Gets all GET requests.
        /// </summary>
        public IEnumerable<RecordedRequest> GetGetRequests() => GetRequests(r => r.Method == "GET");

        /// <summary>
        /// Gets all POST requests.
        /// </summary>
        public IEnumerable<RecordedRequest> GetPostRequests() => GetRequests(r => r.Method == "POST");

        /// <summary>
        /// Gets all PUT requests.
        /// </summary>
        public IEnumerable<RecordedRequest> GetPutRequests() => GetRequests(r => r.Method == "PUT");

        /// <summary>
        /// Gets all DELETE requests.
        /// </summary>
        public IEnumerable<RecordedRequest> GetDeleteRequests() => GetRequests(r => r.Method == "DELETE");

        #endregion

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Record the request
            var recordedRequest = await RecordRequestAsync(request, cancellationToken);

            lock (_lock)
            {
                _receivedRequests.Add(recordedRequest);
            }

            // Find matching response
            RequestMatcher? matchingMatcher;
            lock (_lock)
            {
                matchingMatcher = _requestMatchers.FirstOrDefault(m => m.Predicate(request));
            }

            if (matchingMatcher != null)
            {
                return await matchingMatcher.ResponseFactory(request);
            }

            // Return default response (create new instance to avoid reuse issues)
            return CloneResponse(_defaultResponse);
        }

        private static async Task<RecordedRequest> RecordRequestAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string? body = null;
            if (request.Content != null)
            {
                body = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            var headers = request.Headers
                .ToDictionary(h => h.Key, h => string.Join(", ", h.Value));

            if (request.Content?.Headers != null)
            {
                foreach (var header in request.Content.Headers)
                {
                    headers[header.Key] = string.Join(", ", header.Value);
                }
            }

            return new RecordedRequest(
                request.Method.Method,
                request.RequestUri?.ToString() ?? string.Empty,
                headers,
                body,
                DateTimeOffset.UtcNow);
        }

        private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, object? content)
        {
            var response = new HttpResponseMessage(statusCode);
            if (content != null)
            {
                var json = JsonSerializer.Serialize(content);
                response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            return response;
        }

        private static HttpResponseMessage CloneResponse(HttpResponseMessage original)
        {
            var clone = new HttpResponseMessage(original.StatusCode)
            {
                ReasonPhrase = original.ReasonPhrase,
                Version = original.Version
            };

            if (original.Content != null)
            {
                var contentString = original.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var mediaType = original.Content.Headers.ContentType?.MediaType ?? "application/json";
                clone.Content = new StringContent(contentString, Encoding.UTF8, mediaType);
            }

            foreach (var header in original.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clone;
        }

        private sealed class RequestMatcher
        {
            public Func<HttpRequestMessage, bool> Predicate { get; }
            public Func<HttpRequestMessage, Task<HttpResponseMessage>> ResponseFactory { get; }

            public RequestMatcher(
                Func<HttpRequestMessage, bool> predicate,
                Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory)
            {
                Predicate = predicate;
                ResponseFactory = responseFactory;
            }
        }
    }

    /// <summary>
    /// Represents a recorded HTTP request for testing verification.
    /// </summary>
    public sealed class RecordedRequest
    {
        /// <summary>
        /// Gets the HTTP method of the request.
        /// </summary>
        public string Method { get; }

        /// <summary>
        /// Gets the URI of the request.
        /// </summary>
        public string RequestUri { get; }

        /// <summary>
        /// Gets the headers of the request.
        /// </summary>
        public IReadOnlyDictionary<string, string> Headers { get; }

        /// <summary>
        /// Gets the body content of the request.
        /// </summary>
        public string? Body { get; }

        /// <summary>
        /// Gets the timestamp when the request was recorded.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecordedRequest"/> class.
        /// </summary>
        public RecordedRequest(
            string method,
            string requestUri,
            IReadOnlyDictionary<string, string> headers,
            string? body,
            DateTimeOffset timestamp)
        {
            Method = method;
            RequestUri = requestUri;
            Headers = headers;
            Body = body;
            Timestamp = timestamp;
        }

        /// <summary>
        /// Deserializes the body content as the specified type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <returns>The deserialized object, or default if body is null.</returns>
        public T? GetBodyAs<T>()
        {
            if (string.IsNullOrEmpty(Body))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(Body);
        }

        /// <summary>
        /// Gets a header value by name.
        /// </summary>
        /// <param name="name">The header name.</param>
        /// <returns>The header value, or null if not found.</returns>
        public string? GetHeader(string name)
        {
            return Headers.TryGetValue(name, out var value) ? value : null;
        }

        /// <summary>
        /// Checks if the request has a specific header.
        /// </summary>
        /// <param name="name">The header name.</param>
        /// <returns>True if the header exists; otherwise, false.</returns>
        public bool HasHeader(string name) => Headers.ContainsKey(name);
    }
}

