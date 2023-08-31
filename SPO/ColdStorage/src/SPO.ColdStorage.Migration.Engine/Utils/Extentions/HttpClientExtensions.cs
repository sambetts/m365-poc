﻿using SPO.ColdStorage.Migration.Engine.Utils.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SPO.ColdStorage.Migration.Engine.Utils
{
    public static class HttpClientExtensions
    {
        public static async Task<HttpResponseMessage> GetAsyncWithThrottleRetries(this SecureSPThrottledHttpClient httpClient, string url, DebugTracer debugTracer)
        {
            // Default to return when full content is read
            return await GetAsyncWithThrottleRetries(httpClient, url, HttpCompletionOption.ResponseContentRead, debugTracer);
        }
        public static async Task<HttpResponseMessage> GetAsyncWithThrottleRetries(this SecureSPThrottledHttpClient httpClient, string url, HttpCompletionOption completionOption, DebugTracer debugTracer)
        {
            if (httpClient is null)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }

            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException($"'{nameof(url)}' cannot be null or empty.", nameof(url));
            }

            if (debugTracer is null)
            {
                throw new ArgumentNullException(nameof(debugTracer));
            }

            var response = await httpClient.ExecuteHttpCallWithThrottleRetries(async () => await httpClient.GetAsync(url, completionOption), url);


            return response!;
        }

        public static async Task<HttpResponseMessage> PostAsyncWithThrottleRetries(this SecureSPThrottledHttpClient httpClient, string url, object body, DebugTracer debugTracer)
        {
            if (httpClient is null)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }

            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException($"'{nameof(url)}' cannot be null or empty.", nameof(url));
            }

            if (debugTracer is null)
            {
                throw new ArgumentNullException(nameof(debugTracer));
            }

            var payload = JsonSerializer.Serialize(body);
            var httpContent = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.ExecuteHttpCallWithThrottleRetries(async () => await httpClient.PostAsync(url, httpContent), url);

            return response;
        }


        public static async Task<HttpResponseMessage> PostAsyncWithThrottleRetries(this SecureSPThrottledHttpClient httpClient, string url, string bodyContent, string mimeType, string boundary, DebugTracer debugTracer)
        {
            if (httpClient is null)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }

            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException($"'{nameof(url)}' cannot be null or empty.", nameof(url));
            }

            if (debugTracer is null)
            {
                throw new ArgumentNullException(nameof(debugTracer));
            }

            var body = new StringContent(bodyContent);
            var header = new MediaTypeHeaderValue(mimeType);
            header.Parameters.Add(new NameValueHeaderValue("boundary", boundary));
            body.Headers.ContentType = header;

            var response = await httpClient.ExecuteHttpCallWithThrottleRetries(async () => await httpClient.PostAsync(url, body), url);

            return response;
        }

        public static int? GetRetryAfterHeaderSeconds(this HttpResponseMessage response)
        {
            int responseWaitVal = 0;
            response.Headers.TryGetValues("Retry-After", out var r);

            if (r != null)
                foreach (var retryAfterHeaderVal in r)
                {
                    if (int.TryParse(retryAfterHeaderVal, out responseWaitVal))
                    {
                        return responseWaitVal;
                    }
                }

            return null;
        }
    }
}
