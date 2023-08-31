import { BlobItem } from "@azure/storage-blob";


export interface DriveItem {
    webUrl: string
}

export const startAzureFileEdit = async (token: string, url: string): Promise<DriveItem> => {

    var urlEncoded = encodeURIComponent(url);

    return fetch('EditActions/StartEdit?url=' + urlEncoded, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': 'Bearer ' + token,
        }
    }
    )
        .then(async response => {

            if (response.ok) {
                // Edit & lock applied. Return driveitem
                var driveItem: DriveItem = await response.json();

                return Promise.resolve(driveItem);
            }
            else {
                const dataText: string = await response.text();
                return Promise.reject(dataText);
            }
        });
};


export const releaseLock = async (token: string, lock: FileLock): Promise<undefined | void> => {
    return fetch('EditActions/ReleaseLock?driveItemId=' + lock.rowKey, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': 'Bearer ' + token,
        }
    }
    )
        .then(async response => {

            if (response.ok) {
                return Promise.resolve();
            }
            else {
                const dataText: string = await response.text();
                return Promise.reject(dataText);
            }
        });
};

export interface FileLock
{
    rowKey: string,
    azureBlobUrl: string,
    webUrl: string,
    lockedByUser: string
}
export const getActiveLocks = async (token: string): Promise<FileLock[]> => {

    return fetch('EditActions/GetActiveLocks', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': 'Bearer ' + token,
        }
    }
    )
        .then(async response => {

            if (response.ok) {
                var driveItem: FileLock[] = await response.json();

                return Promise.resolve(driveItem);
            }
            else {
                const dataText: string = await response.text();
                return Promise.reject(dataText);
            }
        });
};


export const getSubscriptionsConfig = async (token: string): Promise<WebhooksState> => {

    return fetch('WebhookAdmin/SubscriptionsConfig', {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': 'Bearer ' + token,
        }
    }
    )
        .then(async response => {

            if (response.ok) {
                var data: WebhooksState = await response.json();

                return Promise.resolve(data);
            }
            else {
                const dataText: string = await response.text();
                return Promise.reject(dataText);
            }
        });
};
export interface Subscription
{
    notificationUrl: string,
    resource: string,
    changeType: string,
    expirationDateTime: Date
}
export interface WebhooksState
{
    targetEndpoint: string,
    subscriptions: Subscription[]
}

export const postCreateOrUpdateSubscription = async (token: string): Promise<Subscription> => {

    return fetch('WebhookAdmin/CreateOrUpdateSubscription', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': 'Bearer ' + token,
        }
    }
    )
        .then(async response => {

            if (response.ok) {
                var data: Subscription = await response.json();

                return Promise.resolve(data);
            }
            else {
                const dataText: string = await response.text();
                return Promise.reject(dataText);
            }
        });
};
export interface BlobWithLock
{
    blob: BlobItem,
    lock: FileLock | null
}