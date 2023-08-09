using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Globalization;

namespace GraphHeadlessCSharp.JSonEntities
{
    /// <summary>
    /// Convertion rules for JSon strings
    /// </summary>
    internal static class JsonConverter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}
