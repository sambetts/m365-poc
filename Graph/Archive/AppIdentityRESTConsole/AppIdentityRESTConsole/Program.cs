using AppIdentityRESTConsole.Entities;
using AppIdentityRESTConsole.Properties;
using System;
using System.Threading.Tasks;

namespace AppIdentityRESTConsole
{
    /// <summary>
    /// This app demonstrates use of Graph via Azure AD with just basic HTTP calls.
    /// No frameworks are used.
    /// </summary>
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Info & disclaimer
            Console.WriteLine("Sam Betts Graph API PoC (sambetts@microsoft.com). IMPORTANT: Run this at your own risk!");
            Console.WriteLine("This app will login as an application and read all user-names in this directory. ");

            // Do the things.
#if !DEBUG
            try
            {

#endif
            var mainTask = Task.Run(() => Go());
            mainTask.Wait();
#if !DEBUG
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Got an unexpected error:\n{ex.Message}");
            }
#endif

            // Fin
            Console.WriteLine("\nAll done. Press ENTER to exit.");
            Console.Read();
        }

        /// <summary>
        /// Main execution logic. 
        /// </summary>
        private static async Task Go()
        {
            // Use our basic authentication helper
            AzureADContext authHelper = new AzureADContext(Settings.Default.ApplicationId,
                Settings.Default.ClientSecret, Settings.Default.DirectoryId);

            // Get AD Azure API token 
            AccessToken token = await authHelper.GetOAuthTokenForGraph();
            Console.WriteLine("[Azure AD] Got an OAuth token to access Graph with. Now we can make API calls...");

            // Run the people-search example
            ListsResponse lists = await GraphCalls.GetLists(token);
            OutputListsResponse(lists);

        }

        private static void OutputUsersResponse(Users users)
        {
            Console.WriteLine("\nSuccess. Users found:");
            string usersDebug = string.Empty;
            foreach (var user in users.Value)
            {
                usersDebug += user.DisplayName + ", ";
            }
            usersDebug = usersDebug.TrimEnd(", ".ToCharArray());
        }

        private static void OutputListsResponse(ListsResponse lists)
        {
            // Output results to debug window
            Console.WriteLine("\nSuccess. Lists found:");
            string listsDebug = string.Empty;
            foreach (var user in lists.Value)
            {
                listsDebug += user.DisplayName + ", ";
            }
            listsDebug = listsDebug.TrimEnd(", ".ToCharArray());
        }
    }
}
