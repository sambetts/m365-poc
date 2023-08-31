using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using CommonUtils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using SPOAzBlob.Engine;
using SPOAzBlob.Web.Models;

namespace SPO.ColdStorage.Web.Controllers
{
    /// <summary>
    /// Handles React app requests for app configuration
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ApiController]
    [Route("[controller]")]
    public class AppConfigurationController : ControllerBase
    {
        private readonly DebugTracer _tracer;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly Config _config;

        public AppConfigurationController(Config config, DebugTracer tracer, GraphServiceClient graphServiceClient)
        {
            _tracer = tracer;
            this._graphServiceClient = graphServiceClient;
            this._config = config;
        }


        // Generate app ServiceConfiguration + storage configuration + key to read blobs
        // GET: AppConfiguration/ServiceConfiguration
        [HttpGet("[action]")]
        public async Task<ActionResult<ServiceConfiguration>> GetServiceConfiguration()
        {
            var client = new BlobServiceClient(_config.ConnectionStrings.Storage);

            var driveInfo = await _graphServiceClient.Sites[_config.SharePointSiteId].Drive.Root.Request().GetAsync();

            // Generate a new shared-access-signature
            var sasUri = client.GenerateAccountSasUri(AccountSasPermissions.List | AccountSasPermissions.Read,
                DateTime.Now.AddDays(1),
                AccountSasResourceTypes.Container | AccountSasResourceTypes.Object);

            // Return for react app
            return new ServiceConfiguration 
            {
                BaseSharePointDriveUrl = driveInfo.WebUrl,
                WebhookUrl = _config.WebhookUrlOverride,
                StorageInfo = new StorageInfo
                {
                    AccountURI = client.Uri.ToString(),
                    SharedAccessToken = sasUri.Query,
                    ContainerName = _config.BlobContainerName
                }
            };
        }
    }
}
