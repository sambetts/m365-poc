using Azure;
using Azure.Data.Tables;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace Web.Models
{

    public class UserSession : ITableEntity
    {
        public string PartitionKey { get => PartitionKeyStaticVal; set { } }
        public string RowKey { get; set; } = String.Empty;
        public static string PartitionKeyStaticVal => "Sessions";
        public DateTimeOffset? Timestamp { get; set; } = DateTimeOffset.Now;
        public ETag ETag { get; set; }

        public string AppDetailsJson { get; set; } = null;

        [IgnoreDataMember]
        public AppDetails AppDetails
        {
            get
            {
                if (string.IsNullOrEmpty(AppDetailsJson))
                {
                    return new AppDetails();
                }
                try
                {
                    return JsonConvert.DeserializeObject<AppDetails>(AppDetailsJson);
                }
                catch (JsonException)
                {
                    return new AppDetails();
                }
            }
            set
            {
                AppDetailsJson = JsonConvert.SerializeObject(value);
            }
        }

        public string SavedManifestUrl { get; set; } = string.Empty;


        public static async Task<UserSession> AddNewSessionToAzTable(TableClient tableClient)
        {
            tableClient.CreateIfNotExists();

            var id = Guid.NewGuid();
            var newRandomSession = new UserSession { RowKey = id.ToString() };
            await tableClient.AddEntityAsync(newRandomSession);

            return newRandomSession;
        }

        public static async Task<UserSession?> GetSessionFromAzTable(string sessionId, TableClient tableClient)
        {
            Response<UserSession?> entityResponse = null;
            try
            {
                entityResponse = await tableClient.GetEntityAsync<UserSession>(PartitionKeyStaticVal, sessionId);
            }
            catch (RequestFailedException ex)
            {
                if (ex.ErrorCode == "ResourceNotFound")
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }

            return entityResponse;
        }

        public async Task UpdateTableRecord(UserSessionTableClient tableClient)
        {
            await tableClient.UpdateEntityAsync<UserSession>(this, new ETag("*"));
        }
    }
}
