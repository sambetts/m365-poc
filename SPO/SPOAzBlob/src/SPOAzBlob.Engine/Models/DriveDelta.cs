namespace SPOAzBlob.Engine.Models
{
    /// <summary>
    /// Graph drive delta code + timestamp
    /// </summary>
    public class DriveDelta
    {
        public DriveDelta() { }
        public DriveDelta(PropertyBagEntry propertyBagEntry)
        {
            this.Code = propertyBagEntry.Value;
            if (propertyBagEntry.Timestamp.HasValue)
            {
                this.Timestamp = propertyBagEntry.Timestamp.Value;
            }
            else
            {
                throw new ArgumentNullException(nameof(propertyBagEntry.Timestamp));
            }
        }

        /// <summary>
        /// Find the delta token in a Graph request URL
        /// </summary>
        public static string? ExtractCodeFromGraphUrl(string graphUrl)
        {
            const string TOKEN = "abc1232";
            var testUrl = $"https://graph.microsoft.com/v1.0/sites('contoso.sharepoint.com,guid,guid')/drive/root/microsoft.graph.delta(token='{TOKEN}')?$expand=LastModifiedByUser";

            const string TOKEN_START = "token='";
            var tokenEqualStart = graphUrl.IndexOf(TOKEN_START);
            var tokenStart = tokenEqualStart + TOKEN.Length;
            if (tokenEqualStart > -1)
            {
                var tokenEnd = graphUrl.IndexOf("'", tokenStart);
                if (tokenEnd > -1)
                {
                    var token = graphUrl.Substring(tokenStart, tokenEnd - tokenStart);
                    return token;
                }
            }

            return null;
        }

        public string Code { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }
}
