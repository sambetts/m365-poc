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

// Default scopes - these can be customized by the user at login
export const defaultScopes = ['User.Read', 'Chat.Read', 'Mail.Read'];

// Available scopes with descriptions for user selection
export const availableScopes = [
  { value: 'User.Read', label: 'Read user profile', description: 'Read your basic profile information' },
  { value: 'User.ReadBasic.All', label: 'Read all users basic info', description: 'Read basic information about all users' },
  { value: 'Chat.Read', label: 'Read chats', description: 'Read your chat messages' },
  { value: 'Chat.ReadWrite', label: 'Read and write chats', description: 'Read and send chat messages' },
  { value: 'Mail.Read', label: 'Read mail', description: 'Read your email' },
  { value: 'Mail.ReadBasic', label: 'Read basic mail info', description: 'Read basic email information' },
  { value: 'Mail.ReadWrite', label: 'Read and write mail', description: 'Read and send email' },
  { value: 'Files.Read', label: 'Read files', description: 'Read your OneDrive files' },
  { value: 'Files.ReadWrite', label: 'Read and write files', description: 'Read and modify your OneDrive files' },
  { value: 'Calendars.Read', label: 'Read calendar', description: 'Read your calendar events' },
  { value: 'Calendars.ReadWrite', label: 'Read and write calendar', description: 'Read and modify your calendar' }
];
