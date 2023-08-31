using CommonUtils;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using OfficeNotifications.Engine.Models;
using OfficeNotifications.Engine.Webhooks;
using StackExchange.Redis;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace OfficeNotifications.Engine
{
    /// <summary>
    /// Handles messages received from webhooks & turns them into relevant notification caches
    /// </summary>
    public class UserNotificationsManager : AbstractManager
    {
        #region Constructor/Factory & Privates

        private ConnectionMultiplexer redis;
        private readonly X509Certificate2 _certificate;

        // 1 webhooks manager instance per userId
        private Dictionary<string, UserChatsWebhooksManager> _userChatsWebhooksManagers = new();
        private Dictionary<string, UserEmailsWebhooksManager> _userEmailsWebhooksManagers = new();

        public UserNotificationsManager(X509Certificate2 certificate, Config config, ILogger trace) : base(config, trace)
        {
            redis = ConnectionMultiplexer.Connect(config.ConnectionStrings.Redis);
            this._certificate = certificate;
        }

        public async static Task<UserNotificationsManager> GetNotificationManager(string certName, Config config, ILogger trace)
        {
            var cert = await AuthUtils.RetrieveKeyVaultCertificate(certName, config.AzureAdConfig.TenantId, config.AzureAdConfig.ClientID, config.AzureAdConfig.ClientSecret, config.KeyVaultUrl);
            return new UserNotificationsManager(cert, config, trace);
        }

        #endregion

        public async Task<IEnumerable<UserNotification>> GetNotifications(string userId)
        {
            var db = redis.GetDatabase();
            var results = await db.ListRangeAsync(GetKey(userId), 0, -1);
            var content = results.Where(r=> r.HasValue).Select(r => r.ToString());

            var notifications = new List<UserNotification>();
            foreach (var c in content)
            {
                try
                {
                    var n = JsonSerializer.Deserialize<UserNotification>(c);

                    if (n != null)
                    {
                        notifications.Add(n);
                    }
                }
                catch (JsonException)
                {
                    // Ignore
                }
            }

            return notifications;
        }

        public async Task ClearNotifications(string userId)
        {
            var db = redis.GetDatabase();
            await db.KeyDeleteAsync(GetKey(userId));
        }

        public async Task ProcessWebhookMessage(ChangeNotificationForUserId notification, string notificationContentJson)
        {
            if (notification is null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            if (string.IsNullOrEmpty(notificationContentJson))
            {
                throw new ArgumentException($"'{nameof(notificationContentJson)}' cannot be null or empty.", nameof(notificationContentJson));
            }

            switch (notification.ResourceData?.OdataType)
            {
                case "#Microsoft.Graph.chatMessage":
                    var msg = JsonSerializer.Deserialize<ChatMessage>(notificationContentJson);
                    await ProcessChatMessage(notification, msg!);

                    return;
                case "#microsoft.graph.message":
                    var email = JsonSerializer.Deserialize<Message>(notificationContentJson);
                    await ProcessEmail(notification, email!);

                    return;
                default:
                    break;
            }
            throw new ArgumentOutOfRangeException(nameof(notification), "Invalid/unsupported notification resource type");
        }

        /// <summary>
        /// Activate email & teams chat notifications
        /// </summary>
        public async Task<List<Subscription>> EnableNotifications(string userId)
        {
            var chatsWebhooksManager = GetUserEmailsWebhooksManagers(userId).Item1;
            var emailsWebhooksManager = GetUserEmailsWebhooksManagers(userId).Item2;

            var subs = new List<Subscription>();
            try
            {
                var results = await Task.WhenAll(chatsWebhooksManager.CreateOrUpdateSubscription(), emailsWebhooksManager.CreateOrUpdateSubscription());
                subs = results.ToList();
            }
            catch (ServiceException ex)
            {
                _trace.LogError(ex.Message, ex);
                throw ex;
            }

            return subs;
        }

        public async Task<List<Subscription>> GetSubscriptions(string userId)
        {
            var chatsWebhooksManager = GetUserEmailsWebhooksManagers(userId).Item1;
            var emailsWebhooksManager = GetUserEmailsWebhooksManagers(userId).Item2;

            List<Subscription>? subs = null;
            try
            {
                var all = await Task.WhenAll(chatsWebhooksManager.GetInScopeSubscriptions(), emailsWebhooksManager.GetInScopeSubscriptions());
                subs = all.SelectMany(s => s).ToList();
            }
            catch (ServiceException ex)
            {
                _trace.LogError(ex.Message, ex);
                throw ex;
            }

            if (subs != null)
            {
                return subs;
            }
            else return new List<Subscription>();
        }

        /// <summary>
        /// Delete webhooks for notifications
        /// </summary>
        public async Task DisableNotifications(string userId)
        {
            var chatsWebhooksManager = GetUserEmailsWebhooksManagers(userId).Item1;
            var emailsWebhooksManager = GetUserEmailsWebhooksManagers(userId).Item2;

            try
            {
                await Task.WhenAll(chatsWebhooksManager.DeleteWebhooks(), emailsWebhooksManager.DeleteWebhooks());
            }
            catch (ServiceException ex)
            {
                _trace.LogError(ex.Message, ex);
                throw ex;
            }
        }

        /// <summary>
        /// Verify valid email & teams chat notifications
        /// </summary>
        public async Task<bool> HaveValidSubscriptions(string userId)
        {
            var chatsWebhooksManager = GetUserEmailsWebhooksManagers(userId).Item1;
            var emailsWebhooksManager = GetUserEmailsWebhooksManagers(userId).Item2;

            bool[]? results = null;
            try
            {
                results = await Task.WhenAll(chatsWebhooksManager.HaveValidSubscription(), emailsWebhooksManager.HaveValidSubscription());
            }
            catch (ServiceException ex)
            {
                _trace.LogError(ex.Message, ex);
                throw ex;
            }

            // Check all subs are valid
            return results != null && results.All(r=> r);
        }

        #region Private Functions

        (UserChatsWebhooksManager, UserEmailsWebhooksManager) GetUserEmailsWebhooksManagers(string userId)
        {
            if (!_userChatsWebhooksManagers.ContainsKey(userId))
            {
                _userChatsWebhooksManagers.Add(userId, new UserChatsWebhooksManager(userId, _certificate, _config, _trace));
            }
            if (!_userEmailsWebhooksManagers.ContainsKey(userId))
            {
                _userEmailsWebhooksManagers.Add(userId, new UserEmailsWebhooksManager(userId, _certificate, _config, _trace));
            }

            var chatsWebhooksManager = _userChatsWebhooksManagers[userId];
            var emailsWebhooksManager = _userEmailsWebhooksManagers[userId];

            return (chatsWebhooksManager, emailsWebhooksManager);
        }

        private async Task ProcessEmail(ChangeNotificationForUserId notification, Message msg)
        {
            var from = msg.From?.EmailAddress?.Name ?? msg.From?.EmailAddress?.Address;
            await AddNotification(notification.UserId, new UserNotification { Message = $"New email from '{from}'" });
        }

        private async Task ProcessChatMessage(ChangeNotificationForUserId notification, ChatMessage msg)
        {
            await AddNotification(notification.UserId, new UserNotification { Message = $"New message from '{msg.From?.User?.DisplayName}'" });
        }

        private async Task AddNotification(string userId, UserNotification notification)
        {
            var db = redis.GetDatabase();
            await db.ListRightPushAsync(GetKey(userId), new RedisValue(JsonSerializer.Serialize(notification)));
        }

        private RedisKey GetKey(string userId)
        {
            return new RedisKey($"notifications-{userId}");
        }
        #endregion
    }
}
