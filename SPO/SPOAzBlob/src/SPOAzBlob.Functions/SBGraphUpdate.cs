using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using CommonUtils;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Graph;
using SPOAzBlob.Engine;
using SPOAzBlob.Engine.Models;

namespace SPOAzBlob.Functions
{
    public class SBGraphUpdate
    {
        [Function("SBGraphUpdate")]
        public async Task Run([ServiceBusTrigger("graphupdates", Connection = "ServiceBusConnectionString")] string messageContents, FunctionContext context)
        {
            var trace = (DebugTracer)context.InstanceServices.GetService(typeof(DebugTracer));
            var config = (Config)context.InstanceServices.GetService(typeof(Config));

            var update = JsonSerializer.Deserialize<GraphNotification>(messageContents);
            if (update != null && update.IsValid)
            {
                var fm = new FileOperationsManager(config, trace);
                var results = new List<DriveItem>();
                try
                {
                    results = await fm.ProcessSpoUpdatesForActiveLocks();
                }
                catch (Exception ex)
                {
                    trace.TrackTrace("Couldn't process new SPO updates", Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Critical);
                    trace.TrackExceptionAndLogToTrace(ex);
                }

                trace.TrackTrace($"Processed {results.Count} file updates.");


            }
            else
            {
                trace.TrackTrace($"Got invalid Graph update notification with body '{messageContents}'");
            }

        }
    }
}
