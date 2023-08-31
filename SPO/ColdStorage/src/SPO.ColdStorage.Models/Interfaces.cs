using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SPO.ColdStorage.Models
{
    public abstract class GraphPageableResponse<T> where T : BaseGraphObject
    {
        [JsonPropertyName("@odata.nextLink")]
        public string OdataNextLink { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public List<T> PageResults { get; set; } = new List<T>();
    }

    public abstract class BaseGraphObject
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }
}
