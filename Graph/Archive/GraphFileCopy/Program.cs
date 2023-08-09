using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using NDesk.Options;
using System;
using System.Linq;

namespace GraphFileCopy
{
    /// <summary>
    /// This program copies from one doc-library to another via Graph API. Includes metadata too.
    /// Note: this is purely for testing purposes only.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            string dest = string.Empty, source = string.Empty;

            // Build argument requirements. NDesk.Options is a handy argument parsing library
            var options = new OptionSet() {
                { "d|dest=", "Destination library name.",
                   (string v) => dest = v},
                { "s|source=", "Source library name.",
                   (string v) => source = v}
            };

            // Parse args into our own variables
            options.Parse(args);

            // Have we got valid params?
            bool showHelp = !AreValidParams(dest, source);

            if (showHelp)
            {
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
            }
            else
            {
                // Authenticate
                IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
                    .Create("a0309e2c-2397-400f-9b82-f7a8fa6a7c57")
                    .WithRedirectUri("http://localhost:5500/")
                    .WithClientSecret("O[p]QE4+D1O71ppPQMOm-WTP.[pE6fmz")
                    .WithTenantId("988e233f-3df7-49ca-a29b-b2fc20074711")
                    .Build();

                // Build new GraphClient
                ClientCredentialProvider authenticationProvider = new ClientCredentialProvider(confidentialClientApplication);
                GraphServiceClient graphServiceClient = new GraphServiceClient(authenticationProvider);


                // Get lists in root site
                var lists = graphServiceClient.Sites.Root.Lists.Request().WithForceRefresh(true).GetAsync().Result;

                // Get list instances
                List sourceList = lists.Where(l => l.Name.ToLower() == source.ToLower()).SingleOrDefault()
                    , destList = lists.Where(l => l.Name.ToLower() == dest.ToLower()).SingleOrDefault();

                // Validation
                if (sourceList == null)
                {
                    throw new ArgumentOutOfRangeException($"Cannot find list with name '{source}' in root site.");
                }
                if (destList == null)
                {
                    throw new ArgumentOutOfRangeException($"Cannot find list with name '{source}' in root site.");
                }


                // Do the things
                GraphCalls calls = new GraphCalls(graphServiceClient);
                calls.CopyFilesAndMetadata(sourceList, destList).Wait();
            }

            Console.WriteLine("All done. Press something to exit");
            Console.ReadKey();
        }

        private static bool AreValidParams(string dest, string source)
        {
            return !string.IsNullOrWhiteSpace(dest) && !string.IsNullOrWhiteSpace(source);
        }

    }
}
