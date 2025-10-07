import React, { useState } from 'react';
import './App.css';
import { AuthenticatedTemplate, UnauthenticatedTemplate, useIsAuthenticated, useMsal } from '@azure/msal-react';
import { defaultScopes } from './authConfig';
import { ScopeSelector } from './components/ScopeSelector';
import AppMainContent from './components/AppMainContent';
import { ExampleAppGraphLoader } from './services/ExampleAppGraphLoader';

function App() {

  const [graphLoader, setGraphLoader] = useState<ExampleAppGraphLoader | null>(null);
  const [selectedScopes, setSelectedScopes] = useState<string[]>(defaultScopes);

  const isAuthenticated = useIsAuthenticated();
  const { instance, accounts } = useMsal();

  const handleScopesSelected = (scopes: string[]) => {
    setSelectedScopes(scopes);
  };

  const RefreshGraphLoader = React.useCallback(() => {

    if (accounts.length > 0) {
      const [firstAccount] = accounts;
      instance.setActiveAccount(firstAccount);
    }

    setGraphLoader(new ExampleAppGraphLoader(instance, selectedScopes));

  }, [accounts, instance, selectedScopes]);

  React.useEffect(() => {
    
    // Get OAuth token
    if (isAuthenticated) {
      RefreshGraphLoader();
    }
  }, [RefreshGraphLoader, isAuthenticated]);

  return (
    <>
      <UnauthenticatedTemplate>
        <ScopeSelector onScopesSelected={handleScopesSelected} msalInstance={instance} />
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
