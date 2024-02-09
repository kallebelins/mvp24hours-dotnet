using System;
using System.Net;
using System.Net.Http;

namespace Mvp24Hours.Core.Exceptions
{
    public class HttpStatusCodeException(string message, HttpStatusCode statusCode, HttpMethod method = null, Uri requestUri = null, string responseBody = null) : HttpRequestException(message ?? $"Non-success HTTP status code: {(int)statusCode} {statusCode}.")
    {
        public HttpStatusCodeException(HttpStatusCode statusCode, HttpMethod method = null, Uri requestUri = null)
            : this(null, statusCode, method, requestUri)
        {
        }

        new public HttpStatusCode StatusCode { get; private set; } = statusCode;
        public HttpMethod Method { get; private set; } = method;
        public Uri RequestUri { get; private set; } = requestUri;
        public string ResponseBody { get; set; } = responseBody;
    }
}
