using GraphNotifications;
using Microsoft.Extensions.Configuration;

namespace Bookify.Server;

public class AppConfig : IWebhookConfig
{
    public AzureAdConfig AzureAd { get; set; } = new();
    public string? SharedRoomMailboxUpn { get; set; }
    public ConnectionStringsConfig ConnectionStrings { get; set; } = new();
    public IAzureAdConfig AzureAdConfig  => AzureAd;

    public string KeyVaultUrl { get; set; } = null!; // must be set in configuration

    public string WebhookUrlOverride { get; set; } = null!; // must be set in configuration

    public AppConfig(IConfiguration configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        // Bind AzureAd section if present
        configuration.GetSection("AzureAd").Bind(AzureAd);

        SharedRoomMailboxUpn = configuration["SharedRoomMailboxUpn"]; // optional

        // Prefer connection string from named connection strings section
        ConnectionStrings.DefaultConnection =
            configuration.GetConnectionString("DefaultConnection") ??
            configuration["ConnectionStrings:DefaultConnection"] ?? string.Empty;
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
