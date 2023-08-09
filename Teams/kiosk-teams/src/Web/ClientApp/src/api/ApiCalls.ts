import { loadFromApi } from "./ApiLoader";

export const getStorageConfigFromAPI = async (): Promise<ServiceConfiguration> => {
  return loadFromApi('AppConfiguration/GetServiceConfiguration', 'GET')
    .then(async response => {
      const d: ServiceConfiguration = JSON.parse(response);
      return d;
    });
}

export const getCurrentPlayListItem = async (clientTerminalName: string): Promise<PlayListItem | null> => {
  return loadFromApi('Playlist/now?scope=' + clientTerminalName, 'GET')
    .then(async response => {
      if (response && response !== "") {
        const d: PlayListItem = JSON.parse(response);
        return d;
      }
      return Promise.resolve(null);
    });
}

