using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Entities;
using Entities.Configuration;
using Models;
using Web.Models;

namespace Web.Controllers;
/// <summary>
/// Handles React app requests for app configuration
/// </summary>
[Microsoft.AspNetCore.Authorization.Authorize]
[ApiController]
[Route("[controller]")]
public class AppConfigurationController(SPOColdStorageDbContext context, Config config, ILogger<AppConfigurationController> logger) : ControllerBase
{
    private readonly ILogger<AppConfigurationController> _logger = logger;
    private readonly SPOColdStorageDbContext _context = context;
    private readonly Config _config = config;

    // Generate app ServiceConfiguration + storage configuration + key to read blobs
    // GET: AppConfiguration/ServiceConfiguration
    [HttpGet("[action]")]
    public ActionResult<ServiceConfiguration> GetServiceConfiguration()
    {
        var client = new BlobServiceClient(_config.ConnectionStrings.Storage);

        // Generate a new shared-access-signature
        var sasUri = client.GenerateAccountSasUri(AccountSasPermissions.List | AccountSasPermissions.Read,
            DateTime.Now.AddDays(1),
            AccountSasResourceTypes.Container | AccountSasResourceTypes.Object);

        // Return for react app
        return new ServiceConfiguration
        {
            StorageInfo = new StorageInfo
            {
                AccountURI = client.Uri.ToString(),
                SharedAccessToken = sasUri.Query,
                ContainerName = _config.BlobContainerName
            },
            SearchConfiguration = new SearchConfiguration
            {
                IndexName = _config.SearchConfig.IndexName,
                QueryKey = _config.SearchConfig.QueryKey,
                ServiceName = _config.SearchConfig.ServiceName
            }
        };
    }

    // GET: AppConfiguration/GetGetMigrationTargets
    [HttpGet("[action]")]
    public async Task<ActionResult<IEnumerable<SiteListFilterConfig>>> GetMigrationTargets()
    {
        var targets = await _context.TargetSharePointSites.ToListAsync();
        var returnList = new List<SiteListFilterConfig>();
        foreach (var target in targets)
            returnList.Add(target.ToSiteListFilterConfig());

        return returnList;
    }

    /// <summary>
    /// Set migration config. Validates that the configured app registration can reach each
    /// site via Microsoft Graph (Sites.Read.All).
    /// </summary>
    // POST: AppConfiguration/SetMigrationTargets
    [HttpPost("[action]")]
    public async Task<ActionResult> SetMigrationTargets(List<SiteListFilterConfig> targets)
    {
        if (targets == null || targets.Count == 0)
        {
            return BadRequest($"{nameof(targets)} is null");
        }
        foreach (var target in targets)
        {
            if (!target.IsValid)
            {
                return BadRequest("Invalid config data");
            }
        }

        // Build Graph client up front - validates app registration is reachable
        GraphServiceClient graphClient;
        try
        {
            var credential = new ClientSecretCredential(
                _config.AzureAdConfig.TenantId,
                _config.AzureAdConfig.ClientID,
                _config.AzureAdConfig.Secret);
            graphClient = new GraphServiceClient(credential);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error initialising Microsoft Graph client");
            return BadRequest($"Got '{ex.Message}' initialising Microsoft Graph client. Check service configuration.");
        }

        // Remove old target configuration & set new
        var oldTargetSites = await _context.TargetSharePointSites.ToListAsync();
        _context.TargetSharePointSites.RemoveRange(oldTargetSites);

        // Verify each site exists via Graph (using {hostname}:{site-path} syntax)
        foreach (var target in targets)
        {
            try
            {
                var uri = new Uri(target.RootURL);
                var graphSiteRef = $"{uri.Host}:{uri.AbsolutePath}";
                var site = await graphClient.Sites[graphSiteRef].GetAsync();
                if (site == null || string.IsNullOrEmpty(site.Id))
                {
                    return BadRequest($"Site '{target.RootURL}' could not be resolved via Microsoft Graph.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating site '{target.RootURL}'");
                return BadRequest($"Got '{ex.Message}' validating SPO site URL '{target.RootURL}'. It's not a valid SharePoint site-collection URL or the app registration lacks Graph 'Sites.Read.All' permission?");
            }

            var dbObj = new Entities.DBEntities.TargetMigrationSite(target);

            // Assuming no error, save to SQL
            _context.TargetSharePointSites.Add(dbObj);
        }
        await _context.SaveChangesAsync();

        return Ok();
    }
}
