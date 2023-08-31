using Azure.Data.Tables;
using Azure.Storage.Blobs;
using CommonUtils;
using Microsoft.Graph;
using SPOAzBlob.Engine.Models;

namespace SPOAzBlob.Engine
{
    /// <summary>
    /// Handles Azure blob & table storage
    /// </summary>
    public class AzureStorageManager : AbstractGraphManager
    {
        #region Constructors

        private BlobServiceClient _blobServiceClient;
        private TableServiceClient _tableServiceClient;
        public AzureStorageManager(Config config, DebugTracer trace) : base(config, trace)
        {
            _tableServiceClient = new TableServiceClient(_config.ConnectionStrings.Storage);
            _blobServiceClient = new BlobServiceClient(_config.ConnectionStrings.Storage);
        }
        #endregion

        public async Task<Uri> UploadSharePointFileToAzureBlob(string fileTitle, string userName)
        {
            // Get drive item
            var driveItem = await _client.Sites[_config.SharePointSiteId].Drive.Root.ItemWithPath(fileTitle).Request().GetAsync();

            return await UploadSharePointFileToAzureBlob(driveItem, userName);
        }
        public async Task<Uri> UploadSharePointFileToAzureBlob(DriveItem driveItem, string userName)
        {
            var existingLock = await GetLock(driveItem);
            LockCheck(driveItem, existingLock, userName, true);

            // Copy file from SPO to az blob
            var containerClient = _blobServiceClient.GetBlobContainerClient(_config.BlobContainerName);

            // Figure out dir from parent
            var dir = driveItem.ParentReference.Path.Replace("/drive/root:", string.Empty);

            string fileTitle = $"{dir}/{driveItem.Name}";

            var fileRef = containerClient.GetBlobClient(fileTitle);
            using (var fs = await _client.Sites[_config.SharePointSiteId].Drive.Root.ItemWithPath(fileTitle).Content.Request().GetAsync())
            {
                await fileRef.UploadAsync(fs, true);
            }

            return fileRef.Uri;
        }


        #region PropertyBag


        public async Task<PropertyBagEntry?> GetPropertyValue(string property)
        {
            var tableClient = await GetTableClient(_config.AzureTablePropertyBag);
            var queryResultsFilter = tableClient.QueryAsync<PropertyBagEntry>(f =>
                f.RowKey == property
            );

            // Iterate the <see cref="Pageable"> to access all queried entities.
            await foreach (var qEntity in queryResultsFilter)
            {
                return qEntity;
            }

            // No results
            return null;
        }
        public async Task SetPropertyValue(string property, string value)
        {
            var tableClient = await GetTableClient(_config.AzureTablePropertyBag);

            var entity = new PropertyBagEntry(property, value);

            // Entity doesn't exist in table, so invoking UpsertEntity will simply insert the entity.
            await tableClient.UpsertEntityAsync(entity);
        }


        public async Task ClearPropertyValue(string property)
        {
            var tableClient = await GetTableClient(_config.AzureTablePropertyBag);
            tableClient.DeleteEntity(PropertyBagEntry.PARTITION_NAME, property);
        }
        #endregion

        #region Locks

        public async Task<FileLock?> GetLock(DriveItem driveItem)
        {
            var tableClient = await GetTableClient(_config.AzureTableLocks);


            var queryResultsFilter = tableClient.QueryAsync<FileLock>(f =>
                f.RowKey == driveItem.Id &&
                f.PartitionKey == driveItem.ParentReference.DriveId
            );

            // Iterate the <see cref="Pageable"> to access all queried entities.
            await foreach (var qEntity in queryResultsFilter)
            {
                return qEntity;
            }

            // No results
            return null;
        }


        public async Task<List<FileLock>> GetLocks(string driveId)
        {
            var tableClient = await GetTableClient(_config.AzureTableLocks);
            var queryResultsFilter = tableClient.QueryAsync<FileLock>(f =>
                f.PartitionKey == driveId
            );

            var results = new List<FileLock>();
            await foreach (var qEntity in queryResultsFilter)
            {
                if (qEntity != null)
                    results.Add(qEntity);
            }

            return results;
        }

        public async Task SetOrUpdateLock(DriveItem driveItem, string azureBlobUrl, string userName)
        {
            var tableClient = await GetTableClient(_config.AzureTableLocks);

            // Don't overwrite a lock by someone else for this item
            var existingLock = await GetLock(driveItem);
            LockCheck(driveItem, existingLock, userName, false);

            var entity = new FileLock(driveItem, azureBlobUrl, userName);

            // Entity doesn't exist in table, so invoking UpsertEntity will simply insert the entity.
            await tableClient.UpsertEntityAsync(entity);
        }


        public async Task ClearLock(DriveItem driveItem)
        {
            var tableClient = await GetTableClient(_config.AzureTableLocks);

            tableClient.DeleteEntity(driveItem.ParentReference.DriveId, driveItem.Id);
        }
        public async Task ClearLock(FileLock l)
        {
            var tableClient = await GetTableClient(_config.AzureTableLocks);
            tableClient.DeleteEntity(l.PartitionKey, l.RowKey);
        }

        async Task<TableClient> GetTableClient(string tableName)
        {
            await _tableServiceClient.CreateTableIfNotExistsAsync(tableName);
            var tableClient = _tableServiceClient.GetTableClient(tableName);
            return tableClient;
        }


        private void LockCheck(DriveItem driveItem, FileLock? existingLock, string userName, bool checkContentsToo)
        {
            if (existingLock != null)
            {
                // Does someone else have another lock?
                if (existingLock.LockedByUser != userName)
                {
                    throw new SetLockFileLockedByAnotherUserException(existingLock.LockedByUser);
                }

                // Was the SP file updated before/after our lock?
                if (checkContentsToo && existingLock.FileContentETag != driveItem.CTag)
                {
                    throw new SetLockFileUpdateConflictException();
                }
            }
        }

        #endregion

        internal string GetFileTitleFromFQDN(Uri azFileUrlWithSAS)
        {
            var prefix = "/" + _config.BlobContainerName;

            var title = azFileUrlWithSAS.AbsolutePath.Replace(prefix, string.Empty);

            return title;
        }
    }
}
