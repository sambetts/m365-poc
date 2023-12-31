export const msalConfig = {
  auth: {
    clientId: String(process.env.REACT_APP_MSAL_CLIENT_ID),
    authority: String(process.env.REACT_APP_MSAL_AUTHORITY),
    redirectUri: String(process.env.REACT_APP_MSAL_REDIRECT),
  },
  cache: {
    cacheLocation: "sessionStorage", // This configures where your cache will be stored
    storeAuthStateInCookie: false, // Set this to "true" if you are having issues on IE11 or Edge
  }
};

export const scopes = ['User.Read', 'Chat.Read', 'Mail.ReadBasic'];
