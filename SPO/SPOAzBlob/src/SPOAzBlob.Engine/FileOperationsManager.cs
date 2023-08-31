using CommonUtils;
using Microsoft.Graph;
using SPOAzBlob.Engine.Models;

namespace SPOAzBlob.Engine
{
    /// <summary>
    /// High-level server-side funcionality of solution
    /// </summary>
    public class FileOperationsManager : AbstractGraphManager
    {
        #region Privates & Constructor

        private HttpClient _httpClient;
        private AzureStorageManager _azureStorageManager;
        private SPManager _spManager;

        public FileOperationsManager(Config config, DebugTracer trace) : base(config, trace)
        {
            _httpClient = new HttpClient();
            _azureStorageManager = new AzureStorageManager(config, trace);
            _spManager = new SPManager(config, trace);
        }

        #endregion

        /// <summary>
        /// User wants to start editing a file. Copy to SPO and create lock
        /// </summary>
        public async Task<DriveItem> StartFileEditInSpo(string azFileUrlWithSAS, string userName)
        {
            var azFileUri = new Uri(azFileUrlWithSAS);
            string fileTitle = _azureStorageManager.GetFileTitleFromFQDN(azFileUri);

            // See if file already exists
            DriveItem? existingSpoDriveItem = null;
            try
            {
                existingSpoDriveItem = await _client.Sites[_config.SharePointSiteId].Drive.Root.ItemWithPath(fileTitle)
                    .Request()
                    .GetAsync();
            }
            catch (ServiceException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // File not found. This is fine.
                }
                else throw;   // Something else.
            }

            // Check if this file is already being edited
            if (existingSpoDriveItem != null)
            {
                // Do we have a lock for this file?
                var existingItemLock = await _azureStorageManager.GetLock(existingSpoDriveItem);
                if (existingItemLock != null)
                {
                    throw new SpoFileAlreadyBeingEditedException();
                }
            }

            // Get file from az blob & upload to SPO
            DriveItem? newFile = null;
            using (var fs = await _httpClient.GetStreamAsync(azFileUrlWithSAS))
                newFile = await _spManager.UploadDoc(fileTitle, fs);

            // Create lock for new file
            await _azureStorageManager.SetOrUpdateLock(newFile, azFileUri.AbsoluteUri.Replace(azFileUri.Query, string.Empty), userName);
            return newFile;
        }

        /// <summary>
        /// Get recent updates (or all files if 1st time). For each lock, update Azure blob contents for corresponding SP file
        /// </summary>
        public async Task<List<DriveItem>> ProcessSpoUpdatesForActiveLocks()
        {
            // Figure out latest changes
            var spManager = new SPManager(_config, _trace);
            var spItemsChanged = await spManager.GetUpdatedDriveItems(_azureStorageManager);

            var updatedItemsInAzureBlob = new List<DriveItem>();
            if (spItemsChanged.Count > 0)
            {

                // If something has changed in SPO, compare locks to change list
                var driveId = spItemsChanged[0].ParentReference.DriveId;
                var allCurrentLocks = await _azureStorageManager.GetLocks(driveId);

                _trace.TrackTrace($"{nameof(ProcessSpoUpdatesForActiveLocks)}: Found {spItemsChanged.Count} SharePoint updates and {allCurrentLocks.Count} active locks.");

                // Only worry about updated files with locks
                foreach (var currentLock in allCurrentLocks)
                {
                    var spoDriveItem = spItemsChanged.Where(i => i.Id == currentLock.RowKey).SingleOrDefault();
                    if (spoDriveItem != null)
                    {
                        var success = false;
                        try
                        {
                            // Send to Azure blob
                            success = await UpdateAzureFile(spoDriveItem, currentLock);
                        }
                        catch (UpdateConflictException ex)
                        {
                            _trace.TrackTrace("Couldn't update Azure file from updated SPO file.", Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error);
                            _trace.TrackExceptionAndLogToTrace(ex);
                        }

                        if (success)
                        {
                            updatedItemsInAzureBlob.Add(spoDriveItem);
                        }
                    }
                }

            }

            _trace.TrackTrace($"{nameof(ProcessSpoUpdatesForActiveLocks)}: Updated {updatedItemsInAzureBlob.Count} Azure blob files.");

            return updatedItemsInAzureBlob;
        }


