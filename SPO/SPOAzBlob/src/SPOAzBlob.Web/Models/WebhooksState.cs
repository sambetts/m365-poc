using Microsoft.Graph;

namespace SPOAzBlob.Web.Models
{
    public class WebhooksState
    {
        public string TargetEndpoint { get; set; } = string.Empty;
        public List<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    }
}
