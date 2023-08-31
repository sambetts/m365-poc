using SPO.ColdStorage.Migration.Engine.Utils.Http;
using SPO.ColdStorage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SPO.ColdStorage.Migration.Engine
{
    public static class GraphLoader
    {
        public static async Task<List<P>> LoadGraphPageable<T, P>(this AutoThrottleHttpClient httpClient, string url, DebugTracer debugTracer) where T : GraphPageableResponse<P> where P : BaseGraphObject
        {
            var allResults = new List<P>();
            var nextUrl = url;

            debugTracer.TrackTrace($"Loading pagable query {url}...");

            int pageCount = 0;
            while (!string.IsNullOrEmpty(nextUrl))
            {
                var response = await httpClient.ExecuteHttpCallWithThrottleRetries(async () => await httpClient.GetAsync(nextUrl), nextUrl);

                var body = await response.Content.ReadAsStringAsync();
                response.EnsureSuccessStatusCode();
                var r = JsonSerializer.Deserialize<T>(body);

                if (r != null)
                {
                    allResults.AddRange(r.PageResults);
                    nextUrl = r.OdataNextLink;
                    pageCount++;
                    debugTracer.TrackTrace($"Loading page {pageCount} ({nextUrl})...");
                }
            }
            debugTracer.TrackTrace($"{allResults.Count} for {url}.");

            return allResults;
        }
    }
}