        /// <summary>
        /// Clear lock, update Azure file, and try deleting SPO file.
        /// </summary>
        public async Task FinishEditing(string driveItemId, string driveId)
        {
            var locks = await _azureStorageManager.GetLocks(driveId);

            foreach (var l in locks)
            {
                if (l.RowKey == driveItemId)
                {
                    // Found our target
                    await FinishEditing(l);
                    return;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(driveItemId));
        }

        async Task FinishEditing(FileLock existingLock)
        {
            // Figure out latest changes
            var spManager = new SPManager(_config, _trace);
            var spItemsChanged = await spManager.GetUpdatedDriveItems(_azureStorageManager);

            // One last update back to blob storage?
            var spm = new SPManager(_config, base._trace);
            var spItem = await spm.GetDriveItem(existingLock.RowKey); // RowKey == driveItemId
            var neededUpdate = await UpdateAzureFile(spItem, existingLock);

            // Allow others to edit
            await _azureStorageManager.ClearLock(existingLock);

            // Clean file. May fail if editing was just done
            try
            {
                await spm.DeleteFile(existingLock.RowKey);
            }
            catch (ServiceException ex)
            {
                _trace.TrackExceptionAndLogToTrace(ex);
                _trace.TrackTrace($"Couldn't clean-up file in SPO - see logged exception.");
            }

            _trace.TrackTrace($"{nameof(FinishEditing)}: {existingLock.AzureBlobUrl} unlocked. Needed update from SPO: {neededUpdate}.");
        }

        /// <summary>
        /// Clean anything in SPO without an active lock
        /// </summary>
        public async Task<int> CleanOldFiles(string driveId)
        {
            var locks = await _azureStorageManager.GetLocks(driveId);
            var cleaned = 0;

            var spManager = new SPManager(_config, _trace);
            var allItems = await spManager.GetDriveItems();
            foreach (var spItem in allItems)
            {
                if (!locks.Where(l => l.RowKey == spItem.Id).Any())
                {
                    try
                    {
                        await spManager.DeleteFile(spItem.Id);
                        cleaned++;
                    }
                    catch (ServiceException ex)
                    {
                        _trace.TrackExceptionAndLogToTrace(ex);
                        _trace.TrackTrace($"Couldn't clean-up file in SPO - see logged exception.");
                    }
                }
            }

            return cleaned;
        }

        private async Task<bool> UpdateAzureFile(DriveItem spoDriveItem, FileLock currentLock)
        {
            var userName = GetUserName(spoDriveItem.LastModifiedBy);

            if (!SPManager.FileContentsSame(spoDriveItem, currentLock))
            {
                // Update lock 1st, otherwise UploadSharePointFileToAzureBlob will fail the lock check on contents
                currentLock.FileContentETag = spoDriveItem.CTag;
                await _azureStorageManager.SetOrUpdateLock(spoDriveItem, currentLock.AzureBlobUrl, userName);

                // Upload to Azure
                await _azureStorageManager.UploadSharePointFileToAzureBlob(spoDriveItem, userName);

                return true;
            }
            return false;
        }

        private string GetUserName(IdentitySet identitySet)
        {
            if (identitySet is null)
            {
                throw new ArgumentNullException(nameof(identitySet));
            }

            if (identitySet.User != null)
            {
                // Maybe use UPN here
                return identitySet.User.DisplayName;
            }
            else if (identitySet.Application != null)
            {
                return identitySet.Application.DisplayName;
            }

            throw new ArgumentOutOfRangeException(nameof(identitySet));
        }
    }
}
