import { Configuration, LogLevel } from '@azure/msal-browser';

const clientId = import.meta.env.VITE_ENTRA_CLIENT_ID;
const tenantId = import.meta.env.VITE_ENTRA_TENANT_ID;

/**
 * MSAL configuration — set VITE_ENTRA_CLIENT_ID and VITE_ENTRA_TENANT_ID
 * in .env (or .env.local) with your Entra ID app registration values.
 */
export const msalConfig: Configuration = {
  auth: {
    clientId,
    authority: `https://login.microsoftonline.com/${tenantId}`,
    redirectUri: window.location.origin,
  },
  cache: {
    cacheLocation: 'sessionStorage',
    storeAuthStateInCookie: false,
  },
  system: {
    loggerOptions: {
      logLevel: LogLevel.Warning,
      loggerCallback: (_level, message) => console.log(message),
    },
  },
};

/**
 * Scopes requested when calling the Bot.Admin API.
 * Must match the "Expose an API" scope in your Entra app registration.
 */
export const apiScopes = [`api://${clientId}/access_as_user`];

/**
 * Login request used by MSAL.
 */
export const loginRequest = {
  scopes: apiScopes,
};
