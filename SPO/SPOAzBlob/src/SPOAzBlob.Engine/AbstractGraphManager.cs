using Azure.Identity;
using CommonUtils;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPOAzBlob.Engine
{
    /// <summary>
    /// Something that interacts with Graph
    /// </summary>
    public abstract class AbstractGraphManager
    {

        protected GraphServiceClient _client;
        protected Config _config;
        protected DebugTracer _trace;

        public AbstractGraphManager(Config config, DebugTracer trace)
        {
            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };
            var scopes = new[] { "https://graph.microsoft.com/.default" };

            var clientSecretCredential = new ClientSecretCredential(config.AzureAdConfig.TenantId, config.AzureAdConfig.ClientID, config.AzureAdConfig.Secret, options);
            _client = new GraphServiceClient(clientSecretCredential, scopes);
            _config = config;
            _trace = trace;
        }

    }
}
