using CommonUtils;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OfficeNotifications.Engine;
using System.Threading.Tasks;

namespace OfficeNotifications.Functions
{
    public class Program 
    {
        static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration(c =>
                {
                    c.AddEnvironmentVariables();
                    c.AddCommandLine(args);
                    c.AddUserSecrets(System.Reflection.Assembly.GetEntryAssembly(), true);
                    c.SetBasePath(System.IO.Directory.GetCurrentDirectory())
                        .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
                    c.Build();
                })
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices((context, services) =>
                {

                    var config = new Config(context.Configuration);
                    services.AddSingleton(config);

                    services.AddApplicationInsightsTelemetryWorkerService(ops => ops.ConnectionString = $"InstrumentationKey={config.AppInsightsInstrumentationKey}");


                    // Service-bus & Az blob
                    services.AddAzureClients(builder =>
                    {
                        builder.AddServiceBusClient(config.ConnectionStrings.ServiceBus);
                    });
                })
                .Build();

            await host.RunAsync();
        }
    }
}
