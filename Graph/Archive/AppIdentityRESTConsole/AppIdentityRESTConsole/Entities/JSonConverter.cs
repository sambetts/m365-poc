using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Globalization;

namespace AppIdentityRESTConsole.Entities
{
    /// <summary>
    /// Class to host conversion settings for JSon serialisation
    /// </summary>
    internal static class JSonAppConverter
    {
        /// <summary>
        /// Use our app config.
        /// </summary>
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
