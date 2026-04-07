import { IPublicClientApplication } from '@azure/msal-browser';
import { apiFetch } from './apiClient';
import type { ScriptDto } from '../types';

const BASE = '/api/scripts';

export async function getScripts(msal: IPublicClientApplication): Promise<ScriptDto[]> {
  const res = await apiFetch(msal, BASE);
  if (!res.ok) throw new Error(`Failed to fetch scripts: ${res.statusText}`);
  return res.json();
}

export async function getScript(msal: IPublicClientApplication, id: string): Promise<ScriptDto> {
  const res = await apiFetch(msal, `${BASE}/${id}`);
  if (!res.ok) throw new Error(`Failed to fetch script: ${res.statusText}`);
  return res.json();
}

export async function createScript(msal: IPublicClientApplication, script: Partial<ScriptDto>): Promise<ScriptDto> {
  const res = await apiFetch(msal, BASE, {
    method: 'POST',
    body: JSON.stringify(script),
  });
  if (!res.ok) throw new Error(`Failed to create script: ${res.statusText}`);
  return res.json();
}

export async function updateScript(msal: IPublicClientApplication, id: string, script: Partial<ScriptDto>): Promise<ScriptDto> {
  const res = await apiFetch(msal, `${BASE}/${id}`, {
    method: 'PUT',
    body: JSON.stringify(script),
  });
  if (!res.ok) throw new Error(`Failed to update script: ${res.statusText}`);
  return res.json();
}

export async function deleteScript(msal: IPublicClientApplication, id: string): Promise<void> {
  const res = await apiFetch(msal, `${BASE}/${id}`, { method: 'DELETE' });
  if (!res.ok) throw new Error(`Failed to delete script: ${res.statusText}`);
}
