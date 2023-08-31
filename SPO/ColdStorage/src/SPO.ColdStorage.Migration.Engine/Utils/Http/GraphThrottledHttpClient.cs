using Azure.Core;
using Azure.Identity;
using SPO.ColdStorage.Entities.Configuration;
using System.Net.Http.Headers;

namespace SPO.ColdStorage.Migration.Engine.Utils.Http
{
    /// <summary>
    /// HttpClient that can handle HTTP 429s automatically
    /// </summary>
    public class GraphThrottledHttpClient : AutoThrottleHttpClient
    {
        public GraphThrottledHttpClient(Config config, bool ignoreRetryHeader, DebugTracer debugTracer) : base(ignoreRetryHeader, debugTracer, new SecureGraphHandler(config))
        {
        }
    }

    public class SecureGraphHandler : DelegatingHandler
    {
        protected Config _config;
        protected AccessToken _token;
        public SecureGraphHandler(Config config)
        {
            _config = config;
            InnerHandler = new HttpClientHandler();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var confidentialClientApplication = new ClientSecretCredential(_config.AzureAdConfig.TenantId, _config.AzureAdConfig.ClientID, _config.AzureAdConfig.Secret);
            if (_token.ExpiresOn < DateTime.Now.AddMinutes(5))
            {
                _token = await confidentialClientApplication.GetTokenAsync(new TokenRequestContext(new string[] { "https://graph.microsoft.com/.default" }));
            }
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token.Token);

            return await base.SendAsync(request, cancellationToken);
        }

    }
}
