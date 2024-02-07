//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Extensions;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Mvp24Hours.Helpers
{
    /// <summary>
    /// Contains functions for web requests
    /// </summary>
    public static class WebRequestHelper
    {
        /// <summary>
        /// 
        /// </summary>
        public static Encoding EncodingRequest { get; set; } = Encoding.UTF8;

        /// <summary>
        /// 
        /// </summary>
        public static string ToQueryString(params object[] objs)
        {
            if (objs == null)
            {
                return string.Empty;
            }

            var result = new List<string>();
            foreach (var obj in objs)
            {
                var props = obj
                    .GetType()
                    .GetProperties()
                    .Where(p => p.GetValue(obj, null) != null);

                foreach (var p in props)
                {
                    var value = p.GetValue(obj, null);
                    if (value is ICollection enumerable)
                    {
                        result.AddRange(from object v in enumerable select string.Format("{0}={1}", p.Name, HttpUtility.UrlEncode(v.ToString())));
                    }
                    else
                    {
                        result.Add(string.Format("{0}={1}", p.Name, HttpUtility.UrlEncode(value.ToString())));
                    }
                }
            }
#pragma warning disable IDE0305 // Simplify collection initialization
            return string.Join("&", result.ToArray());
#pragma warning restore IDE0305 // Simplify collection initialization
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task<string> PostAsync(string urlService, string data = "", IDictionary<string, string> headers = null, ICredentials credentials = null)
        {
            return await SendAsync(urlService, headers, credentials, "POST", data);
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task<T> PostAsync<T>(string urlService, string data = "", IDictionary<string, string> headers = null, ICredentials credentials = null, JsonSerializerSettings jsonSerializerSettings = null)
        {
            var result = await SendAsync(urlService, headers, credentials, "POST", data);
            if (!result.HasValue())
            {
                return default;
            }

            return result.ToDeserialize<T>(jsonSerializerSettings);
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task<string> GetAsync(string url, IDictionary<string, string> headers = null, ICredentials credentials = null)
        {
            return await SendAsync(url, headers, credentials, "GET", null);
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task<T> GetAsync<T>(string url, IDictionary<string, string> headers = null, ICredentials credentials = null, JsonSerializerSettings jsonSerializerSettings = null)
        {
            var result = await SendAsync(url, headers, credentials, "GET", null);
            if (!result.HasValue())
            {
                return default;
            }
            return result.ToDeserialize<T>(jsonSerializerSettings);
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task<string> PutAsync(string urlService, string data = "", IDictionary<string, string> headers = null, ICredentials credentials = null)
        {
            return await SendAsync(urlService, headers, credentials, "PUT", data);
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task<T> PutAsync<T>(string urlService, string data = "", IDictionary<string, string> headers = null, ICredentials credentials = null, JsonSerializerSettings jsonSerializerSettings = null)
        {
            var result = await SendAsync(urlService, headers, credentials, "PUT", data);
            if (!result.HasValue())
            {
                return default;
            }
            return result.ToDeserialize<T>(jsonSerializerSettings);
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task<string> PatchAsync(string urlService, string data = "", IDictionary<string, string> headers = null, ICredentials credentials = null)
        {
            return await SendAsync(urlService, headers, credentials, "PATCH", data);
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task<T> PatchAsync<T>(string urlService, string data = "", IDictionary<string, string> headers = null, ICredentials credentials = null, JsonSerializerSettings jsonSerializerSettings = null)
        {
            var result = await SendAsync(urlService, headers, credentials, "PATCH", data);
            if (!result.HasValue())
            {
                return default;
            }
            return result.ToDeserialize<T>(jsonSerializerSettings);
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task<string> DeleteAsync(string url, IDictionary<string, string> headers = null, ICredentials credentials = null)
        {
            return await SendAsync(url, headers, credentials, "DELETE", null);
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task<T> DeleteAsync<T>(string url, IDictionary<string, string> headers = null, ICredentials credentials = null, JsonSerializerSettings jsonSerializerSettings = null)
        {
            var result = await SendAsync(url, headers, credentials, "DELETE", null);
            if (!result.HasValue())
            {
                return default;
            }
            return result.ToDeserialize<T>(jsonSerializerSettings);
        }

        private static async Task<string> SendAsync(string url, IDictionary<string, string> headers, ICredentials credentials, string method, string data)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "infra-webrequesthelper-start");
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

                EncodingRequest ??= Encoding.UTF8;

#pragma warning disable SYSLIB0014 // Type or member is obsolete
                HttpWebRequest client = (HttpWebRequest)WebRequest.Create(url);
#pragma warning restore SYSLIB0014 // Type or member is obsolete
                client.Method = method;
                client.Timeout = 300000;

                if (credentials != null)
                    client.Credentials = credentials;

                if (headers.AnyOrNotNull())
                {
                    foreach (var keyValue in headers)
                    {
                        client.Headers.Add(keyValue.Key.ToString(), keyValue.Value.ToString());
                    }
                }

                if (!headers.ContainsKeySafe("Content-Type"))
                    client.ContentType = $"application/json; charset={EncodingRequest.BodyName.ToLower()}";

                if (!headers.ContainsKeySafe("Accept-Encoding"))
                {
                    client.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                    client.Headers.Add("Accept-Encoding", "gzip,deflate");
                }

                byte[] bytes = null;
                if (method == "POST" || method == "PUT" || method == "PATCH")
                {
                    bytes = EncodingRequest.GetBytes(data ?? string.Empty);
                    client.ContentLength = bytes.Length;
                }

                try
                {
                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "infra-webrequesthelper-sendasync", $"url:{url}|method:{method}");

                    if (bytes == null)
                    {
                        using var response = client.GetResponse();
                        using var content = response.GetResponseStream();
                        using var reader = new StreamReader(content, EncodingRequest);
                        return await reader.ReadToEndAsync();
                    }
                    else
                    {
                        using var reqstream = client.GetRequestStream();
                        reqstream.Write(bytes, 0, bytes.Length);
                        var httpResponse = (HttpWebResponse)client.GetResponse();
                        using var streamReader = new StreamReader(httpResponse.GetResponseStream(), EncodingRequest);
                        return await streamReader.ReadToEndAsync();
                    }
                }
                catch (WebException we)
                {
                    TelemetryHelper.Execute(TelemetryLevels.Error, "infra-webrequesthelper-failure", we);
                    if (we.Response != null)
                    {
                        using var stream = we.Response.GetResponseStream();
                        using var reader = new StreamReader(stream);
                        return reader.ReadToEnd();
                    }
                    throw;
                }
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "infra-webrequesthelper-end");
            }
        }
    }
}
