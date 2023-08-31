using CommonUtils;
using CommonUtils.Config;
using OfficeNotifications.Engine.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficeNotifications.Engine
{

    public class Config : AzureAdWithTelemetryConfig
    {
        public Config(Microsoft.Extensions.Configuration.IConfiguration config) : base(config) { }

        [ConfigValue(true)] public string KeyVaultUrl { get; set; } = string.Empty;
        [ConfigValue] public string WebhookUrlOverride { get; set; } = string.Empty;

        public string ServiceBusQueueName => "graphupdates";

        [ConfigSection("ConnectionStrings")] public ConnectionStrings ConnectionStrings { get; set; } = null!;

    }

    public class ConnectionStrings : PropertyBoundConfig
    {
        public ConnectionStrings(Microsoft.Extensions.Configuration.IConfigurationSection config) : base(config)
        {
        }

        [ConfigValue]
        public string ServiceBus { get; set; } = string.Empty;

        [ConfigValue]
        public string Redis { get; set; } = string.Empty;
    }
}
