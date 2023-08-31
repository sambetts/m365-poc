using System;
using System.Text.Json;
using System.Threading.Tasks;
using CommonUtils;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OfficeNotifications.Engine;
using OfficeNotifications.Engine.Models;

namespace OfficeNotifications.Functions
{
    public class SBGraphUpdate
    {
        private readonly Config _config;
        private readonly ILogger<SBGraphUpdate> _tracer;

        public SBGraphUpdate(Config config, ILogger<SBGraphUpdate> tracer)
        {
            this._config = config;
            this._tracer = tracer;
        }

        [Function("SBGraphUpdate")]
        public async Task Run([ServiceBusTrigger("graphupdates", Connection = "ServiceBusConnectionString")] string messageContents)
        {
            if (string.IsNullOrEmpty(messageContents))
            {
                _tracer.LogWarning("Got empty message from the queue. Ignoring");
                return;
            }

            var contentDecryptingCert = await AuthUtils.RetrieveKeyVaultCertificate("webhooks", _config.AzureAdConfig.TenantId, _config.AzureAdConfig.ClientID, _config.AzureAdConfig.ClientSecret, _config.KeyVaultUrl);
            var notificationManager = new UserNotificationsManager(contentDecryptingCert, _config, _tracer);
            GraphNotification update = null;
            try
            {
                update = JsonSerializer.Deserialize<GraphNotification>(messageContents);
            }
            catch (Exception ex)
            {
                _tracer.LogError(ex.Message, ex);
            }
            
            if (update != null && update.IsValid)
            {
                foreach (var n in update.Notifications)
                {
                    var notificationContentJson = n.EncryptedResourceDataContent.DecryptResourceDataContent(contentDecryptingCert);

                    // Determine what to do
                    if (n.IsValid && !string.IsNullOrEmpty(notificationContentJson))
                    {
                        await notificationManager.ProcessWebhookMessage(n, notificationContentJson);
                    }
                    else
                    {
                        _tracer.LogWarning($"Got invalid Graph notification");
                    }
                }
            }
            else
            {
                _tracer.LogWarning($"Got invalid Graph update notification with body '{messageContents}'");
            }
        }
    }
}
