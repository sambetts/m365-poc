using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Configuration;
using System.Threading.Tasks;

namespace GraphHeadlessCSharp
{

    /*
     * This example shows how user authentication is possible for headless logins.
     */
    class Program
    {
        private static string aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
        private static string tenant = ConfigurationManager.AppSettings["ida:Tenant"];
        private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static string authority = String.Format(System.Globalization.CultureInfo.InvariantCulture, aadInstance, tenant);


        static void Main(string[] args)
        {
            // Login as a user, non-interactively & get the auth code
            AuthenticationResult result = GetNonInteractiveUserAuthentication().Result;

            // Do things with Graph
            DoThingsWithGraph(result).Wait();

            // Finish up
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\nAll done.");
            Console.ReadKey();

        }

        static async Task<AuthenticationResult> GetNonInteractiveUserAuthentication()
        {
            const string GRAPH_RESOURCE_ID = "https://graph.microsoft.com";

            // Create context with our own file-cache for OUath tokens
            AuthenticationContext authContext = new AuthenticationContext(authority, new FileCache());
            AuthenticationResult result = null;

            Console.WriteLine($"Getting OAuth token for Azure AD application '{clientId}' access to {GRAPH_RESOURCE_ID}...");

            // first, try to get a token silently
            try
            {
                result = await authContext.AcquireTokenSilentAsync(GRAPH_RESOURCE_ID, clientId);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Token taken from cache/silently.");

                return result;
            }
            catch (AdalException adalException)
            {
                // There is no token in the cache; prompt the user to sign-in.
                if (adalException.ErrorCode == AdalError.FailedToAcquireTokenSilently
                    || adalException.ErrorCode == AdalError.InteractionRequired)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("No token in the cache. Aquiring full token from Azure AD...");

                    UserCredential credentials = new UserPasswordCredential(@"admin@M365x246423.onmicrosoft.com", @"sambetts@1024");
                    result = await authContext.AcquireTokenAsync(GRAPH_RESOURCE_ID, clientId, credentials);
                    Console.WriteLine("Success!");
                }
            }
            return result;
        }


        private static async Task DoThingsWithGraph(AuthenticationResult graphAuth)
        {
            // Show users
            Console.WriteLine($"Getting users from Graph API...");
            await UserCalls.OutputAllUsers(graphAuth);

            // Get SharePoint lists from root site
            Console.WriteLine($"\nGetting SharePoint lists from Graph API...");
            await SharePointCalls.GetLists(graphAuth);
        }

    }
}
