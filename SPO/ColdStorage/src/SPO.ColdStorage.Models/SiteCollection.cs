using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SPO.ColdStorage.Models
{
    public class SiteCollectionsResult : GraphPageableResponse<SiteCollection>
    {
    }

    public class SiteCollection : BaseGraphObject
    {
        [JsonPropertyName("createdDateTime")]
        public DateTime? CreatedDateTime { get; set; }


        [JsonPropertyName("lastModifiedDateTime")]
        public DateTime LastModifiedDateTime { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("webUrl")]
        public string WebUrl { get; set; } = string.Empty;

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;
    }
}
