import { IPublicClientApplication } from '@azure/msal-browser';
import { apiFetch } from './apiClient';
import type { JoinCallRequest, JoinCallResponse, StartScriptRequest } from '../types';

export async function joinCall(msal: IPublicClientApplication, request: JoinCallRequest): Promise<JoinCallResponse> {
  const res = await apiFetch(msal, '/api/bot/join', {
    method: 'POST',
    body: JSON.stringify(request),
  });
  if (!res.ok) throw new Error(`Failed to join call: ${res.statusText}`);
  return res.json();
}

export async function startScript(msal: IPublicClientApplication, request: StartScriptRequest): Promise<void> {
  const res = await apiFetch(msal, '/api/bot/start-script', {
    method: 'POST',
    body: JSON.stringify(request),
  });
  if (!res.ok) throw new Error(`Failed to start script: ${res.statusText}`);
}
