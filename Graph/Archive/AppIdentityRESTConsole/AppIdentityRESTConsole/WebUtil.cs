using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace AppIdentityRESTConsole
{
    /// <summary>
    /// Utility class for getting HTTP responses
    /// </summary>
    public class WebUtil
    {
        /// <summary>
        /// POST 'x-www-form-urlencoded' data somewhere
        /// </summary>
        internal async static Task<string> Post(string url, string body)
        {
            HttpClient client = new HttpClient();

            StringContent bodyContent = new StringContent(body);
            bodyContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            // Get response body
            HttpResponseMessage response = await client.PostAsync(url, bodyContent);

            // Read response
            return await GetResponseBodyAndCheckIsValid(response);
        }

        /// <summary>
        /// POST 'x-www-form-urlencoded' data somewhere
        /// </summary>
        internal async static Task<string> Post(string url, string json, string bearer)
        {
            HttpClient client = new HttpClient();

            if (!string.IsNullOrEmpty(bearer))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            }

            StringContent bodyContent = new StringContent(json);
            bodyContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            // Get response body
            HttpResponseMessage response = await client.PostAsync(url, bodyContent);

            // Read response
            return await GetResponseBodyAndCheckIsValid(response);
        }

        /// <summary>
        /// GET a URL. No body payload.
        /// </summary>
        internal async static Task<string> Get(string url, string bearer)
        {
            HttpClient c = new HttpClient();

            if (!string.IsNullOrEmpty(bearer))
            {
                c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            }
            
            // Get response body
            HttpResponseMessage response = c.GetAsync(url).Result;

            // Make sure it worked
            return await GetResponseBodyAndCheckIsValid(response);
        }

        static async Task<string> GetResponseBodyAndCheckIsValid(HttpResponseMessage response)
        {
            string result = await response.Content.ReadAsStringAsync();
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException)
            {
                Console.WriteLine("Got unexpected error:");
                Console.WriteLine(result);
                throw;
            }

            return result;
        }
    }
}
