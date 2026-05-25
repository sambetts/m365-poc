using System.Text;

namespace SPO.ColdStorage.Entities.Configuration;

public class AzureAdConfig(Microsoft.Extensions.Configuration.IConfigurationSection config) : BaseConfig(config)
{
    [ConfigValue]
    public string? Secret { get; set; } = string.Empty;

    [ConfigValue]
    public string? ClientID { get; set; } = string.Empty;

    [ConfigValue]
    public string? TenantId { get; set; } = string.Empty;
}
