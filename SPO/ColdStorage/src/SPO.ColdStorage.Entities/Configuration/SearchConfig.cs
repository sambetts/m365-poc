namespace SPO.ColdStorage.Entities.Configuration
{
    public class SearchConfig : BaseConfig
    {
        public SearchConfig(Microsoft.Extensions.Configuration.IConfigurationSection config) : base(config)
        {
        }

        [ConfigValue(true)]
        public string IndexName { get; set; } = string.Empty;

        [ConfigValue(true)]
        public string ServiceName { get; set; } = string.Empty;

        [ConfigValue(true)]
        public string QueryKey { get; set; } = string.Empty;
    }
}
