using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;

namespace OfficeNotifications.Engine.Webhooks
{
    public class UserChatsWebhooksManager : UserBaseWebhooksManager
    {
        public UserChatsWebhooksManager(string userId, X509Certificate2 cert, Config config, ILogger trace) : base(userId, config, trace)
        {
            EncryptionCertificate = Convert.ToBase64String(cert.Export(X509ContentType.Cert));
            EncryptionCertificateId = cert.Subject;
        }

        /// <summary>
        /// We want the chat info back with the notification
        /// </summary>
        public override bool IncludeResourceData { get => true; }

        public override string ChangeType => "created";

        public override string Resource => $"/users/{_userId}/chats/getAllMessages";

        public override DateTime MaxNotificationAgeFromToday => DateTime.Now.AddMinutes(55);

    }
}
