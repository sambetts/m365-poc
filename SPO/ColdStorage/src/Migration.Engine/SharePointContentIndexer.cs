using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.EntityFrameworkCore;
using Entities;
using Entities.Configuration;
using Migration.Engine.Migration;
using Migration.Engine.SnapshotBuilder;
using Migration.Engine.Utils;
using Models;

using Microsoft.Extensions.Logging;
namespace Migration.Engine;

/// <summary>
/// Finds files to migrate in a SharePoint site-collection via Microsoft Graph drives
/// and queues each one as a Service Bus message for the migrator to process.
/// </summary>
public class SharePointContentIndexer : BaseComponent
{
    #region Constructors & Privates

    private readonly BlobServiceClient _blobServiceClient;
    private BlobContainerClient? _containerClient;
    private readonly SharePointFileMigrator _sharePointFileMigrator;

    public SharePointContentIndexer(Config config, ILogger ILogger) : base(config, ILogger)
    {
        var sbConnectionProps = ServiceBusConnectionStringProperties.Parse(_config.ConnectionStrings.ServiceBus);
        _logger.LogWarning($"Sending new SharePoint files to migrate to service-bus '{sbConnectionProps.Endpoint}'.");

        // Create BlobServiceClient with appropriate authentication based on connection string type
        _blobServiceClient = BlobServiceClientFactory.Create(_config.ConnectionStrings.Storage, _config);
        _sharePointFileMigrator = new SharePointFileMigrator(config, _logger);
    }

    #endregion

    public async Task StartMigrateAllSites()
    {
        // Create the container and return a container client object
        this._containerClient = _blobServiceClient.GetBlobContainerClient(_config.BlobContainerName);

        // Try to create container with no access to public
        // If container already exists or we don't have permission to create, continue anyway
        try
        {
            await _containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 403 || ex.Status == 409)
        {
            _logger.LogInformation($"Container '{_config.BlobContainerName}' not created (may already exist or insufficient permissions): {ex.Message}");

            try
            {
                if (!await _containerClient.ExistsAsync())
                {
                    throw new InvalidOperationException($"Container '{_config.BlobContainerName}' does not exist and service principal does not have permission to create it. Please create the container manually or assign Storage Blob Data Contributor role.", ex);
                }
            }
            catch (Azure.RequestFailedException existsEx) when (existsEx.Status == 403)
            {
                _logger.LogWarning($"Cannot verify container '{_config.BlobContainerName}' existence due to insufficient permissions. Assuming container exists. Please ensure service principal has 'Storage Blob Data Contributor' role assigned.");
            }
        }

        using var db = new SPOColdStorageDbContext(this._config);
        var sitesToMigrate = await db.TargetSharePointSites.ToListAsync();
        _logger.LogWarning($"Found {sitesToMigrate.Count} site-collections to migrate.");
        foreach (var s in sitesToMigrate)
        {
            await StartSiteMigration(s.RootURL);
        }
    }

    async Task StartSiteMigration(string siteUrl)
    {
        _logger.LogInformation($"Scanning site-collection '{siteUrl}' via Graph drives API...");

        var snapshotModel = new SiteSnapshotModel { Started = DateTime.UtcNow };
        var driveBuilder = new GraphDriveSnapshotBuilder(_config, siteUrl, _logger);

        // GraphDriveSnapshotBuilder fires a sync Action<>; queue each batch on the same thread
        // (queueing is just blob check + Service Bus enqueue, so synchronous waits are acceptable).
        await driveBuilder.BuildSnapshotAsync(snapshotModel, 100,
            batch => QueueFilesAsync(batch).GetAwaiter().GetResult());
    }

    async Task QueueFilesAsync(List<SharePointFileInfoWithList> batch)
    {
        foreach (var file in batch)
        {
            if (file is DriveItemSharePointFileInfo driveItemFile)
            {
                await _sharePointFileMigrator.QueueSharePointFileMigrationIfNeeded(driveItemFile, _containerClient!);
            }
            else
            {
                _logger.LogWarning($"Skipping file '{file.FullSharePointUrl}' - not a Graph drive item, cannot be migrated.");
            }
        }
    }
}
