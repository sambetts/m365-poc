using Azure.Messaging.ServiceBus;
using CommonUtils;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SPOAzBlob.Engine;

namespace SPOAzBlob.Functions
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration(c =>
                {
                    c.AddEnvironmentVariables();
                    c.AddCommandLine(args);
                    c.SetBasePath(System.IO.Directory.GetCurrentDirectory())
                        .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
                    c.Build();
                })
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices((context, services) =>
                {
                    var config = new Config(context.Configuration);
                    services.AddSingleton(config);
                    var telemetry = new DebugTracer(config.AppInsightsInstrumentationKey, "Functions");
                    services.AddSingleton(telemetry);

                    // Service-bus
                    var sbClient = new ServiceBusClient(config.ConnectionStrings.ServiceBus);
                    var sbSender = sbClient.CreateSender(config.ServiceBusQueueName);
                    services.AddSingleton(sbSender);

                    // Az blob
                    services.AddAzureClients(builder =>
                    {
                        builder.AddBlobServiceClient(config.ConnectionStrings.Storage);
                    });

                })
                .Build();

            host.Run();
        }
    }
}
