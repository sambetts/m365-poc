using GraphBasicConsole.Entities;
using GraphBasicConsole.Properties;
using System;
using System.Threading.Tasks;

namespace GraphBasicConsole
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
            Console.WriteLine("This app will login a user, then read their OneDrive for Business drive information. ");

            // Do the things.
            try
            {
                Go();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Got an unexpected error:\n{ex.Message}");
            }
            

            // Fin
            Console.WriteLine("\nAll done. Press ENTER to exit.");
            Console.Read();
        }

        /// <summary>
        /// Main execution logic. Single-threaded due to WinForms elements.
        /// </summary>
        private static void Go()
        {
            // Use our basic authentication helper
            AzureADContext authHelper = new AzureADContext(Settings.Default.ApplicationId, Settings.Default.RedirectUrl, Settings.Default.DirectoryId);

            // Get user logon code from our form. No async due to ActiveX controls being used for browser.

            Console.WriteLine("[Azure AD] Opening browser to get authorisation code...");
            string loginCode = authHelper.GetLogonCodeFromBrowserForm();
            Console.WriteLine("[Azure AD] Got an authorisation code. Asking for OAuth code with authorisation code...");

            // Get AD Azure API token 
            var getOAuthTokenTask = Task.Run(() => authHelper.GetOAuthTokenForGraph(loginCode));
            AccessToken token = getOAuthTokenTask.Result;
            Console.WriteLine("[Azure AD] Got an OAuth token to access Graph with. Now we can make API calls...");

            // Run the OneDrive example
            Task.Run(() =>GraphCalls.GetOneDriveInfo(token)).Wait();

        }
    }
}
