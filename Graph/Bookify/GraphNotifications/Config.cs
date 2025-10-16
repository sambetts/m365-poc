namespace GraphNotifications;

public interface IWebhookConfig
{
    public IAzureAdConfig AzureAdConfig { get; }
    public string KeyVaultUrl { get; }
    public string WebhookUrlOverride { get; }
}

public interface IAzureAdConfig 
{
    public string ClientSecret { get; }

    public string ClientId { get; } 

    public string TenantId { get; }
}
