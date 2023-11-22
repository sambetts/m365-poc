import React, { useState } from 'react';
import './App.css';
import { AuthenticatedTemplate, UnauthenticatedTemplate, useIsAuthenticated, useMsal } from '@azure/msal-react';
import { scopes } from './authConfig';
import { SignInButton } from './components/SignInButton';
import AppMainContent from './components/AppMainContent';
import { ExampleAppGraphLoader } from './services/ExampleAppGraphLoader';
import AppTemplate from './components/AppTemplate';
import { UserLoaderCache } from './services/GraphObjectsLoaderCaches';

function App() {

  const [userCache, setUserCache] = useState<UserLoaderCache | null>(null);
  const [graphLoader, setGraphLoader] = useState<ExampleAppGraphLoader | null>(null);
  const [loginError, setLoginError] = useState<Error | null>(null);

  const [user, setUser] = useState<microsoftgraph.User | undefined>(undefined);

  const isAuthenticated = useIsAuthenticated();
  const { instance, accounts } = useMsal();

  const RefreshGraphLoader = React.useCallback(() => {

    if (accounts.length > 0) {
      const [firstAccount] = accounts;
      instance.setActiveAccount(firstAccount);
    }

    const loader = new ExampleAppGraphLoader(instance, scopes);
    setGraphLoader(loader);
    setUserCache(new UserLoaderCache(loader))

  }, [accounts, instance]);

  React.useEffect(() => {

    // Get OAuth token
    if (isAuthenticated) {
      RefreshGraphLoader();
    }
  }, [RefreshGraphLoader, isAuthenticated]);

  return (
    <>
      <AppTemplate user={user}>
        <UnauthenticatedTemplate>
          <p>Sign in to Azure AD to access Graph resources.</p>

          {loginError &&
            <pre>{JSON.stringify(loginError)}</pre>
          }

          <SignInButton permissions={scopes} onError={(er: Error) => setLoginError(er)} />
        </UnauthenticatedTemplate>
        <AuthenticatedTemplate>
          {isAuthenticated && graphLoader && userCache ?
            <AppMainContent loader={graphLoader} userLoaded={(u: microsoftgraph.User) => setUser(u)} userCache={userCache} />
            :
            <p>No account</p>
          }
        </AuthenticatedTemplate>
      </AppTemplate>

    </>
  );
}

export default App;
