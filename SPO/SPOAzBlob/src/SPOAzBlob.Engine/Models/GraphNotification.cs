using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SPOAzBlob.Engine.Models
{
    public class GraphNotification
    {
        [JsonPropertyName("value")]
        public List<ChangeNotification> Notifications { get; set; } = new List<ChangeNotification>();

        [JsonIgnore]
        public bool IsValid => Notifications.Any();
    }

    public class ChangeNotification
    {
        [JsonPropertyName("subscriptionId")]
        public Guid SubscriptionId { get; set; }

        [JsonPropertyName("resource")]
        public string Resource { get; set; } = string.Empty;

        [JsonPropertyName("tenantId")]
        public Guid TenantId { get; set; } = Guid.Empty;


        [JsonPropertyName("subscriptionExpirationDateTime")]
        public DateTimeOffset SubscriptionExpirationDateTime { get; set; }

        [JsonPropertyName("changeType")]
        public string ChangeType { get; set; } = string.Empty;
    }
}
