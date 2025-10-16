using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Text.Json;

namespace GraphNotifications;


/// <summary>
/// Webhook manager for a user
/// </summary>
public abstract class UserBaseWebhooksManager : BaseWebhooksManager
{
    protected readonly string _userId;

    public UserBaseWebhooksManager(GraphServiceClient client, string userId, IWebhookConfig config, ILogger logger) : base(client, config, logger)
    {
        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentException($"'{nameof(userId)}' cannot be null or empty.", nameof(userId));
        }

        _userId = userId;
    }

    public override NotificationContext? ClientStateModel => new NotificationContext { ForUserId = _userId };

    public static async Task<T> LoadFromKeyvault<T>(string certName, string userId, IWebhookConfig config, ILogger trace)
        where T : UserBaseWebhooksManager
    {

        var cert = await AuthUtils.RetrieveKeyVaultCertificate(certName, config.AzureAdConfig.TenantId, config.AzureAdConfig.ClientId, config.AzureAdConfig.ClientSecret, config.KeyVaultUrl);
        var instance = Activator.CreateInstance(typeof(T), [userId, cert, config, trace]);

        return (T)instance!;
    }
}


/// <summary>
/// Manages webhooks for update subscriptions.
/// https://learn.microsoft.com/en-us/graph/change-notifications-with-resource-data
/// </summary>
public abstract class BaseWebhooksManager
{
    private List<Subscription>? subsCache = null;

    protected GraphServiceClient _client;
    private readonly IWebhookConfig _config;
    private readonly ILogger _logger;

    public abstract string ChangeType { get; }
    public abstract string Resource { get; }

    public virtual bool IncludeResourceData { get; } = false;
    public string EncryptionCertificate { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public string EncryptionCertificateId { get; set; } = string.Empty;
    public virtual NotificationContext? ClientStateModel { get; } = null;
    public abstract DateTime MaxNotificationAgeFromToday { get; }

    public BaseWebhooksManager(GraphServiceClient client, IWebhookConfig config, ILogger logger)
    {
        WebhookUrl = config.WebhookUrlOverride;
        _client = client;
        _config = config;
        _logger = logger;
    }

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

            var state = string.Empty;

            if (ClientStateModel != null)
            {
                state = JsonSerializer.Serialize(ClientStateModel);
                if (state != null && state.Length > 128)        // Max len for this field
                {
                    throw new InvalidOperationException("Client state too long");
                }
            }

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
                    EncryptionCertificateId = EncryptionCertificateId,
                    ClientState = state
                };
            }
            else
            {
                sub = new Subscription
                {
                    Resource = Resource,
                    ClientState = state,
                    ChangeType = ChangeType,
                    ExpirationDateTime = MaxNotificationAgeFromToday,
                    NotificationUrl = WebhookUrl,
                    IncludeResourceData = false
                };
            }

            returnSub = await _client.Subscriptions.PostAsync(sub);
        }

        return returnSub;
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

