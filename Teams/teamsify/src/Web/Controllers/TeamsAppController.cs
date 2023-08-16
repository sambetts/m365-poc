using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Mvc;
using Web.Config;
using Web.Models;

namespace Web
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeamsAppController : ControllerBase
    {
        private UserSessionTableClient _tableClient;
        private UserManifestsBlobContainerClient _blobClient;
        private CaptchaManager _captchaManager;
        private readonly WebAppConfig _config;

        public TeamsAppController(WebAppConfig config)
        {
            this._config = config;

            _tableClient = new UserSessionTableClient(config.ConnectionStrings.Storage);
            _blobClient = new UserManifestsBlobContainerClient(config.ConnectionStrings.Storage);
            _captchaManager = new CaptchaManager(_config.CaptchaSecret);
        }

        // POST api/TeamsApp/NewSession?captchaResponseOnPage={captchaResponseOnPage}
        [HttpPost("[action]")]
        public async Task<IActionResult> NewSession(string captchaResponseOnPage)
        {
            var validCaptcha = await _captchaManager.Verify(captchaResponseOnPage);

            if (validCaptcha)
            {
                // Generate new session now we know it's a human
                var newSession = await UserSession.AddNewSessionToAzTable(_tableClient);

                // Return session ID to JS app
                var response = Ok(newSession.RowKey);

                return response;
            }
            else
            {
                throw new Exception("Captcha validation failed");
            }
        }

        // POST api/TeamsApp/Contact?captchaResponseOnPage={captchaResponseOnPage}
        [HttpPost("[action]")]
        public async Task<IActionResult> Contact([FromBody] EmailMessage emailMessage, string captchaResponseOnPage)
        {
            if (emailMessage == null || !emailMessage.IsValid)
            {
                return BadRequest();
            }
            var validCaptcha = await _captchaManager.Verify(captchaResponseOnPage);
            if (validCaptcha)
            {
                var client = new SendGrid.SendGridClient(_config.SendGridApiKey);

                var from = _config.EmailsFrom;
                var to = _config.EmailsTo;
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
        [HttpPost("[action]")]
        public async Task<IActionResult> CreateApp([FromBody] AppDetails appDetails, string url, string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(url) || appDetails == null || !appDetails.IsValid)
            {
                return BadRequest();
            }

            var sesh = await UserSession.GetSessionFromAzTable(sessionId, _tableClient);

            if (sesh == null) // No session with that ID
                return NotFound();

            var manifest = appDetails.ToTeamsAppManifest(url);
            var bytes = manifest.BuildZip(appDetails.EntityName);

            // Save file to blob storage
            await _blobClient.CreateIfNotExistsAsync();

            var fileName = $"{DateTime.Now.Ticks}/{appDetails.EntityName}.zip";
            Response<BlobContentInfo?> manifestRef = null;
            using (var ms = new MemoryStream(bytes))
            {
                manifestRef = await _blobClient.UploadBlobAsync(fileName, ms);
            }

            // Remember deets in cache
            sesh.AppDetails = appDetails;
            sesh.SavedManifestUrl = fileName;
            await sesh.UpdateTableRecord(_tableClient);

            // Respond with filename
            return Ok(fileName);
        }

        // GET api/TeamsApp/DownloadApp?fileUrl={fileUrl}&sessionId={sessionId}
        [HttpGet("[action]")]
        public async Task<IActionResult> DownloadApp(string fileUrl, string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(fileUrl))
            {
                return BadRequest();
            }

            var sesh = await UserSession.GetSessionFromAzTable(sessionId, _tableClient);

            if (sesh == null) // No session with that ID
                return NotFound();


            var blob = _blobClient.GetBlobClient(fileUrl);
            var exists = await blob.ExistsAsync();
            if (!exists)
            {
                return NotFound();
            }

            var url = blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.Now.AddMinutes(2));

            // Respond with filename
            return RedirectPermanent(url.ToString());
        }
    }
}
