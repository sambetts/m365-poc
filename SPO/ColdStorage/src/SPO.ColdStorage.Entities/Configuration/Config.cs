﻿namespace SPO.ColdStorage.Entities.Configuration
{

    public class Config : BaseConfig
    {
        public Config(Microsoft.Extensions.Configuration.IConfiguration config) : base(config)
        {
        }


        [ConfigValue]
        public string BaseServerAddress { get; set; } = string.Empty;

        public string ServiceBusQueueName => "filediscovery";

        [ConfigValue]
        public string KeyVaultUrl { get; set; } = string.Empty;


        [ConfigValue]
        public string BlobContainerName { get; set; } = string.Empty;

        [ConfigValue(true)]
        public string AppInsightsInstrumentationKey { get; set; } = string.Empty;

        public bool HaveAppInsightsConfigured => !string.IsNullOrEmpty(AppInsightsInstrumentationKey);

        [ConfigSection("AzureAd")]
        public AzureAdConfig AzureAdConfig { get; set; } = null!;

        [ConfigSection("ConnectionStrings")]
        public ConnectionStrings ConnectionStrings { get; set; } = null!;

        [ConfigSection("Dev")]
        public DevConfig DevConfig { get; set; } = null!;


        [ConfigSection("Search")]
        public SearchConfig SearchConfig { get; set; } = null!;
    }

    public class ConfigException : Exception
    {
        public ConfigException(string message) : base(message) { }
    }
}
