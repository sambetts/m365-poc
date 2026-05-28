namespace Entities.Configuration;

/// <summary>
/// Azure AD application registration used to authenticate against Microsoft Graph.
/// Authentication is always client-secret based; the app needs Graph Sites.Read.All
/// and Files.Read.All application permissions.
/// </summary>
public class AzureAdConfig(Microsoft.Extensions.Configuration.IConfigurationSection config) : BaseConfig(config)
{
    [ConfigValue]
    public string? Secret { get; set; } = string.Empty;

    [ConfigValue]
    public string? ClientID { get; set; } = string.Empty;

    [ConfigValue]
    public string? TenantId { get; set; } = string.Empty;
}
