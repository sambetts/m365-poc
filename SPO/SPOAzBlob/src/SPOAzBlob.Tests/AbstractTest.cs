using Azure.Identity;
using CommonUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPOAzBlob.Tests
{
    public abstract class AbstractTest
    {

        protected TestConfig? _config;
        protected DebugTracer _tracer = DebugTracer.ConsoleOnlyTracer();
        protected GraphServiceClient? _client;
        [TestInitialize]
        public void Init()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", true);


            var config = builder.Build();
            _config = new TestConfig(config);


            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };
            var scopes = new[] { "https://graph.microsoft.com/.default" };

            var clientSecretCredential = new ClientSecretCredential(_config.AzureAdConfig.TenantId, _config.AzureAdConfig.ClientID, _config.AzureAdConfig.Secret, options);
            _client = new GraphServiceClient(clientSecretCredential, scopes);


        }
    }
}
