using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bot.Services.ServiceSetup
{
    /// <summary>
    /// Class ServicesExtension.
    /// </summary>
    public static class ServicesExtension
    {
        /// <summary>
        /// Adds the core services.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
        public static void AddCoreServices(this IServiceCollection services, IConfiguration configuration)
        {
            new ServiceHost().Configure(services, configuration);
        }
    }
}
