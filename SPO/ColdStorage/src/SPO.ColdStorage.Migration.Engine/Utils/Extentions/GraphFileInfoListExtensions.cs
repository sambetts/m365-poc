using SPO.ColdStorage.Migration.Engine.SnapshotBuilder;
using SPO.ColdStorage.Migration.Engine.Utils.Http;
using SPO.ColdStorage.Models;
using System.Text.Json;

namespace SPO.ColdStorage.Migration.Engine.Utils
{
    public static class GraphFileInfoListExtensions
    {
        public static async Task<BackgroundUpdate> GetDriveItemsAnalytics(this List<DocumentSiteWithMetadata> graphFiles, string baseSiteAddress, SecureSPThrottledHttpClient httpClient, DebugTracer tracer)
        {
            return await GetDriveItemsAnalytics(graphFiles, baseSiteAddress, httpClient, tracer, 100);
        }
        public static async Task<BackgroundUpdate> GetDriveItemsAnalytics(this List<DocumentSiteWithMetadata> graphFiles, string baseSiteAddress, SecureSPThrottledHttpClient httpClient, DebugTracer tracer, int waitMs)
        {
            var fileSuccessResults = new Dictionary<DocumentSiteWithMetadata, object>();

            foreach (var fileToUpdate in graphFiles)
            {
                // Read doc analytics
                var url = $"{baseSiteAddress}/_api/v2.0/drives/{fileToUpdate.DriveId}/items/{fileToUpdate.GraphItemId}" +
                    $"/analytics/allTime";

                try
                {
                    // Do our own parsing as Graph SDK doesn't do this very well
                    using (var analyticsResponse = await httpClient.GetAsyncWithThrottleRetries(url, tracer))
                    {
                        var analyticsResponseBody = await analyticsResponse.Content.ReadAsStringAsync();

                        // Ensure valid response
                        analyticsResponse.EnsureSuccessStatusCode();
                        var activitiesResponse = JsonSerializer.Deserialize<ItemAnalyticsRepsonse>(analyticsResponseBody) ?? new ItemAnalyticsRepsonse();
                        fileToUpdate.State = SiteFileAnalysisState.Complete;

                        fileSuccessResults.Add(fileToUpdate, activitiesResponse);
                        await Task.Delay(waitMs);
                    }
                }
                catch (HttpRequestException ex)
                {
                    if (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        fileToUpdate.State = SiteFileAnalysisState.TransientError;
                    }
                    else
                    {
                        fileToUpdate.State = SiteFileAnalysisState.FatalError;
                    }
                    tracer.TrackException(ex);
                    tracer.TrackTrace($"Got HTTP exception {ex.Message} getting analytics data for drive item {fileToUpdate.GraphItemId}", Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error);
                }
                catch (Exception ex)
                {
                    tracer.TrackException(ex);
                    tracer.TrackTrace($"Got general exception {ex.Message} getting analytics data for drive item {fileToUpdate.GraphItemId}", Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error);
                }
            }
            return new BackgroundUpdate { UpdateResults = fileSuccessResults };
        }

        public static async Task<BackgroundUpdate> GetDriveItemsHistory(this List<DocumentSiteWithMetadata> graphFiles, string baseSiteAddress, SecureSPThrottledHttpClient httpClient, DebugTracer tracer)
        { 
            return await GetDriveItemsHistory(graphFiles, baseSiteAddress, httpClient, tracer, 100);
        }
        public static async Task<BackgroundUpdate> GetDriveItemsHistory(this List<DocumentSiteWithMetadata> graphFiles, string baseSiteAddress, SecureSPThrottledHttpClient httpClient, DebugTracer tracer, int waitMs)
        {
            var fileSuccessResults = new Dictionary<DocumentSiteWithMetadata, object>();

            foreach (var fileToUpdate in graphFiles)
            {
                // Read doc analytics
                var url = $"{baseSiteAddress}/_api/v2.0/drives/{fileToUpdate.DriveId}/items/{fileToUpdate.GraphItemId}/versions";

                try
                {
                    // Do our own parsing as Graph SDK doesn't do this very well
                    using (var analyticsResponse = await httpClient.GetAsyncWithThrottleRetries(url, tracer))
                    {
                        var analyticsResponseBody = await analyticsResponse.Content.ReadAsStringAsync();

                        // Ensure valid response
                        analyticsResponse.EnsureSuccessStatusCode();
                        var activitiesResponse = JsonSerializer.Deserialize<DriveItemVersionInfo>(analyticsResponseBody) ?? new DriveItemVersionInfo();
                        fileToUpdate.State = SiteFileAnalysisState.Complete;

                        fileSuccessResults.Add(fileToUpdate, activitiesResponse);
                        await Task.Delay(waitMs);
                    }
                }
                catch (HttpRequestException ex)
                {
                    if (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        fileToUpdate.State = SiteFileAnalysisState.TransientError;
                    }
                    else
                    {
                        fileToUpdate.State = SiteFileAnalysisState.FatalError;
                    }
                    tracer.TrackException(ex);
                    tracer.TrackTrace($"Got HTTP exception {ex.Message} getting version data for drive item {fileToUpdate.GraphItemId}", Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error);
                }
                catch (Exception ex)
                {
                    tracer.TrackException(ex);
                    tracer.TrackTrace($"Got general exception {ex.Message} getting version data for drive item {fileToUpdate.GraphItemId}", Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error);
                }
            }

            return new BackgroundUpdate() { UpdateResults = fileSuccessResults };
        }
    }
}
