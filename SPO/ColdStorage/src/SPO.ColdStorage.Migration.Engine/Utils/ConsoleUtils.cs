using Microsoft.Extensions.Configuration;
using SPO.ColdStorage.Entities.Configuration;

namespace SPO.ColdStorage.Migration.Engine.Utils
{
    public class ConsoleUtils
    {
        public static Config GetConfigurationWithDefaultBuilder<T>() where T : class
        {
            var builder = GetConfigurationBuilder<T>();

            var configCollection = builder.Build();
            return new Config(configCollection);
        }

        public static IConfigurationBuilder GetConfigurationBuilder<T>() where T : class
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddUserSecrets<T>()
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", true);
        }

        public static void PrintCommonStartupDetails()
        {
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            Console.WriteLine($"Start-up: '{assembly?.FullName}'.");
        }
    }
}
