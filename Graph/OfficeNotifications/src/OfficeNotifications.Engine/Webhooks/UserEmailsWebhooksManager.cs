using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;

namespace OfficeNotifications.Engine.Webhooks
{
    public class UserEmailsWebhooksManager : UserBaseWebhooksManager
    {
        public UserEmailsWebhooksManager(string userId, X509Certificate2 cert, Config config, ILogger trace) : base(userId, config, trace)
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
}
