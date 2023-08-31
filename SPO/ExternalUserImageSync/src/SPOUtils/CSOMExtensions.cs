﻿using Microsoft.SharePoint.Client;
using System.Net;

namespace SPOUtils
{
    public static class CSOMExtensions
    {
        public static async Task ExecuteQueryAsyncWithThrottleRetries(this ClientContext clientContext, DebugTracer tracer)
        {
            int retryAttempts = 0;
            int backoffIntervalSeconds = 1;
            int retryAfterInterval = 0;
            bool retryWithWrapper = false;
            ClientRequestWrapper? wrapper = null;

            // Do while retry attempt is less than retry count
            while (retryAttempts < Constants.MAX_SPO_API_RETRIES)
            {
                try
                {
                    if (!retryWithWrapper)
                    {
                        await clientContext.ExecuteQueryAsync();
                        return;
                    }
                    else
                    {
                        retryAttempts++;

                        // retry the previous request using wrapper
                        if (wrapper != null && wrapper.Value != null)
                        {
                            await clientContext.RetryQueryAsync(wrapper.Value);
                            return;
                        }
                        // retry the previous request as normal
                        else
                        {
                            await clientContext.ExecuteQueryAsync();
                            return;
                        }
                    }
                }
                catch (WebException ex)
                {
                    var response = ex.Response as HttpWebResponse;

                    // Check if request was throttled - http status code 429
                    // Check is request failed due to server unavailable - http status code 503
                    if (response != null && (response.StatusCode == (HttpStatusCode)429 || response.StatusCode == (HttpStatusCode)503))
                    {
                        var clientRequestData = ex.Data["ClientRequest"];
                        if (clientRequestData != null)
                        {
                            wrapper = (ClientRequestWrapper)clientRequestData;
                            retryWithWrapper = true;
                        }

                        // Determine the retry after value - use the `Retry-After` header when available
                        string retryAfterHeader = response.GetResponseHeader("Retry-After");
                        if (!string.IsNullOrEmpty(retryAfterHeader))
                        {
                            if (!Int32.TryParse(retryAfterHeader, out retryAfterInterval))
                            {
                                retryAfterInterval = backoffIntervalSeconds;
                            }
                        }
                        else
                        {
                            retryAfterInterval = backoffIntervalSeconds;
                        }

                        // Trace standard throttling message
                        tracer.TrackTrace($"{Constants.THROTTLE_ERROR} executing CSOM request. Sleeping for {retryAfterInterval} seconds.", Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Warning);

                        // Delay for the requested seconds
                        await Task.Delay(retryAfterInterval * 1000);

                        // Increase counters
                        backoffIntervalSeconds = backoffIntervalSeconds * 2;
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // Track error & throw exception
            var givingUpMsgBody = $"Maximum retry attempts {Constants.MAX_SPO_API_RETRIES} has been attempted.";
            tracer.TrackTrace($"{Constants.THROTTLE_ERROR} executing CSOM request. {givingUpMsgBody}", Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error);
            throw new Exception($"Error executing CSOM request. {givingUpMsgBody}");

        }
    }
}
