using Azure.Identity;
using Entities.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Models;

namespace Migration.Engine.Migration;

/// <summary>
/// Downloads files from SharePoint Online to local file-system using Microsoft Graph drives API.
/// Streams content chunk-wise so files larger than 2 GB are safe.
/// </summary>
public class SharePointFileDownloader : BaseComponent
{
    private readonly GraphServiceClient _graphClient;

    public SharePointFileDownloader(Config config, ILogger ILogger) : base(config, ILogger)
    {
        var credential = new ClientSecretCredential(
            _config.AzureAdConfig.TenantId,
            _config.AzureAdConfig.ClientID,
            _config.AzureAdConfig.Secret);

        _graphClient = new GraphServiceClient(credential);
    }

    /// <summary>
    /// Download file via Graph drive item content endpoint and return temp file-name + size.
    /// </summary>
    public async Task<(string, long)> DownloadFileToTempDir(DriveItemSharePointFileInfo sharePointFile)
    {
        if (string.IsNullOrEmpty(sharePointFile.DriveId) || string.IsNullOrEmpty(sharePointFile.GraphItemId))
        {
            throw new InvalidOperationException($"DriveId and GraphItemId are required to download '{sharePointFile.FullSharePointUrl}'.");
        }

        var tempFileName = GetTempFileNameAndCreateDir(sharePointFile);

        _logger.LogDebug($"Downloading '{sharePointFile.FullSharePointUrl}' via Graph drives/{sharePointFile.DriveId}/items/{sharePointFile.GraphItemId}...");

        long fileSize;
        using (var sourceStream = await _graphClient.Drives[sharePointFile.DriveId]
            .Items[sharePointFile.GraphItemId]
            .Content
            .GetAsync())
        {
            if (sourceStream == null)
            {
                throw new InvalidOperationException($"Graph returned no content stream for '{sharePointFile.FullSharePointUrl}'.");
            }

            using var streamToWriteTo = File.Open(tempFileName, FileMode.Create);
            await sourceStream.CopyToAsync(streamToWriteTo);
            fileSize = streamToWriteTo.Length;
        }

        _logger.LogDebug($"Wrote {fileSize:N0} bytes to '{tempFileName}'.");

        return (tempFileName, fileSize);
    }

    public static string GetTempFileNameAndCreateDir(BaseSharePointFileInfo sharePointFile)
    {
        var tempFileName = Path.GetTempPath() + @"\SpoColdStorageMigration\" + DateTime.Now.Ticks + @"\" + sharePointFile.ServerRelativeFilePath.Replace("/", @"\");
        var tempFileInfo = new FileInfo(tempFileName);
        Directory.CreateDirectory(tempFileInfo.DirectoryName!);

        return tempFileName;
    }
}
