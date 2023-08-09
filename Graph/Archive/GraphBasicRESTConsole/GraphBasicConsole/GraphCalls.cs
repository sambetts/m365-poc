using GraphBasicConsole.Entities;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace GraphBasicConsole
{
    /// <summary>
    /// Example calls we can make to Graph API.
    /// </summary>
    public class GraphCalls
    {
        /// <summary>
        /// Read the users' OneDrive info
        /// </summary>
        public static async Task<OneDriveInfo> GetOneDriveInfo(AccessToken token)
        {
            
            // Read data
            Console.WriteLine("[Graph] Calling 'GET' on 'https://graph.microsoft.com/v1.0/me/drive/'...");

            string driveResponse = await WebUtil.Get("https://graph.microsoft.com/v1.0/me/drive/", token.access_token);

            // Convert to object
            OneDriveInfo driveInfo = JsonConvert.DeserializeObject<OneDriveInfo>(driveResponse);

            // Output space
            Console.WriteLine("\nTEST SUCCESS! Read back drive info. \"{0:N0}\" bytes total, \"{1:N0}\" bytes remaining.",
                driveInfo.Quota.Total, driveInfo.Quota.Remaining);
            
            return driveInfo;
            
        }
        

    }
}
