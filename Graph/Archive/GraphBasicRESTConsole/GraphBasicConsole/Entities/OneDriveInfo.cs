
using System;
using Newtonsoft.Json;


namespace GraphBasicConsole.Entities
{
    /// <summary>
    /// Response from 
    /// </summary>
    public partial class OneDriveInfo
    {
        [JsonProperty("@odata.context")]
        public Uri OdataContext { get; set; }

        [JsonProperty("createdDateTime")]
        public DateTimeOffset CreatedDateTime { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("lastModifiedDateTime")]
        public DateTimeOffset LastModifiedDateTime { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("webUrl")]
        public Uri WebUrl { get; set; }

        [JsonProperty("driveType")]
        public string DriveType { get; set; }

        [JsonProperty("createdBy")]
        public CreatedBy CreatedBy { get; set; }

        [JsonProperty("lastModifiedBy")]
        public LastModifiedBy LastModifiedBy { get; set; }

        [JsonProperty("owner")]
        public LastModifiedBy Owner { get; set; }

        [JsonProperty("quota")]
        public Quota Quota { get; set; }
    }

    public partial class CreatedBy
    {
        [JsonProperty("user")]
        public CreatedByUser User { get; set; }
    }

    public partial class CreatedByUser
    {
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
    }

    public partial class LastModifiedBy
    {
        [JsonProperty("user")]
        public LastModifiedByUser User { get; set; }
    }

    public partial class LastModifiedByUser
    {
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
    }

    public partial class Quota
    {
        [JsonProperty("deleted")]
        public long Deleted { get; set; }

        [JsonProperty("remaining")]
        public long Remaining { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("total")]
        public long Total { get; set; }

        [JsonProperty("used")]
        public long Used { get; set; }
    }

    public partial class OneDriveInfo
    {
        public static OneDriveInfo FromJson(string json) => JsonConvert.DeserializeObject<OneDriveInfo>(json, JSonConverter.Settings);
    }


}
