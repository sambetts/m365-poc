using Azure;
using Azure.Data.Tables;
using Microsoft.Graph;

namespace SPOAzBlob.Engine.Models
{
    public class FileLock : ITableEntity
    {
        public FileLock()
        { 
        }

        public FileLock(DriveItem driveItem, string azureBlobUrl, string userName)
        {
            // Partition by drive
            this.PartitionKey = driveItem.ParentReference.DriveId;
            
            // Key is encoded URL
            this.RowKey = driveItem.Id;

            this.AzureBlobUrl = azureBlobUrl;
            this.WebUrl = driveItem.WebUrl;
            this.FileContentETag = driveItem.CTag;
            this.LockedByUser = userName;
        }

        public string LockedByUser { get; set; } = string.Empty;
        public string WebUrl { get; set; } = string.Empty;
        public string AzureBlobUrl { get; set; } = string.Empty;
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = String.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string FileContentETag { get; set; } = string.Empty;
    }
}
