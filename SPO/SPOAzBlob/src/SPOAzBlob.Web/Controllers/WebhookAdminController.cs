using CommonUtils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using SPOAzBlob.Engine;
using SPOAzBlob.Web.Models;

namespace SPO.ColdStorage.Web.Controllers
{
    /// <summary>
    /// Handles React app requests for app configuration
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ApiController]
    [Route("[controller]")]
    public class WebhookAdminController : ControllerBase
    {
        private readonly DebugTracer _tracer;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly Config _config;

        public WebhookAdminController(Config config, DebugTracer tracer, GraphServiceClient graphServiceClient)
        {
            _tracer = tracer;
            this._graphServiceClient = graphServiceClient;
            this._config = config;
        }

        // Return all Graph subs for endpoint + target
        // GET: WebhookAdmin/SubscriptionsConfig
        [HttpGet("[action]")]
        public async Task<ActionResult<WebhooksState>> SubscriptionsConfig()
        {
            var client = new WebhooksManager(_config, _tracer, _config.WebhookUrlOverride);

            var allSubs = await client.GetInScopeSubscriptions();
            return new WebhooksState { Subscriptions = allSubs, TargetEndpoint = _config.WebhookUrlOverride };
        }

        // Renew or create an active subscription
        // POST: WebhookAdmin/CreateOrUpdateSubscription
        [HttpPost("[action]")]
        public async Task<ActionResult<Subscription>> CreateOrUpdateSubscription()
        {
            var client = new WebhooksManager(_config, _tracer, _config.WebhookUrlOverride);

            var sub = await client.CreateOrUpdateSubscription();
            return sub;
        }
    }
}
