using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OfficeNotifications.Tests
{
    public abstract class AbstractTest
    {
        protected TestConfig? _config;
        protected ILogger? _tracer;
        protected GraphServiceClient? _client;


        [TestInitialize]
        public void Init()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddEnvironmentVariables()
                .AddUserSecrets(System.Reflection.Assembly.GetExecutingAssembly(), true)
                .AddJsonFile("appsettings.json", true);

            _tracer = LoggerFactory.Create(config =>
            {
                config.AddConsole();
            }).CreateLogger("Unit tests");

            var config = builder.Build();
            _config = new TestConfig(config);


            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };
            var scopes = new[] { "https://graph.microsoft.com/.default" };

            var clientSecretCredential = new ClientSecretCredential(_config.AzureAdConfig.TenantId, _config.AzureAdConfig.ClientID, _config.AzureAdConfig.ClientSecret, options);
            _client = new GraphServiceClient(clientSecretCredential, scopes);
        }
    }
}
