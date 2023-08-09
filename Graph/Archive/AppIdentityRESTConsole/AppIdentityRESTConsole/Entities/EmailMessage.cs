using Newtonsoft.Json;
using System.Collections.Generic;

namespace AppIdentityRESTConsole.Entities
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/graph/api/resources/message?view=graph-rest-1.0#properties
    /// </summary>
    public class EmailMessage
    {
        public EmailMessage()
        {
            this.Message = new Message();
        }

        public static EmailMessage FromJson(string json) => JsonConvert.DeserializeObject<EmailMessage>(json, JSonAppConverter.Settings);

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("message")]
        public Message Message { get; set; }
    }

    public class EmailAddress
    {
        [JsonProperty("address")]
        public string Address { get; set; }
    }


    public class Message
    {
        public Message()
        {
            this.Body = new Body();
            this.ToRecipients = new List<Recipient>();
            this.From = new Recipient();
        }
        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("body")]
        public Body Body { get; set; }

        [JsonProperty("toRecipients")]
        public List<Recipient> ToRecipients { get; set; }

        [JsonProperty("from")]
        public Recipient From { get; set; }
    }

    public class Body
    {
        [JsonProperty("contentType")]
        public string ContentType { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }
    }

    public class Recipient
    {
        public Recipient()
        {
            this.EmailAddress = new EmailAddress();
        }

        [JsonProperty("emailAddress")]
        public EmailAddress EmailAddress { get; set; }
    }


    public static class Serialize
    {
        public static string ToJson(this EmailMessage self) => JsonConvert.SerializeObject(self, JSonAppConverter.Settings);
    }
    
}
