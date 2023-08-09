namespace Entities.Configuration;

public class AppConfig : BaseConfig
{
    public AppConfig(Microsoft.Extensions.Configuration.IConfiguration config) : base(config)
    {
    }

    [ConfigSection("ConnectionStrings")]
    public ConnectionStrings ConnectionStrings { get; set; } = null!;

    [ConfigValue]
    public string AcsEndpointVal { get; set; } = null!;

    [ConfigValue]
    public string AcsAccessKeyVal { get; set; } = null!;

    [ConfigValue]
    public string DefaultLocationName { get; set; }
}

public class ConfigException : Exception
{
    public ConfigException(string message) : base(message) { }
}
