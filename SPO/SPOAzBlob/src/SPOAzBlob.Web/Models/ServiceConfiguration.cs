namespace SPOAzBlob.Web.Models
{
    public class ServiceConfiguration
    {
        public StorageInfo StorageInfo { get; set; } = new StorageInfo();
        public string BaseSharePointDriveUrl { get; set; } = string.Empty;
        public string WebhookUrl { get; set; } = string.Empty;
    }

    public class StorageInfo
    {
        public string SharedAccessToken { get; set; } = string.Empty;
        public string AccountURI { get; set; } = string.Empty;
        public string ContainerName { get; set; } = string.Empty;
    }
}
