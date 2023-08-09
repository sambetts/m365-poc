using System;
using System.Windows.Forms;

namespace GraphBasicConsole
{
    /// <summary>
    /// A real simple form to do our logins. 
    /// </summary>
    public partial class BasicLoginForm : Form
    {
        /// <summary>
        /// Create new form for a given authentication context
        /// </summary>
        /// <param name="context"></param>
        public BasicLoginForm(AzureADContext context)
        {
            this.Context = context;
            InitializeComponent();
        }

        #region Properties

        public AzureADContext Context { get; set; }

        public string LogonToken { get; internal set; }

        #endregion

        private void LoginFormcs_Load(object sender, EventArgs e)
        {
            // Documentation https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-auth-code-flow

            // Build navigation URL
            string url = $"https://login.microsoftonline.com/{this.Context.TenantId}/oauth2/authorize?" +
                $"response_type=code&" +
                $"client_id={this.Context.ApplicationID}&" +
                $"redirect_uri={this.Context.RedirectURL}";

            webBrowser1.Navigate(new Uri(url));
        }


        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            // Once the redirect flow reaches our redirect-url, we're done
            if (webBrowser1.Url.AbsoluteUri.StartsWith(Context.RedirectURL))
            {
                // Grab return params
                string[] urlParams = webBrowser1.Url.Query.Split("&".ToCharArray());

                // Find the auth code.
                this.LogonToken = urlParams[0].TrimStart("? code = ".ToCharArray());
                
                // We're done once we have the code
                this.Close();
            }
        }
    }
}
