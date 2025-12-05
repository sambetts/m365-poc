import { BlobServiceClient, ContainerClient } from '@azure/storage-blob';
import { BlobFileList } from './BlobFileList';
import '../NavMenu.css';
import React from 'react';
import { useIsAuthenticated, useMsal } from "@azure/msal-react";
import { getStorageConfigFromAPI, ServiceConfiguration } from '../ConfigReader';
import { storageRequest } from '../../authConfig';

export const FileBrowser: React.FC<{ token: string }> = (props) => {

  const [client, setClient] = React.useState<ContainerClient | null>(null);
  const [serviceConfiguration, setServiceConfiguration] = React.useState<ServiceConfiguration | null>(null);
  const [storageToken, setStorageToken] = React.useState<string | null>(null);

  const [loading, setLoading] = React.useState<boolean>(false);

  const isAuthenticated = useIsAuthenticated();
  const { accounts, instance } = useMsal();
  const getStorageConfig = React.useCallback(async (token : string) => 
  {
    return await getStorageConfigFromAPI(token).then((response : ServiceConfiguration)  => {
      setLoading(false);
      return Promise.resolve(response);
    });
  }, []);

  // Acquire storage token separately
  React.useEffect(() => {
    if (isAuthenticated && !storageToken) {
      const request = {
        ...storageRequest,
        account: accounts[0]
      };

      instance.acquireTokenSilent(request)
        .then((response) => {
          setStorageToken(response.accessToken);
        })
        .catch((e) => {
          instance.acquireTokenPopup(request)
            .then((response) => {
              setStorageToken(response.accessToken);
            });
        });
    }
  }, [isAuthenticated, storageToken, accounts, instance]);

  React.useEffect(() => {
    if (!storageToken) return;

    // Load storage config first
    getStorageConfig(props.token)
      .then((storageConfigInfo: ServiceConfiguration) => {
        console.log('Got service config from site API');
        setServiceConfiguration(storageConfigInfo);

        // Create a custom credential that uses the storage access token
        const tokenCredential = {
          getToken: async () => {
            return {
              token: storageToken,
              expiresOnTimestamp: Date.now() + 3600000 // 1 hour from now
            };
          }
        };

        // Create a new BlobServiceClient using Azure AD authentication
        const blobServiceClient = new BlobServiceClient(
          storageConfigInfo.storageInfo.accountURI,
          tokenCredential
        );

        const containerName = storageConfigInfo.storageInfo.containerName;
        const blobStorageClient = blobServiceClient.getContainerClient(containerName);

        setClient(blobStorageClient);
      });

  }, [getStorageConfig, props.token, storageToken]);

  const name = accounts[0] && accounts[0].name;
  return (
    <div>
      <h1>Cold Storage Access Web</h1>

      <p>This application is for finding files moved into Azure Blob cold storage.</p>

      <span>Signed In: {name}</span>
      <p><b>Files in Storage Account:</b></p>

      {!loading && client ?
        (
          <div>
            <BlobFileList client={client!} accessToken={props.token} storageInfo={serviceConfiguration!.storageInfo} />
          </div>
        )
        : <div>Loading</div>
      }
    </div>
  );
};
