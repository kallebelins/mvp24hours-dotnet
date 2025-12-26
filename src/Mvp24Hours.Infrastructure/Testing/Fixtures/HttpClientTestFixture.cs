//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
#nullable enable
using Mvp24Hours.Infrastructure.Testing.Http;
using System;
using System.Net;
using System.Net.Http;

namespace Mvp24Hours.Infrastructure.Testing.Fixtures
{
    /// <summary>
    /// Test fixture specifically for HTTP client testing scenarios.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This fixture provides a lightweight setup for testing HTTP client
    /// interactions without the full infrastructure setup.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class MyHttpTests : IClassFixture&lt;HttpClientTestFixture&gt;
    /// {
    ///     private readonly HttpClientTestFixture _fixture;
    ///     
    ///     public MyHttpTests(HttpClientTestFixture fixture)
    ///     {
    ///         _fixture = fixture;
    ///         _fixture.Reset();
    ///     }
    ///     
    ///     [Fact]
    ///     public async Task TestApiCall()
    ///     {
    ///         _fixture.SetupGetResponse("/api/users/1", new { id = 1, name = "Test" });
    ///         
    ///         var client = _fixture.CreateClient();
    ///         var response = await client.GetAsync("https://api.example.com/api/users/1");
    ///         
    ///         Assert.True(response.IsSuccessStatusCode);
    ///     }
    /// }
    /// </code>
    /// </example>
    public class HttpClientTestFixture : IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// Gets the test HTTP message handler.
        /// </summary>
        public TestHttpMessageHandler Handler { get; }

        /// <summary>
        /// Gets or sets the base address for created HTTP clients.
        /// </summary>
        public Uri? BaseAddress { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientTestFixture"/> class.
        /// </summary>
        public HttpClientTestFixture()
        {
            Handler = new TestHttpMessageHandler();
        }

        /// <summary>
        /// Creates a new HttpClient using the test handler.
        /// </summary>
        /// <returns>A configured HttpClient.</returns>
        public HttpClient CreateClient()
        {
            var client = new HttpClient(Handler);
            if (BaseAddress != null)
            {
                client.BaseAddress = BaseAddress;
            }
            return client;
        }

        /// <summary>
        /// Creates a new HttpClient with a specific base address.
        /// </summary>
        /// <param name="baseAddress">The base address for the client.</param>
        /// <returns>A configured HttpClient.</returns>
        public HttpClient CreateClient(string baseAddress)
        {
            var client = new HttpClient(Handler)
            {
                BaseAddress = new Uri(baseAddress)
            };
            return client;
        }

        /// <summary>
        /// Resets the handler state.
        /// </summary>
        public void Reset()
        {
            Handler.ClearRequests();
            Handler.ClearMatchers();
        }

        /// <summary>
        /// Sets up a response for GET requests to the specified URL path.
        /// </summary>
        /// <param name="urlPath">The URL path to match.</param>
        /// <param name="content">The content to return.</param>
        /// <param name="statusCode">The HTTP status code.</param>
        public void SetupGetResponse(string urlPath, object content, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            Handler.WhenGet(urlPath, statusCode, content);
        }

        /// <summary>
        /// Sets up a response for POST requests to the specified URL path.
        /// </summary>
        /// <param name="urlPath">The URL path to match.</param>
        /// <param name="content">The content to return.</param>
        /// <param name="statusCode">The HTTP status code.</param>
        public void SetupPostResponse(string urlPath, object? content = null, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            Handler.WhenPost(urlPath, statusCode, content);
        }

        /// <summary>
        /// Sets up a response for PUT requests to the specified URL path.
        /// </summary>
        /// <param name="urlPath">The URL path to match.</param>
        /// <param name="content">The content to return.</param>
        /// <param name="statusCode">The HTTP status code.</param>
        public void SetupPutResponse(string urlPath, object? content = null, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            Handler.WhenPut(urlPath, statusCode, content);
        }

        /// <summary>
        /// Sets up a response for DELETE requests to the specified URL path.
        /// </summary>
        /// <param name="urlPath">The URL path to match.</param>
        /// <param name="statusCode">The HTTP status code.</param>
        public void SetupDeleteResponse(string urlPath, HttpStatusCode statusCode = HttpStatusCode.NoContent)
        {
            Handler.WhenDelete(urlPath, statusCode);
        }

        /// <summary>
        /// Sets up a 404 Not Found response for the specified URL path.
        /// </summary>
        /// <param name="urlPath">The URL path to match.</param>
        public void SetupNotFound(string urlPath)
        {
            Handler.WhenUrl(urlPath, HttpStatusCode.NotFound, new { error = "Not found" });
        }

        /// <summary>
        /// Sets up a 500 Internal Server Error response for the specified URL path.
        /// </summary>
        /// <param name="urlPath">The URL path to match.</param>
        /// <param name="errorMessage">Optional error message.</param>
        public void SetupServerError(string urlPath, string errorMessage = "Internal server error")
        {
            Handler.WhenUrl(urlPath, HttpStatusCode.InternalServerError, new { error = errorMessage });
        }

        /// <summary>
        /// Sets up a timeout simulation for all requests.
        /// </summary>
        /// <param name="delay">Optional delay before timeout.</param>
        public void SetupTimeout(TimeSpan? delay = null)
        {
            Handler.SimulateTimeout(delay);
        }

        /// <summary>
        /// Sets up network failure simulation for all requests.
        /// </summary>
        public void SetupNetworkFailure()
        {
            Handler.SimulateNetworkFailure();
        }

        /// <summary>
        /// Verifies that a request was made to the specified URL.
        /// </summary>
        /// <param name="urlPath">The URL path to check.</param>
        /// <returns>True if a matching request was made.</returns>
        public bool VerifyRequestMade(string urlPath)
        {
            return Handler.VerifyRequestUrl(urlPath);
        }

        /// <summary>
        /// Gets the total number of requests made.
        /// </summary>
        public int RequestCount => Handler.RequestCount;

        /// <summary>
        /// Disposes of the fixture resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the fixture resources.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Handler.Dispose();
                }
                _disposed = true;
            }
        }
    }
}

