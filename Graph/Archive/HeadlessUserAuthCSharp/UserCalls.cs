using GraphHeadlessCSharp.JSonEntities;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace GraphHeadlessCSharp
{
    public class UserCalls
    {

        public static async Task OutputAllUsers(AuthenticationResult graphAuth)
        {
            // Call Graph API with OAuth token
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", graphAuth.AccessToken);

            string graphApiCallURL = "https://graph.microsoft.com/v1.0/me";

            // Make HTTP call & make sure no HTTP error
            HttpResponseMessage response = await httpClient.GetAsync(graphApiCallURL);
            response.EnsureSuccessStatusCode();

            // Read response into JSon object
            string responseString = response.Content.ReadAsStringAsync().Result;
            Me me = Me.FromJson(responseString);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Success! Read back: I am: {me.DisplayName}, email: {me.Mail}");
            Console.ForegroundColor = ConsoleColor.White;

        }
    }
}
