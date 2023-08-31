using System;
using System.Threading.Tasks;
using Azure.Identity;
using CommonUtils;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Graph;
using SPOAzBlob.Engine;
using SPOAzBlob.Functions.Models;

namespace SPOAzBlob.Functions
{
    public class SPOCleanUp
    {
        // Every 2 mins: 0 */2 * * * *
        // Every day: 0 0 * * *
        [Function("SPOCleanUp")]
        public static async Task Run([TimerTrigger("0 0 * * *")] TimerJobRefreshInfo timerInfo, FunctionContext context)
        {
            var trace = (DebugTracer)context.InstanceServices.GetService(typeof(DebugTracer));
            var config = (Config)context.InstanceServices.GetService(typeof(Config));

            trace.TrackTrace($"SPOCleanUp executing.");


            // Create new Graph client
            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };
            var scopes = new[] { "https://graph.microsoft.com/.default" };

            var clientSecretCredential = new ClientSecretCredential(config.AzureAdConfig.TenantId, config.AzureAdConfig.ClientID, config.AzureAdConfig.Secret, options);
            var client = new GraphServiceClient(clientSecretCredential, scopes);


            var drive = await client!.Sites[config.SharePointSiteId].Drive.Request().GetAsync();

            // Cleanup old SPO files
            var fm = new FileOperationsManager(config, trace);
            try
            {
                await fm.CleanOldFiles(drive.Id);
                trace.TrackTrace($"SPO files cleaned successfully");
            }
            catch (Exception ex)
            {
                trace.TrackTrace($"SPO files failed to clean", Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Critical);
                trace.TrackExceptionAndLogToTrace(ex);
            }

            trace.TrackTrace($"Next timer schedule at: {timerInfo.ScheduleStatus.Next}");
        }
    }

}
