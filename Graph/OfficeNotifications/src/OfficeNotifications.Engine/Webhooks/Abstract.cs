using CommonUtils;
using Microsoft.Graph;
using OfficeNotifications.Engine.Models;
using OfficeNotifications.Engine;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace OfficeNotifications.Engine.Webhooks
{
    /// <summary>
    /// Webhook manager for a user
    /// </summary>
    public abstract class UserBaseWebhooksManager : BaseWebhooksManager
    {
        protected readonly string _userId;

        public UserBaseWebhooksManager(string userId, Config config, ILogger trace) : base(config, trace)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException($"'{nameof(userId)}' cannot be null or empty.", nameof(userId));
            }

            _userId = userId;
        }

        public override NotificationContext? ClientStateModel => new NotificationContext { ForUserId = _userId };

        public static async Task<T> LoadFromKeyvault<T>(string certName, string userId, Config config, ILogger trace)
            where T : UserBaseWebhooksManager
        {
            var cert = await AuthUtils.RetrieveKeyVaultCertificate(certName, config.AzureAdConfig.TenantId, config.AzureAdConfig.ClientID, config.AzureAdConfig.ClientSecret, config.KeyVaultUrl);
            var instance = Activator.CreateInstance(typeof(T), new object[] { userId, cert, config, trace });

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

        public BaseWebhooksManager(Config config, ILogger trace) : base(config, trace)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (trace is null)
            {
                throw new ArgumentNullException(nameof(trace));
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
                await _client.Subscriptions[existingSub.Id].Request().DeleteAsync();
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
                returnSub = await _client.Subscriptions[existingSub.Id].Request().UpdateAsync(subscription);
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

                returnSub = await _client.Subscriptions.Request().AddAsync(sub);
            }

            return returnSub;
        }

        public async Task<List<Subscription>> GetInScopeSubscriptions()
        {
            if (subsCache == null)
            {
                var subs = await _client.Subscriptions.Request().GetAsync();
                subsCache = subs.Where(s => s.ChangeType == ChangeType && s.NotificationUrl == WebhookUrl && s.Resource == Resource).ToList();
            }
            return subsCache;
        }
    }
}
