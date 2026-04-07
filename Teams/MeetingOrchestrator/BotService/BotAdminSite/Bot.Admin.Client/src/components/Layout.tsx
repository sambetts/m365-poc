import { ReactNode } from 'react';
import {
  AuthenticatedTemplate,
  UnauthenticatedTemplate,
  useMsal,
} from '@azure/msal-react';
import { loginRequest } from '../authConfig';

export default function Layout({ children }: { children: ReactNode }) {
  const { instance, accounts } = useMsal();
  const account = accounts[0];

  return (
    <div style={{ fontFamily: 'Segoe UI, sans-serif', margin: 0 }}>
      <header
        style={{
          background: '#6264a7',
          color: '#fff',
          padding: '12px 24px',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
        }}
      >
        <h1 style={{ margin: 0, fontSize: '1.25rem' }}>
          Meeting Orchestrator Admin
        </h1>
        <AuthenticatedTemplate>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <span>{account?.name ?? account?.username}</span>
            <button onClick={() => instance.logoutRedirect()} style={btnStyle}>
              Sign out
            </button>
          </div>
        </AuthenticatedTemplate>
      </header>

      <main style={{ padding: 24 }}>
        <UnauthenticatedTemplate>
          <div style={{ textAlign: 'center', marginTop: 80 }}>
            <h2>Welcome to Meeting Orchestrator Admin</h2>
            <p>Sign in with your Microsoft account to manage scripts and bots.</p>
            <button
              onClick={() => instance.loginRedirect(loginRequest)}
              style={{ ...btnStyle, fontSize: '1rem', padding: '10px 24px' }}
            >
              Sign in
            </button>
          </div>
        </UnauthenticatedTemplate>

        <AuthenticatedTemplate>{children}</AuthenticatedTemplate>
      </main>
    </div>
  );
}

const btnStyle: React.CSSProperties = {
  background: '#fff',
  color: '#6264a7',
  border: 'none',
  borderRadius: 4,
  padding: '6px 16px',
  cursor: 'pointer',
  fontWeight: 600,
};
