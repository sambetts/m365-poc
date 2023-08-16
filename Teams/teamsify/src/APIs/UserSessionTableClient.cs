using Azure.Data.Tables;
using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APIs
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