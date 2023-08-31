using CommonUtils;
using CommonUtils.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPOAzBlob.Engine
{

    public class Config : AzureAdWithTelemetryConfig
    {
        public Config(Microsoft.Extensions.Configuration.IConfiguration config) : base(config) { }

        [ConfigValue] public string BlobContainerName { get; set; } = string.Empty;

        [ConfigValue] public string SharePointSiteId { get; set; } = string.Empty;
        [ConfigValue(true)] public string WebhookUrlOverride { get; set; } = string.Empty;

        public string ServiceBusQueueName => "graphupdates";
        public string AzureTableActivity => "activity";
        public string AzureTableLocks => "locks";
        public string AzureTablePropertyBag => "propertybag";

        [ConfigSection("ConnectionStrings")] public ConnectionStrings ConnectionStrings { get; set; } = null!;

    }

    public class ConnectionStrings : PropertyBoundConfig
    {
        public ConnectionStrings(Microsoft.Extensions.Configuration.IConfigurationSection config) : base(config)
        {
        }

        [ConfigValue]
        public string Storage { get; set; } = string.Empty;

        [ConfigValue]
        public string ServiceBus { get; set; } = string.Empty;

    }
}
