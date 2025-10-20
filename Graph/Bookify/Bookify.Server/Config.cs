using GraphNotifications;

namespace Bookify.Server;

public class AppConfig : IWebhookConfig
{
    public AzureAdConfig AzureAd { get; set; } = new();
    public string SharedRoomMailboxUpn { get; set; } = string.Empty;
    public ConnectionStringsConfig ConnectionStrings { get; set; } = new();
    public IAzureAdConfig AzureAdConfig => AzureAd;

    // Required settings
    public string KeyVaultUrl { get; set; } = string.Empty; // must be set in configuration
    public string WebhookUrlOverride { get; set; } = string.Empty; // must be set in configuration

    public AppConfig(IConfiguration configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        // Bind AzureAd section if present
        configuration.GetSection("AzureAd").Bind(AzureAd);

        SharedRoomMailboxUpn = configuration["SharedRoomMailboxUpn"] ?? throw new InvalidOperationException("SharedRoomMailboxUpn configuration is required.");

        // Prefer connection string from named connection strings section
        ConnectionStrings.DefaultConnection =
            configuration.GetConnectionString("DefaultConnection") ??
            configuration["ConnectionStrings:DefaultConnection"] ?? string.Empty;

        // Key Vault URL (support both KeyVault:Url and KeyVaultUrl)
        KeyVaultUrl = configuration["KeyVault:Url"] ?? configuration["KeyVaultUrl"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(KeyVaultUrl))
            throw new InvalidOperationException("KeyVaultUrl (or KeyVault:Url) configuration is required.");

        // Webhook override (required). Support multiple keys.
        WebhookUrlOverride = configuration["WebhookUrlOverride"] ?? configuration["Graph:WebhookUrlOverride"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(WebhookUrlOverride))
            throw new InvalidOperationException("WebhookUrlOverride (or Graph:WebhookUrlOverride) configuration is required.");
    }
}

public class AzureAdConfig : IAzureAdConfig
{
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public class ConnectionStringsConfig
{
    public string DefaultConnection { get; set; } = string.Empty;
}
