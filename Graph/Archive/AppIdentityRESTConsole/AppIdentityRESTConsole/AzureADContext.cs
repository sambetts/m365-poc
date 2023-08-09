using AppIdentityRESTConsole.Entities;
using System;
using System.Threading.Tasks;

namespace AppIdentityRESTConsole
{
    /// <summary>
    /// Basic version of MSA library. Helps with authentication
    /// </summary>
    public class AzureADContext
    {

        #region Properties

        /// <summary>
        /// Azure AD Application ID
        /// </summary>
        public string ApplicationID { get; set; }

        public string ClientSecret { get; set; }

        public string TenantId { get; set; }

        #endregion

        #region Constructors

        public AzureADContext(string appID, string clientSecret, string tenantId)
        {
            if (string.IsNullOrWhiteSpace(appID))
            {
                throw new ArgumentNullException("appID", "We need an application ID. Use the configured value in Azure AD.");
            }
            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                throw new ArgumentNullException("clientSecret", "We need a client secret. Use the configured value in Azure AD.");
            }
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new ArgumentNullException("tenantId", "We need an Azure AD tenant ID. Use the configured value in Azure AD.");
            }
            this.ApplicationID = appID;
            this.ClientSecret = clientSecret;
            this.TenantId = tenantId;
        }

        #endregion
        

        /// <summary>
        /// Convert a logon code to an access code (OAuth).
        /// </summary>
        public async Task<AccessToken> GetOAuthTokenForGraph()
        {
            // Build POST request
            string body =   "client_id=" + this.ApplicationID + "&" +
                            "client_secret=" + System.Web.HttpUtility.UrlEncode(this.ClientSecret) + "&" +
                            "grant_type=client_credentials&" +
                            "resource=" + System.Web.HttpUtility.UrlEncode("https://graph.microsoft.com");

            // Get response
            string postResult = await WebUtil.Post($"https://login.microsoftonline.com/{this.TenantId}/oauth2/token", body);

            // Convert to object
            AccessToken token = AccessToken.FromJson(postResult);

            return token;
        }
    }
}
