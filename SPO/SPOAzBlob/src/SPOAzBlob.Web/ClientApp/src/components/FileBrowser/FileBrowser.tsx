import { BlobServiceClient, ContainerClient } from '@azure/storage-blob';
import { BlobFileList } from './BlobFileList';
import '../NavMenu.css';
import React from 'react';
import { useIsAuthenticated, useMsal } from "@azure/msal-react";
import { getStorageConfigFromAPI, ServiceConfiguration } from '../ConfigReader'

export const FileBrowser: React.FC<{ token: string }> = (props) => {

  const [client, setClient] = React.useState<ContainerClient | null>(null);
  const [serviceConfiguration, setServiceConfiguration] = React.useState<ServiceConfiguration | null>(null);

  const [loading, setLoading] = React.useState<boolean>(false);

  const isAuthenticated = useIsAuthenticated();
  const { accounts } = useMsal();
  const getStorageConfig = React.useCallback(async (token) => 
  {
    return await getStorageConfigFromAPI(token).then((response : ServiceConfiguration)  => {
      setLoading(false);
      return Promise.resolve(response);
    });
  }, []);
  React.useEffect(() => {

    // Load storage config first
    getStorageConfig(props.token)
      .then((storageConfigInfo: ServiceConfiguration) => {
        console.log('Got service config from site API');
        setServiceConfiguration(storageConfigInfo);

        // Create a new BlobServiceClient based on config loaded from our own API
        const blobServiceClient = new BlobServiceClient(`${storageConfigInfo.storageInfo.accountURI}${storageConfigInfo.storageInfo.sharedAccessToken}`);

        const containerName = storageConfigInfo.storageInfo.containerName;
        const blobStorageClient = blobServiceClient.getContainerClient(containerName);

        setClient(blobStorageClient);
      })
      .catch((error) => 
      {
        console.log(error);
        alert(error);
      });

  }, [getStorageConfig, isAuthenticated, props]);

  const name = accounts[0] && accounts[0].name;
  return (
    <div>
      <h1>SPOAzBlob Web</h1>
      <p>Click on a file to start editing.</p>
      <span>Signed in as: {name}</span>
      <p><b>Files in Storage Account:</b></p>
      {!loading && client ?
        (
          <div>
            <BlobFileList containerClient={client!} accessToken={props.token} config={serviceConfiguration!} />
          </div>
        )
        : <div>Loading storage config...</div>
      }
    </div>
  );
};
