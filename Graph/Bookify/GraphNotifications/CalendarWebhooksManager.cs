using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace GraphNotifications;

/// <summary>
/// https://learn.microsoft.com/en-us/graph/outlook-change-notifications-overview
/// </summary>
public class CalendarWebhooksManager : UserBaseWebhooksManager
{
    public CalendarWebhooksManager(GraphServiceClient client, string userId, X509Certificate2 cert, IWebhookConfig config, ILogger logger) : base(client, userId, config, logger)
    {
        EncryptionCertificate = Convert.ToBase64String(cert.Export(X509ContentType.Cert));
        EncryptionCertificateId = cert.Subject;
    }

    /// <summary>
    /// We want the chat info back with the notification
    /// </summary>
    public override bool IncludeResourceData { get => true; }

    public override string ChangeType => "created";

    /// <summary>
    /// Graph won't let us create webhooks with resource-data for messages without specifying fields
    /// </summary>
    public override string Resource => $"/users/{_userId}/events?$select=Subject";

    public override DateTime MaxNotificationAgeFromToday => DateTime.Now.AddMinutes(55);

}

public class NotificationContext
{
    public string ForUserId { get; set; } = string.Empty;

}
