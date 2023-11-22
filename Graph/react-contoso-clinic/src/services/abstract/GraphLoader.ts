import { AuthenticationResult, IPublicClientApplication } from "@azure/msal-browser";
import { Client, PageCollection } from "@microsoft/microsoft-graph-client";

// Abstracts Graph call loading and OAuth authentication. 
// Can only be used once MSAL has authenticated and set a main account.
export class GraphLoader {
    _perms: string[];
    _instance: IPublicClientApplication;
    constructor(instance: IPublicClientApplication, perms: string[]) {
        this._perms = perms;
        this._instance = instance;
    }

    loadList<T>(graphRelativeUrl: string, maxRecords: number): Promise<T> {
        return this.getGraphClient().then(graphClient => {
            return graphClient.api(graphRelativeUrl).top(maxRecords).get().then((r: PageCollection) => {
                return Promise.resolve(r.value) as T;
            });
        });
    }

    loadSingle<T>(graphRelativeUrl: string): Promise<T> {
        return this.getGraphClient().then(graphClient => {
            return graphClient.api(graphRelativeUrl).get();
        });
    }

    getAccessToken(): Promise<AuthenticationResult> {

        const primaryAccount = this._instance.getActiveAccount();
        if (!primaryAccount) {
            throw new Error("No primary account configured on MSAL instance")
        }

        const request = {
            scopes: this._perms,
            account: primaryAccount
        };

        // https://learn.microsoft.com/en-us/azure/active-directory/develop/scenario-spa-acquire-token?tabs=javascript2
        return this._instance.acquireTokenSilent(request).then((response) => {
            return Promise.resolve(response);
        }).catch((e) => {
            return this._instance.acquireTokenPopup(request).then((response) => {
                return Promise.resolve(response);
            });
        });
    }

    getGraphClient(): Promise<Client> {
        return this.getAccessToken().then(auth => {

            // Initialize Graph client
            const graphClient = Client.init({
                // Use the provided access token to authenticate requests
                authProvider: (done) => {
                    done(null, auth.accessToken);
                },
            });

            return graphClient;
        });
    }
}
