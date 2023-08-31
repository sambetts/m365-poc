using CommonUtils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using SPOAzBlob.Engine;
using SPOAzBlob.Engine.Models;

namespace SPO.ColdStorage.Web.Controllers
{
    /// <summary>
    /// Handles React app requests for editing files
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ApiController]
    [Route("[controller]")]
    public class EditActionsController : ControllerBase
    {
        private readonly DebugTracer _tracer;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly Config _config;

        public EditActionsController(Config config, DebugTracer tracer, GraphServiceClient graphServiceClient)
        {
            _tracer = tracer;
            this._graphServiceClient = graphServiceClient;
            this._config = config;
        }


        // Start editing a document in SPO
        // POST: EditActions/StartEdit?url=1232
        [HttpPost("[action]")]
        public async Task<ActionResult<DriveItem>> StartEdit(string url)
        {
            User? user = null;
            try
            {
                user = await GetUserName();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);  
            }

            var fm = new FileOperationsManager(_config, _tracer);
            return await fm.StartFileEditInSpo(url, GraphUserManager.GetUserName(user));
        }

        // Deletes lock for document
        // POST: EditActions/ReleaseLock?driveItemId=123
        [HttpPost("[action]")]
        public async Task<ActionResult> ReleaseLock(string driveItemId)
        {
            var driveInfo = await _graphServiceClient.Sites[_config.SharePointSiteId].Drive.Request().GetAsync();
            var fm = new FileOperationsManager(_config, _tracer);

            try
            {
                await fm.FinishEditing(driveItemId, driveInfo.Id);
            }
            catch (ArgumentOutOfRangeException)
            {
                return NotFound($"No lock with drive Id '{driveItemId}'");
            }

            return Ok();
        }

        async Task<User> GetUserName()
        {
            var emailClaimValue = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn")?.Value;
            var um = new GraphUserManager(_config, _tracer);
            if (string.IsNullOrEmpty(emailClaimValue))
            {
                throw new InvalidOperationException("No email claim in user");
            }
            var user = await um.GetUserByEmail(emailClaimValue);
            return user;
        }

        // GET: EditActions/GetActiveLocks
        [HttpPost("[action]")]
        public async Task<ActionResult<List<FileLock>>> GetActiveLocks()
        {
            var drive = await _graphServiceClient.Sites[_config.SharePointSiteId].Drive.Request().GetAsync();
            var azManager = new AzureStorageManager(_config, _tracer);

            var initialLocks = await azManager.GetLocks(drive.Id);
            return initialLocks;
        }
    }
}
