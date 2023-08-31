using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OfficeNotifications.Engine;
using OfficeNotifications.Engine.Models;

namespace OfficeNotifications.Functions
{
    public class GraphNotifications
    {
        private readonly Config _config;
        private readonly ILogger<GraphNotification> _tracer;
        private readonly ServiceBusClient _serviceBusClient;

        public GraphNotifications(Config config, ILogger<GraphNotification> tracer, ServiceBusClient serviceBusClient)
        {
            this._config = config;
            this._tracer = tracer;
            this._serviceBusClient = serviceBusClient;
        }

        /// <summary>
        /// Endpoint for Graph to call us with change notifications. 
        /// Must reply within 3 seconds or we'll get delayed responses - https://docs.microsoft.com/en-us/graph/webhooks#change-notifications
        /// </summary>
        [Function("GraphNotifications")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req)
        {
            var defaultOkResponse = req.CreateResponse(HttpStatusCode.OK);

            // Is this a Graph validation call?
            const string VALIDATION_PARAM_NAME = "validationToken";
            var queryDictionary = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            if (queryDictionary.AllKeys.Where(k => k == VALIDATION_PARAM_NAME).Any())
            {
                // Return validation response
                // https://docs.microsoft.com/en-us/graph/webhooks#notification-endpoint-validation
                var validationTokenValue = WebUtility.UrlDecode(queryDictionary[VALIDATION_PARAM_NAME]);
                _tracer.LogInformation($"Got Graph API validation call. Returning decoded validation token value '{validationTokenValue}'.");
                defaultOkResponse.WriteString(validationTokenValue);
                defaultOkResponse.Headers.Add("Content-Type", "text/plain");

                return defaultOkResponse;
            }

            if (_serviceBusClient == null)
            {
                _tracer.LogError($"Can't find service type {typeof(ServiceBusClient).Name} - message not sent to service-bus");
                return req.CreateResponse(HttpStatusCode.InternalServerError);
            }
            var sbSender = _serviceBusClient.CreateSender(_config.ServiceBusQueueName);

            // Figure out update
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            var update = JsonSerializer.Deserialize<GraphNotification>(requestBody);
            if (update != null && update.IsValid)
            {
                var sbMsg = new ServiceBusMessage(requestBody);

                await sbSender.SendMessageAsync(sbMsg);
                _tracer.LogInformation($"Got updates from Graph. Sent to Service-Bus queue for async processing.");

            }
            else
            {
                _tracer.LogWarning($"Got invalid Graph update notification with body '{requestBody}'");
            }

            return defaultOkResponse;
        }
    }
}
