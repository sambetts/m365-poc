using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace GraphNotifications;

public class WebhookContentManager(GraphServiceClient client, X509Certificate2 certificate, ILogger<WebhookContentManager> logger, IWebhookConfig config)
{
    private Dictionary<string, CalendarWebhooksManager> _userChatsWebhooksManagers = new();


    public async static Task<WebhookContentManager> GetNotificationManager(GraphServiceClient client, string certName, IWebhookConfig config, ILogger<WebhookContentManager> trace)
    {
        var cert = await AuthUtils.RetrieveKeyVaultCertificate(certName, config.AzureAdConfig.TenantId, config.AzureAdConfig.ClientId, config.AzureAdConfig.ClientSecret, config.KeyVaultUrl);
        return new WebhookContentManager(client, cert, trace, config);
    }
}
