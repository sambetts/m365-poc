using GraphHeadlessCSharp.JSonEntities;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;



namespace GraphHeadlessCSharp
{
    public class SharePointCalls
    {

        public static async Task<ListsResponse> GetLists(AuthenticationResult graphAuth)
        {
            // Call Graph API with OAuth token
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", graphAuth.AccessToken);

            string graphApiCallURL = "https://graph.microsoft.com/v1.0/sites/root/lists";

            // Make HTTP call & make sure no HTTP error
            HttpResponseMessage response = await httpClient.GetAsync(graphApiCallURL);
            response.EnsureSuccessStatusCode();


            // Read response into a strongly-typed JSon object
            string responseString = response.Content.ReadAsStringAsync().Result;

            ListsResponse lists = ListsResponse.FromJson(responseString);
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Success! Read back:");
            foreach (var list in lists.Value)
            {
                Console.WriteLine(list.Name);
            }
            Console.ForegroundColor = ConsoleColor.Yellow;

            return lists;
        }

    }
}
