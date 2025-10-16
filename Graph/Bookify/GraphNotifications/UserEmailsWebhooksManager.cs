using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace GraphNotifications;

public class UserEmailsWebhooksManager : UserBaseWebhooksManager
{
    public UserEmailsWebhooksManager(GraphServiceClient client, string userId, X509Certificate2 cert, IWebhookConfig config, ILogger<UserEmailsWebhooksManager> logger) : base(client, userId, config, logger)
    {
        EncryptionCertificate = Convert.ToBase64String(cert.Export(X509ContentType.Cert));
        EncryptionCertificateId = cert.Subject;
    }

    // Email notifications with resources only support beta endpoint
    protected override bool UseBetaEndpoint => true;

    /// <summary>
    /// We want the chat info back with the notification
    /// </summary>
    public override bool IncludeResourceData { get => true; }

    public override string ChangeType => "created";

    /// <summary>
    /// Graph won't let us create webhooks with resource-data for messages without specifying fields
    /// </summary>
    public override string Resource => $"/users/{_userId}/messages?$select=From";

    public override DateTime MaxNotificationAgeFromToday => DateTime.Now.AddMinutes(55);

}

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
/// https://docs.microsoft.com/en-us/graph/webhooks
/// https://docs.microsoft.com/en-us/graph/webhooks-with-resource-data
/// </summary>
public abstract class BaseWebhooksManager : AbstractGraphManager
{
    private List<Subscription>? subsCache = null;

    public abstract string ChangeType { get; }
    public abstract string Resource { get; }

    public virtual bool IncludeResourceData { get; } = false;
    public string EncryptionCertificate { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public string EncryptionCertificateId { get; set; } = string.Empty;
    public virtual NotificationContext? ClientStateModel { get; } = null;
    public abstract DateTime MaxNotificationAgeFromToday { get; }

    public BaseWebhooksManager(GraphServiceClient client, IWebhookConfig config, ILogger logger) : base(client, logger)
    {
        if (logger is null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        WebhookUrl = config.WebhookUrlOverride;
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



/// <summary>
/// Something that interacts with Graph
/// </summary>
public abstract class AbstractGraphManager 
{

    protected GraphServiceClient _client;
    protected readonly ILogger _trace;

    protected virtual bool UseBetaEndpoint => false;

    public AbstractGraphManager(GraphServiceClient client, ILogger trace)
    {
        _client = client;
        _trace = trace;
    }
}


public class NotificationContext
{
    public string ForUserId { get; set; } = string.Empty;

}
