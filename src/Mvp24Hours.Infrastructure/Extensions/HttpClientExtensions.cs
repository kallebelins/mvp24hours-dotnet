//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for HttpClient to simplify HTTP requests.
    /// </summary>
    public static class HttpClientExtensions
    {
        private static ILogger _logger;

        /// <summary>
        /// Sets the logger instance for logging HTTP client operations.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }
        const string METHOD_GET = "GET";
        const string METHOD_POST = "POST";
        const string METHOD_PATCH = "PATCH";
        const string METHOD_PUT = "PUT";
        const string METHOD_DELETE = "DELETE";

        /// <summary>
        /// 
        /// </summary>
        public static Encoding EncodingRequest { get; set; } = Encoding.UTF8;

        /// <summary>
        /// 
        /// </summary>
        public static async Task<string> HttpPostAsync(this HttpClient client, string url = "", string data = "", Dictionary<string, string> headers = null)
        {
            return await HttpSendAsync(client, url, headers, METHOD_POST, data);
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task<T> HttpPostAsync<T>(this HttpClient client, string url = "", string data = "", Dictionary<string, string> headers = null, JsonSerializerSettings jsonSerializerSettings = null)
        {
            var result = await HttpSendAsync(client, url, headers, METHOD_POST, data);
            if (!result.HasValue())
            {
                return default;
            }
            return result.ToDeserialize<T>(jsonSerializerSettings);
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task<string> HttpGetAsync(this HttpClient client, string url = "", Dictionary<string, string> headers = null)
        {
            return await HttpSendAsync(client, url, headers, METHOD_GET, null);
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task<T> HttpGetAsync<T>(this HttpClient client, string url = "", Dictionary<string, string> headers = null, JsonSerializerSettings jsonSerializerSettings = null)
        {
            var result = await HttpSendAsync(client, url, headers, METHOD_GET, null);
            if (!result.HasValue())
            {
                return default;
            }
            return result.ToDeserialize<T>(jsonSerializerSettings);
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task<string> HttpPutAsync(this HttpClient client, string url = "", string data = "", Dictionary<string, string> headers = null)
        {
            return await HttpSendAsync(client, url, headers, METHOD_PUT, data);
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task<T> HttpPutAsync<T>(this HttpClient client, string url = "", string data = "", Dictionary<string, string> headers = null, JsonSerializerSettings jsonSerializerSettings = null)
        {
            var result = await HttpSendAsync(client, url, headers, METHOD_PUT, data);
            if (!result.HasValue())
            {
                return default;
            }
            return result.ToDeserialize<T>(jsonSerializerSettings);
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task<string> HttpPatchAsync(this HttpClient client, string url = "", string data = "", Dictionary<string, string> headers = null)
        {
            return await HttpSendAsync(client, url, headers, METHOD_PATCH, data);
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task<T> HttpPatchAsync<T>(this HttpClient client, string url = "", string data = "", Dictionary<string, string> headers = null, JsonSerializerSettings jsonSerializerSettings = null)
        {
            var result = await HttpSendAsync(client, url, headers, METHOD_PATCH, data);
            if (!result.HasValue())
            {
                return default;
            }
            return result.ToDeserialize<T>(jsonSerializerSettings);
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task<string> HttpDeleteAsync(this HttpClient client, string url = "", Dictionary<string, string> headers = null)
        {
            return await HttpSendAsync(client, url, headers, METHOD_DELETE, null);
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task<T> HttpDeleteAsync<T>(this HttpClient client, string url = "", Dictionary<string, string> headers = null, JsonSerializerSettings jsonSerializerSettings = null)
        {
            var result = await HttpSendAsync(client, url, headers, METHOD_DELETE, null);
            if (!result.HasValue())
            {
                return default;
            }
            return result.ToDeserialize<T>(jsonSerializerSettings);
        }

        public static async Task<string> HttpSendAsync(this HttpClient client, string url, Dictionary<string, string> headers, string method, string data)
        {
            _logger?.LogDebug("Sending {Method} request to {Url}", method, url);
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

                EncodingRequest ??= Encoding.UTF8;

                string urlRequest = $"{client.BaseAddress}{url}";

                if (url.StartsWith("//") || url.Contains(":/") || url.Contains(":\\"))
                    urlRequest = url;

                using var request = new HttpRequestMessage(new HttpMethod(method), new Uri(urlRequest));

                MediaTypeBuilder(headers, method, data, request);

                _logger?.LogDebug("Executing {Method} request to {Url}", method, urlRequest);

                var response = await client.SendAsync(request);

                var responseContent = string.Empty;

                if (response.Content != null)
                {
                    responseContent = await response.Content.ReadAsStringAsync();
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogWarning("HTTP request failed with status {StatusCode} {ReasonPhrase} for {Method} {Url}",
                        response.StatusCode, response.ReasonPhrase, method, urlRequest);
                    throw new HttpStatusCodeException(response.ReasonPhrase, response.StatusCode, request.Method, request.RequestUri, responseContent);
                }

                response.EnsureSuccessStatusCode();

                _logger?.LogDebug("Successfully received response from {Url} with status {StatusCode}", urlRequest, response.StatusCode);

                return responseContent;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing {Method} request to {Url}: {ErrorMessage}", method, url, ex.Message);
                throw;
            }
        }

        private static void MediaTypeBuilder(Dictionary<string, string> headers, string method, string data, HttpRequestMessage request)
        {
            string mediaType = string.Empty;

            if (headers.AnyOrNotNull())
            {
                foreach (var keyValue in headers)
                {
                    if (!keyValue.Key.HasValue() || !keyValue.Value.HasValue())
                        continue;
                    if (keyValue.Key == "Content-Type")
                    {
                        mediaType = keyValue.Value.Split(';').ElementAtOrDefault(0);
                    }
                    request.Headers.TryAddWithoutValidation(keyValue.Key, keyValue.Value);
                }
            }

            if (!mediaType.HasValue())
            {
                mediaType = "application/json";
            }

            if (!headers.ContainsKeySafe("Content-Type"))
            {
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(mediaType));
                request.Headers.TryAddWithoutValidation("Content-Type", $"{mediaType}; charset={EncodingRequest.BodyName.ToLower()}");
            }

            if (method == "POST" || method == "PUT" || method == "PATCH")
            {
                request.Headers.TryAddWithoutValidation("Content-Length", EncodingRequest.GetBytes(data ?? string.Empty).Length.ToString());
                request.Content = new StringContent(data, EncodingRequest);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mediaType);
            }
        }

        public static HttpClient PropagateHeaderKey(this HttpClient c, IServiceCollection services, params string[] keys)
        {
            var serviceProvider = services.BuildServiceProvider();
            c.PropagateHeaderKey(serviceProvider, keys);
            return c;
        }

        public static HttpClient PropagateHeaderKey(this HttpClient c, IServiceProvider serviceProvider, params string[] keys)
        {
            var httpAccessor = serviceProvider.GetService<IHttpContextAccessor>();
            c.PropagateHeaderKey(httpAccessor, keys);
            return c;
        }

        public static HttpClient PropagateHeaderKey(this HttpClient c, IHttpContextAccessor httpAccessor, params string[] keys)
        {
            foreach (var key in keys)
            {
                string headerValue = httpAccessor.GetHeaderValue(key);
                if (headerValue != null)
                    c.DefaultRequestHeaders.TryAddWithoutValidation(key, headerValue);
            }
            return c;
        }

        public static HttpRequestMessage PropagateHeaderKey(this HttpRequestMessage request, IServiceCollection services, params string[] keys)
        {
            var serviceProvider = services.BuildServiceProvider();
            request.PropagateHeaderKey(serviceProvider, keys);
            return request;
        }

        public static HttpRequestMessage PropagateHeaderKey(this HttpRequestMessage request, IServiceProvider serviceProvider, params string[] keys)
        {
            var httpAccessor = serviceProvider.GetService<IHttpContextAccessor>();
            request.PropagateHeaderKey(httpAccessor, keys);
            return request;
        }

        public static HttpRequestMessage PropagateHeaderKey(this HttpRequestMessage request, IHttpContextAccessor httpAccessor, params string[] keys)
        {
            if (httpAccessor?.HttpContext != null)
            {
                foreach (var key in keys)
                {
                    var headers = httpAccessor.HttpContext.Request.Headers;
                    var headerValue = headers.GetHeaderValue(key);
                    if (headerValue.HasValue())
                        request.Headers.TryAddWithoutValidation(key, headerValue);
                }
            }
            return request;
        }

        public static string GetHeaderValue(this IHttpContextAccessor httpAccessor, string key)
        {
            if (httpAccessor?.HttpContext != null)
            {
                return httpAccessor.HttpContext.Request.Headers.GetHeaderValue(key);
            }
            return null;
        }

        public static string GetHeaderValue(this IHeaderDictionary headers, string key)
        {
            if (headers.AnyOrNotNull() && headers.TryGetValue(key, out Microsoft.Extensions.Primitives.StringValues value) && !string.IsNullOrEmpty(value))
            {
                return value.ToString();
            }
            return null;
        }

        public static string GetQueryStringFrom<T>(this T model)
        {
            if (model == null) return null;
            return WebRequestHelper.ToQueryString(model);
        }

        public static T GetFromQueryString<T>(this HttpRequest request)
           where T : class
        {
            if (request == null) return null;
            return GetFromQueryString<T>(request.QueryString.Value);
        }

        public static T GetFromQueryString<T>(this string queryString)
            where T : class
        {
            if (!queryString.HasValue()) return null;
            var dict = HttpUtility.ParseQueryString(queryString);
            string json = JsonConvert.SerializeObject(dict.Cast<string>().ToDictionary(k => k, v => dict[v]));
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
