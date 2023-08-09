using Newtonsoft.Json;
using System;

namespace GraphHeadlessCSharp.JSonEntities
{
    // Big shout out to https://app.quicktype.io/ for the JSon class generators

    public partial class ListsResponse
    {
        [JsonProperty("@odata.context")]
        public Uri OdataContext { get; set; }

        [JsonProperty("value")]
        public Value[] Value { get; set; }
    }

    public partial class Value
    {
        [JsonProperty("@odata.etag")]
        public string OdataEtag { get; set; }

        [JsonProperty("createdDateTime")]
        public DateTimeOffset CreatedDateTime { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("eTag")]
        public string ETag { get; set; }

        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("lastModifiedDateTime")]
        public DateTimeOffset LastModifiedDateTime { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("webUrl")]
        public Uri WebUrl { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("createdBy")]
        public EdBy CreatedBy { get; set; }


        [JsonProperty("list")]
        public List List { get; set; }

        [JsonProperty("lastModifiedBy", NullValueHandling = NullValueHandling.Ignore)]
        public EdBy LastModifiedBy { get; set; }
    }

    public partial class EdBy
    {
        [JsonProperty("user")]
        public User User { get; set; }
    }

    public partial class User
    {
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("email", NullValueHandling = NullValueHandling.Ignore)]
        public string Email { get; set; }

        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public Guid? Id { get; set; }
    }

    public partial class List
    {
        [JsonProperty("contentTypesEnabled")]
        public bool ContentTypesEnabled { get; set; }

        [JsonProperty("hidden")]
        public bool Hidden { get; set; }

        [JsonProperty("template")]
        public string Template { get; set; }
    }


    public partial class ListsResponse
    {
        public static ListsResponse FromJson(string json) => JsonConvert.DeserializeObject<ListsResponse>(json, JsonConverter.Settings);
    }

}
