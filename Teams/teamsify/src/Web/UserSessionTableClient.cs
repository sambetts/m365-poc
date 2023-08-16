using Azure.Data.Tables;
using Azure.Storage.Blobs;

namespace Web
{
    public class UserSessionTableClient : TableClient
    {
        const string TABLE_NAME = "Sessions";

        public UserSessionTableClient(string connectionString) : base(connectionString, TABLE_NAME)
        { 
        }
    }

    public class UserManifestsBlobContainerClient : BlobContainerClient
    {
        const string CONTAINER_NAME = "manifests";

        public UserManifestsBlobContainerClient(string connectionString) : base(connectionString, CONTAINER_NAME)
        {
        }
    }
}