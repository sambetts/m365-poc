using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace GraphNotifications;

/// <summary>
/// Manages webhooks for update subscriptions.
/// https://learn.microsoft.com/en-us/graph/change-notifications-with-resource-data
/// </summary>
public abstract class BaseWebhooksManager(GraphServiceClient client, IWebhookConfig config, ILogger logger)
{
    private List<Subscription>? subsCache = null;

    protected GraphServiceClient _client = client;

    public abstract string ChangeType { get; }
    public abstract string Resource { get; }

    public virtual bool IncludeResourceData { get; } = false;
    public string EncryptionCertificate { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = config.WebhookUrlOverride;
    public string EncryptionCertificateId { get; set; } = string.Empty;
    public abstract DateTime MaxNotificationAgeFromToday { get; }

    public async Task<bool> HaveValidSubscription()
    {
        return (await GetInScopeSubscriptions()).Count == 1;
    }
    public async Task DeleteWebhooks()
    {
        var subs = await GetInScopeSubscriptions();

        // Delete everything with this URL & recreate
        foreach (var existingSub in subs)
        {
            await _client.Subscriptions[existingSub.Id].DeleteAsync();
        }

        subsCache = null;
    }

    public async Task<Subscription> CreateOrUpdateSubscription()
    {
        var subs = await GetInScopeSubscriptions();
        var validHookAlready = await HaveValidSubscription();
        Subscription? returnSub = null;

        if (validHookAlready)
        {
            var existingSub = subs[0];

            // Renew single sub
            var subscription = new Subscription
            {
                ExpirationDateTime = MaxNotificationAgeFromToday
            };
            returnSub = await _client.Subscriptions[existingSub.Id].PatchAsync(subscription);
        }
        else
        {
            // Delete everything with this URL & recreate
            await DeleteWebhooks();


            Subscription sub;
            if (IncludeResourceData)
            {
                sub = new Subscription
                {
                    Resource = Resource,
                    ChangeType = ChangeType,
                    ExpirationDateTime = MaxNotificationAgeFromToday,
                    NotificationUrl = WebhookUrl,
                    IncludeResourceData = true,
                    EncryptionCertificate = EncryptionCertificate,
                    EncryptionCertificateId = EncryptionCertificateId
                };
            }
            else
            {
                sub = new Subscription
                {
                    Resource = Resource,
                    ChangeType = ChangeType,
                    ExpirationDateTime = MaxNotificationAgeFromToday,
                    NotificationUrl = WebhookUrl,
                    IncludeResourceData = false
                };
            }

            returnSub = await _client.Subscriptions.PostAsync(sub);
        }

        return returnSub ?? throw new InvalidOperationException("Failed to create or update subscription.");
    }

    public async Task<List<Subscription>> GetInScopeSubscriptions()
    {
        if (subsCache == null)
        {
            var subs = await _client.Subscriptions.GetAsync();
            subsCache = subs?.Value?.Where(s => s.ChangeType == ChangeType && s.NotificationUrl == WebhookUrl && s.Resource == Resource).ToList() ?? new List<Subscription>();
        }
        return subsCache;
    }
}

