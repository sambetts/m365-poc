using CommonUtils;
using Microsoft.Graph;

namespace SPOAzBlob.Engine
{
    /// <summary>
    /// Manages webhooks for update subscriptions.
    /// https://docs.microsoft.com/en-us/graph/webhooks
    /// </summary>
    public class WebhooksManager : AbstractGraphManager
    {
        const string CHANGE_TYPE = "updated";
        private readonly string _webhookUrl;
        private List<Subscription>? subsCache = null;

        public WebhooksManager(Config config, DebugTracer trace, string webhookUrl) : base(config, trace)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (trace is null)
            {
                throw new ArgumentNullException(nameof(trace));
            }

            if (string.IsNullOrEmpty(webhookUrl))
            {
                throw new ArgumentException($"'{nameof(webhookUrl)}' cannot be null or empty.", nameof(webhookUrl));
            }

            this._webhookUrl = webhookUrl;
        }

        string SiteLibraryResourceIdentifier
        {
            get 
            {
                var siteUrl = _client.Sites[_config.SharePointSiteId].Drive.Root.RequestUrl;
                const string GRAPH_URL = "https://graph.microsoft.com/v1.0/";
                var siteResourceUrl = siteUrl.Substring(GRAPH_URL.Length, siteUrl.Length - GRAPH_URL.Length);
                return siteResourceUrl;
            }
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
            var expiry = DateTime.Now.AddDays(7);
            Subscription? returnSub = null;

            if (validHookAlready)
            {
                var existingSub = subs[0];

                // Renew single sub
                var subscription = new Subscription
                {
                    ExpirationDateTime = expiry
                };
                returnSub = await _client.Subscriptions[existingSub.Id].Request().UpdateAsync(subscription);
            }
            else
            {
                // Delete everything with this URL & recreate
                await DeleteWebhooks();

                returnSub = await _client.Subscriptions.Request().AddAsync(new Subscription
                {
                    Resource = SiteLibraryResourceIdentifier,
                    ChangeType = "updated",
                    ExpirationDateTime = expiry,
                    NotificationUrl = _webhookUrl
                });
            }

            return returnSub;
        }

        public async Task<List<Subscription>> GetInScopeSubscriptions()
        {
            if (subsCache == null)
            {
                var subs = await _client.Subscriptions.Request().GetAsync();
                subsCache = subs.Where(s => s.ChangeType == CHANGE_TYPE && s.NotificationUrl == _webhookUrl && s.Resource == SiteLibraryResourceIdentifier).ToList();
            }
            return subsCache;
        }
    }
}
