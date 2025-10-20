using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using System.Security.Cryptography.X509Certificates;

namespace GraphNotifications;

/// <summary>
/// https://learn.microsoft.com/en-us/graph/outlook-change-notifications-overview
/// </summary>
public class CalendarWebhooksManager : BaseWebhooksManager
{
    private readonly string _userId;

    public CalendarWebhooksManager(GraphServiceClient client, string userId, X509Certificate2 cert, IWebhookConfig config, ILogger logger) : base(client, config, logger)
    {
        EncryptionCertificate = Convert.ToBase64String(cert.Export(X509ContentType.Cert));
        EncryptionCertificateId = cert.Subject;
        _userId = userId;
    }

    /// <summary>
    /// We want the event data in the notification
    /// </summary>
    public override bool IncludeResourceData { get => true; }

    public override string ChangeType => "created,updated,deleted";

    /// <summary>
    /// User ID specific calendar events with selected fields that you can update from the Outlook UI
    /// </summary>
    public override string Resource => $"/users/{_userId}/events?$select=" +
        $"subject,bodyPreview,start,end,location,locations,attendees,isAllDay,recurrence,reminderMinutesBeforeStart,isReminderOn,showAs,sensitivity,importance,categories,responseRequested," +
        $"allowNewTimeProposals,isOnlineMeeting,onlineMeetingProvider,onlineMeetingUrl,hasAttachments,isCancelled";

    public override DateTime MaxNotificationAgeFromToday => DateTime.Now.AddMinutes(55);

}

public class NotificationContext
{
    public string ForUserId { get; set; } = string.Empty;

}
