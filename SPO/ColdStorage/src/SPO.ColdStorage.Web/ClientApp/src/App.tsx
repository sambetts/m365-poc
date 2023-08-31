import React, { useState } from 'react';
import { Route } from 'react-router';
import { Layout } from './components/Layout';
import { FileBrowser } from './components/FileBrowser/FileBrowser';
import { Login } from './components/Login';
import { FindFile } from './components/FileSearch/FindFile';
import { FindMigrationLog } from './components/MigrationLogs/FindMigrationLog';

import { AuthenticatedTemplate, UnauthenticatedTemplate, useIsAuthenticated, useMsal } from "@azure/msal-react";
import { loginRequest } from "./authConfig";

import './custom.css'
import { MigrationTargetsConfig } from './components/MigrationTargets/MigrationTargetsConfig';
import { Routes } from 'react-router-dom';

export default function App() {

    const [accessToken, setAccessToken] = useState<string | null>();
    const isAuthenticated = useIsAuthenticated();
    const { instance, accounts } = useMsal();

    const RequestAccessToken = React.useCallback(() => {
        const request = {
            ...loginRequest,
            account: accounts[0]
        };

        // Silently acquires an access token which is then attached to a request for Microsoft Graph data
        instance.acquireTokenSilent(request).then((response) => {
            setAccessToken(response.accessToken);
        }).catch((e) => {
            instance.acquireTokenPopup(request).then((response) => {
                setAccessToken(response.accessToken);
            });
        });
    }, [accounts, instance]);

    React.useEffect(() => {

        // Get OAuth token
        if (isAuthenticated && !accessToken) {
            RequestAccessToken();
        }
    }, [accessToken, RequestAccessToken, isAuthenticated]);


    return (
        <div>
            {accessToken ?
                (
                    <Layout>
                        <AuthenticatedTemplate>
                            <Routes>
                                <Route path='/' element={<FileBrowser {... { token: accessToken! }} />} />
                                <Route path='/FindFile' element={<FindFile {... { token: accessToken! }} />} />
                                <Route path='/FindMigrationLog' element={<FindMigrationLog {... { token: accessToken! }} />} />
                                <Route path='/MigrationTargets' element={<MigrationTargetsConfig {... { token: accessToken! }} />} />
                            </Routes>
                        </AuthenticatedTemplate>
                        <UnauthenticatedTemplate>
                            <Route path='/' element={<Login />} />
                        </UnauthenticatedTemplate>
                    </Layout>
                )
                :
                (
                    <Layout>
                        <UnauthenticatedTemplate>
                            <Routes><Route path='/' element={<Login />} /></Routes>
                        </UnauthenticatedTemplate>
                    </Layout>
                )}
        </div>
    );

}
