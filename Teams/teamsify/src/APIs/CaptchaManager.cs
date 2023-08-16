
namespace APIs
{
    public class CaptchaManager
    {
        private readonly string _captchSecret;
        private readonly HttpClient _httpClient;
        public CaptchaManager(string captchSecret)
        {
            this._captchSecret = captchSecret;
            _httpClient = new HttpClient();
        }

        public async Task<bool> Verify(string captchaPageResponse)
        { 
            // Validate recaptcha - https://developers.google.com/recaptcha/docs/verify
            var captchSecret = System.Configuration.ConfigurationManager.AppSettings["CaptchaSecret"];

            var uri = $"https://www.google.com/recaptcha/api/siteverify?secret={captchSecret}&response={captchaPageResponse}";

            var recaptchaHttpResponse = await _httpClient.GetAsync(uri);
            var recaptchaResponseBody = await recaptchaHttpResponse.Content.ReadAsStringAsync();
            if (!recaptchaHttpResponse.IsSuccessStatusCode)
            {
                return false;
            }
            var captchaResult = JsonConvert.DeserializeObject<CaptchaResponse>(recaptchaResponseBody);

            return captchaResult.Success;
        }
    }
    public class CaptchaResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; } = false;
    }
}