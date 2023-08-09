import React, { useState } from 'react';
import './App.css';
import { AuthenticatedTemplate, UnauthenticatedTemplate, useIsAuthenticated, useMsal } from '@azure/msal-react';
import { scopes } from './authConfig';
import { SignInButton } from './components/SignInButton';
import AppMainContent from './components/AppMainContent';
import { ExampleAppGraphLoader } from './services/ExampleAppGraphLoader';

function App() {

  const [graphLoader, setGraphLoader] = useState<ExampleAppGraphLoader | null>(null);

  const isAuthenticated = useIsAuthenticated();
  const { instance, accounts } = useMsal();

  const RefreshGraphLoader = React.useCallback(() => {

    if (accounts.length > 0) {
      const [firstAccount] = accounts;
      instance.setActiveAccount(firstAccount);
    }

    setGraphLoader(new ExampleAppGraphLoader(instance, scopes));

  }, [accounts, instance]);

  React.useEffect(() => {
    
    // Get OAuth token
    if (isAuthenticated) {
      RefreshGraphLoader();
    }
  }, [RefreshGraphLoader, isAuthenticated]);

  return (
    <>
      <UnauthenticatedTemplate>
        <p>Sign in to Azure AD to access Graph resources.</p>
        <SignInButton permissions={scopes} />
      </UnauthenticatedTemplate>
      <AuthenticatedTemplate>
        {isAuthenticated && graphLoader ?
          <AppMainContent loader={graphLoader} />
          :
          <p>No account</p>
        }
      </AuthenticatedTemplate>

    </>
  );
}

export default App;
