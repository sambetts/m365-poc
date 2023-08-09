using GraphBasicConsole.Entities;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace GraphBasicConsole
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

        public string RedirectURL { get; set; }

        public string TenantId { get; set; }

        #endregion

        #region Constructors

        public AzureADContext(string appID, string redirectUrl, string tenantId)
        {
            if (string.IsNullOrWhiteSpace(appID))
            {
                throw new ArgumentNullException("appID", "We need an application ID. Use the configured value in Azure AD.");
            }
            if (string.IsNullOrWhiteSpace(redirectUrl))
            {
                throw new ArgumentNullException("redirectUrl", "We need a redirect URL. Use the configured value in Azure AD.");
            }
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new ArgumentNullException("tenantId", "We need an Azure AD tenant ID. Use the configured value in Azure AD.");
            }
            this.ApplicationID = appID;
            this.RedirectURL = redirectUrl;
            this.TenantId = tenantId;
        }

        #endregion

        /// <summary>
        /// Open our form to get a logon auth code once they login.
        /// </summary>
        public string GetLogonCodeFromBrowserForm()
        {
            BasicLoginForm logonForm = new BasicLoginForm(this);
            logonForm.ShowDialog();

            if (string.IsNullOrEmpty(logonForm.LogonToken))
            {
                throw new ApplicationException("Didn't get an authorisation code from browser login!");
            }

            return logonForm.LogonToken;
        }

        /// <summary>
        /// Convert a logon code to an access code (OAuth).
        /// </summary>
        public async Task<AccessToken> GetOAuthTokenForGraph(string code)
        {
            // Build POST request
            string body = "grant_type=authorization_code&" +
                            "client_id=" + this.ApplicationID + "&" +
                            "code=" + code + "&" +
                            "redirect_uri=" + System.Web.HttpUtility.UrlEncode(this.RedirectURL) + "&" +
                            "resource=" + System.Web.HttpUtility.UrlEncode("https://graph.microsoft.com");

            // Get response
            string postResult = await WebUtil.Post($"https://login.microsoftonline.com/{this.TenantId}/oauth2/token", body);

            // Convert to object
            AccessToken token = AccessToken.FromJson(postResult);

            return token;
        }
    }
}
