
export const getStorageConfigFromAPI = async (token : string) => {
  return await fetch('AppConfiguration/GetServiceConfiguration', {
    method: 'GET',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': 'Bearer ' + token,
    }
  }
  )
    .then(async response => {
      if (response.ok)
      {
        const data: ServiceConfiguration = await response.json();
        return Promise.resolve(data);
      }
      else
      {
        const dataText: string = await response.text();
        return Promise.reject(dataText);
      }
    });
};


export interface StorageInfo {
  sharedAccessToken: string,
  accountURI: string,
  containerName: string
}


export interface ServiceConfiguration {
  storageInfo: StorageInfo,
  webhookUrl: string,
  baseSharePointDriveUrl: string
}
