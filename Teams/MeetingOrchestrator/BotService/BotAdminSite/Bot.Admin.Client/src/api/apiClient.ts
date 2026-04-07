import { IPublicClientApplication } from '@azure/msal-browser';
import { apiScopes } from '../authConfig';

/**
 * Acquires a bearer token and returns headers for API calls.
 */
async function getAuthHeaders(msalInstance: IPublicClientApplication): Promise<Record<string, string>> {
  const accounts = msalInstance.getAllAccounts();
  if (accounts.length === 0) {
    throw new Error('No authenticated account found.');
  }

  const response = await msalInstance.acquireTokenSilent({
    scopes: apiScopes,
    account: accounts[0],
  });

  return {
    Authorization: `Bearer ${response.accessToken}`,
    'Content-Type': 'application/json',
  };
}

/**
 * Performs an authenticated fetch request against the backend API.
 */
export async function apiFetch(
  msalInstance: IPublicClientApplication,
  url: string,
  options: RequestInit = {}
): Promise<Response> {
  const headers = await getAuthHeaders(msalInstance);
  return fetch(url, {
    ...options,
    headers: { ...headers, ...(options.headers as Record<string, string>) },
  });
}
