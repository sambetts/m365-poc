using Newtonsoft.Json;
using SendGrid.Helpers.Mail;

namespace Web.Models
{
    public class EmailMessage
    {
        [JsonProperty("from")]
        public string From { get; set; } = string.Empty;

        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        public bool IsValid => !string.IsNullOrWhiteSpace(From) && !string.IsNullOrWhiteSpace(Message);

        internal SendGridMessage ToSendGridMessage(string to, string from)
        {
            if (string.IsNullOrEmpty(to))
            {
                throw new ArgumentException($"'{nameof(to)}' cannot be null or empty.", nameof(to));
            }
            if (string.IsNullOrEmpty(from))
            {
                throw new ArgumentException($"'{nameof(from)}' cannot be null or empty.", nameof(to));
            }

            var msg = new SendGridMessage()
            {
                From = new EmailAddress(from),
                ReplyTo = new EmailAddress(to),
                Subject = "Teamsify Contact",
                PlainTextContent = $"From: {from}: {Environment.NewLine}{this.Message}"
            };
            msg.AddTo(to);

            return msg;
        }
    }
}
