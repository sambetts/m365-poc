using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace OfficeNotifications.Engine
{
    public class AbstractManager
    {
        protected Config _config;
        protected ILogger _trace;
        public AbstractManager(Config config, ILogger trace)
        {
            _config = config;
            _trace = trace;
        }
    }

    /// <summary>
    /// Something that interacts with Graph
    /// </summary>
    public abstract class AbstractGraphManager : AbstractManager
    {

        protected GraphServiceClient _client;

        protected virtual bool UseBetaEndpoint => false;

        public AbstractGraphManager(Config config, ILogger trace) :base(config, trace)
        {
            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };
            var scopes = new[] { "https://graph.microsoft.com/.default" };

            var clientSecretCredential = new ClientSecretCredential(config.AzureAdConfig.TenantId, config.AzureAdConfig.ClientID, config.AzureAdConfig.ClientSecret, options);
            _client = new GraphServiceClient(clientSecretCredential, scopes);

            if (UseBetaEndpoint)
            {
                _client.BaseUrl = "https://graph.microsoft.com/beta";
            }

        }

    }
}
