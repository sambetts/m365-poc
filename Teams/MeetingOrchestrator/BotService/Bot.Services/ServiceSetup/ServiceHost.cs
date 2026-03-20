using Bot.Services.Bot;
using Bot.Services.Contract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Communications.Common.Telemetry;
using System;

namespace Bot.Services.ServiceSetup
{
    /// <summary>
    /// Class ServiceHost.
    /// Implements the <see cref="IServiceHost" />
    /// </summary>
    /// <seealso cref="IServiceHost" />
    public class ServiceHost : IServiceHost
    {
        /// <summary>
        /// Gets the services.
        /// </summary>
        /// <value>The services.</value>
        public IServiceCollection Services { get; private set; }
        /// <summary>
        /// Gets the service provider.
        /// </summary>
        /// <value>The service provider.</value>
        public IServiceProvider ServiceProvider { get; private set; }


        /// <summary>
        /// Configures the specified services.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>ServiceHost.</returns>
        public ServiceHost Configure(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IGraphLogger, GraphLogger>(_ => new GraphLogger("MeetingOrchestratorBot", redirectToTrace: true));
            services.Configure<AzureSettings>(configuration.GetSection(nameof(AzureSettings)));

            services.AddSingleton<IAzureSettings>(_ => _.GetRequiredService<IOptions<AzureSettings>>().Value);

            var config = (AzureSettings)services.BuildServiceProvider().GetRequiredService<IAzureSettings>();

            // App Insights logging. We're only interested in info msgs
            services.AddLogging(loggingBuilder =>
                loggingBuilder.SetMinimumLevel(LogLevel.Information));

            // Only add Application Insights if a connection string is configured
            if (!string.IsNullOrEmpty(config.ApplicationInsightsKey))
            {
                services.AddApplicationInsightsTelemetry(options =>
                {
                    options.ConnectionString = config.ApplicationInsightsKey;
                });
            }

            services.AddSingleton<IMediaSessionFactory, DefaultMediaSessionFactory>();
            services.AddSingleton<ICallHandlerFactory, DefaultCallHandlerFactory>();
            services.AddSingleton<IBotService, BotService>();

            return this;
        }
    }
}
