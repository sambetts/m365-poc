using Newtonsoft.Json;

namespace GraphBasicConsole.Entities
{
    /// <summary>
    /// An access token returned by Azure AD. 
    /// </summary>
    public partial class AccessToken
    {
        public string token_type { get; set; }
        public string access_token { get; set; }
        public string refresh_token { get; set; }
    }

    public partial class AccessToken
    {
        public static AccessToken FromJson(string json) => JsonConvert.DeserializeObject<AccessToken>(json, JSonConverter.Settings);
    }
}
