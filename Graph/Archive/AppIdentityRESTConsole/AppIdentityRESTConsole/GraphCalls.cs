using AppIdentityRESTConsole.Entities;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace AppIdentityRESTConsole
{
    /// <summary>
    /// Example calls we can make to Graph API.
    /// </summary>
    public class GraphCalls
    {
        /// <summary>
        /// Read the users' OneDrive info
        /// </summary>
        public static async Task<Users> GetAllUsers(AccessToken token)
        {
            string url = "https://graph.microsoft.com/v1.0/users";

            // Read data
            Console.WriteLine($"[Graph] Calling 'GET' on '{url}'...");

            string peopleResponse = await WebUtil.Get(url, token.access_token);

            // Convert to object
            Users peopleResults = Users.FromJson(peopleResponse);

            // Output space
            
            return peopleResults;
            
        }

        public static async Task GetSites(AccessToken graphAuth)
        {
            string graphApiCallURL = "https://graph.microsoft.com/v1.0/sites?search=*";
            // Read response into a strongly-typed JSon object
            string responseString = await WebUtil.Get(graphApiCallURL, graphAuth.access_token);


            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Success - read back sites!");

        }


        public static async Task<ListsResponse> GetLists(AccessToken graphAuth)
        {
            string graphApiCallURL = "https://graph.microsoft.com/v1.0/sites/root/lists";
            // Read response into a strongly-typed JSon object
            string responseString = await WebUtil.Get(graphApiCallURL, graphAuth.access_token);

            ListsResponse lists = ListsResponse.FromJson(responseString);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Success! Read back:");
            foreach (var list in lists.Value)
            {
                Console.WriteLine(list.DisplayName);
            }
            Console.ForegroundColor = ConsoleColor.Yellow;

            return lists;
        }

        internal static async Task SendEmail(AccessToken graphAuth, string subject, string body, string from, string to)
        {
            // https://docs.microsoft.com/en-us/graph/permissions-reference#mail-permissions

            string graphApiCallURL = "https://graph.microsoft.com/v1.0/users/" + from  + "/sendmail";

            EmailMessage newEmail = new EmailMessage();
            newEmail.Message.Subject = subject;
            newEmail.Message.From.EmailAddress.Address = from;
            newEmail.Message.ToRecipients.Add(new Recipient() { EmailAddress = new EmailAddress() { Address = to } });

            /*
            {
                ""message"": {
                    ""subject"": ""Email Sent with grant_type=client_credentials"",
                    ""body"": {
                        ""contentType"": ""Text"",
                    ""content"": ""Test Body""
                    },
                    ""toRecipients"": [
                    {
                        ""emailAddress"": {
                        ""address"": ""admin@M365x246423.onmicrosoft.com""
                        }
                    }
                    ],
                    ""from"" :
                    {
                        ""emailAddress"": {
                            ""address"": ""meganb@M365x246423.onmicrosoft.com""
                          }
                    }
                }
            }
            */
            
            await WebUtil.Post(graphApiCallURL, newEmail.ToJson(), graphAuth.access_token);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Email send success!");

        }
    }
}
