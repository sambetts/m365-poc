using Microsoft.AspNetCore.Mvc;

namespace APIs.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TeamsAppController
    {
        private UserSessionTableClient _tableClient;
        private UserManifestsBlobContainerClient _blobClient;
        private CaptchaManager _captchaManager;
        public TeamsAppController()
        {
            var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["Storage"]?.ConnectionString;

            _tableClient = new UserSessionTableClient(connectionString);
            _blobClient = new UserManifestsBlobContainerClient(connectionString);
            _captchaManager = new CaptchaManager(System.Configuration.ConfigurationManager.AppSettings["CaptchaSecret"]);
        }

        // POST api/TeamsApp/NewSession?captchaResponseOnPage={captchaResponseOnPage}
        [HttpPost]
        public async Task<HttpResponseMessage> NewSession(string captchaResponseOnPage)
        {
            var validCaptcha = await _captchaManager.Verify(captchaResponseOnPage);

            if (validCaptcha)
            {
                // Generate new session now we know it's a human
                var newSession = await UserSession.AddNewSessionToAzTable(_tableClient);
                var response = Request.CreateResponse(HttpStatusCode.OK);

                // Return session ID to JS app
                response.Content = new StringContent(newSession.RowKey);
                return response;
            }
            else
            {
                throw new Exception("Captcha validation failed");
            }
        }

        // POST api/TeamsApp/Contact?captchaResponseOnPage={captchaResponseOnPage}
        [HttpPost]
        public async Task<IHttpActionResult> Contact([FromBody] EmailMessage emailMessage, string captchaResponseOnPage)
        {
            if (emailMessage == null || !emailMessage.IsValid)
            {
                return BadRequest();
            }
            var validCaptcha = await _captchaManager.Verify(captchaResponseOnPage);
            if (validCaptcha)
            {
                var client = new SendGridClient(System.Configuration.ConfigurationManager.AppSettings["SendGridApiKey"]);

                var from = System.Configuration.ConfigurationManager.AppSettings["EmailsFrom"];
                var to = System.Configuration.ConfigurationManager.AppSettings["EmailsTo"];
                var msg = emailMessage.ToSendGridMessage(to, from);

                var response = await client.SendEmailAsync(msg);
                var responseBody = await response.Body.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Contact command failed with response '{response.StatusCode}' {responseBody}.");
                return
                    Ok(responseBody);
            }
            else
            {
                throw new Exception("Captcha validation failed");
            }
        }


        // POST api/TeamsApp/CreateApp?url={url}&sessionId={sessionId}
        [HttpPost]
        public async Task<HttpResponseMessage> CreateApp([FromBody] AppDetails appDetails, string url, string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(url) || appDetails == null || !appDetails.IsValid)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

            var sesh = await UserSession.GetSessionFromAzTable(sessionId, _tableClient);

            if (sesh == null) // No session with that ID
                return Request.CreateResponse(HttpStatusCode.NotFound);

            var manifest = appDetails.ToTeamsAppManifest(url);
            var bytes = manifest.BuildZip(appDetails.EntityName);

            // Save file to blob storage
            await _blobClient.CreateIfNotExistsAsync();

            var fileName = $"{DateTime.Now.Ticks}/{appDetails.EntityName}.zip";
            Response<BlobContentInfo> manifestRef = null;
            using (var ms = new MemoryStream(bytes))
            {
                manifestRef = await _blobClient.UploadBlobAsync(fileName, ms);
            }

            // Remember deets in cache
            sesh.AppDetails = appDetails;
            sesh.SavedManifestUrl = fileName;
            await sesh.UpdateTableRecord(_tableClient);

            // Respond with filename
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(fileName);
            return response;
        }

        // GET api/TeamsApp/DownloadApp?fileUrl={fileUrl}&sessionId={sessionId}
        [HttpGet]
        public async Task<HttpResponseMessage> DownloadApp(string fileUrl, string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(fileUrl))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

            var sesh = await UserSession.GetSessionFromAzTable(sessionId, _tableClient);

            if (sesh == null) // No session with that ID
                return Request.CreateResponse(HttpStatusCode.NotFound);


            var blob = _blobClient.GetBlobClient(fileUrl);
            var exists = await blob.ExistsAsync();
            if (!exists)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }

            var url = blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.Now.AddMinutes(2));

            // Respond with filename
            var response = Request.CreateResponse(HttpStatusCode.Moved);
            response.Headers.Location = url;

            return response;
        }
    }
}
