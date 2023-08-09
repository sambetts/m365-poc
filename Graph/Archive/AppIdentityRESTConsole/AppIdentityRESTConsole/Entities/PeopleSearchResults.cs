using Newtonsoft.Json;
using System;

namespace AppIdentityRESTConsole.Entities
{
    public partial class Users
    {
        [JsonProperty("@odata.context")]
        public Uri OdataContext { get; set; }

        [JsonProperty("value")]
        public UsersValue[] Value { get; set; }
    }

    public partial class UsersValue
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("businessPhones")]
        public string[] BusinessPhones { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("givenName")]
        public string GivenName { get; set; }

        [JsonProperty("jobTitle")]
        public string JobTitle { get; set; }

        [JsonProperty("mail")]
        public string Mail { get; set; }

        [JsonProperty("mobilePhone")]
        public string MobilePhone { get; set; }

        [JsonProperty("officeLocation")]
        public string OfficeLocation { get; set; }


        [JsonProperty("surname")]
        public string Surname { get; set; }

        [JsonProperty("userPrincipalName")]
        public string UserPrincipalName { get; set; }
    }

    public partial class Users
    {
        public static Users FromJson(string json) => JsonConvert.DeserializeObject<Users>(json, JSonAppConverter.Settings);
    }
    
}
