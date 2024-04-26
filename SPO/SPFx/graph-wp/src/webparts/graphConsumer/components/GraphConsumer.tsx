import * as React from 'react';
import styles from './GraphConsumer.module.scss';
import type { IGraphConsumerProps } from './IGraphConsumerProps';
import { TextField, PrimaryButton, DetailsList, CheckboxVisibility, SelectionMode, DetailsListLayoutMode, BaseButton, Button } from '@fluentui/react';
import * as strings from 'GraphConsumerWebPartStrings';
import { IGraphConsumerState } from './IGraphConsumerState';
import { ClientMode } from './ClientMode';
import { IUserItem } from './IUserItem';

import { escape } from "@microsoft/sp-lodash-subset";

import { AadHttpClient, MSGraphClientV3 } from "@microsoft/sp-http";


// Configure the columns for the DetailsList component
const _usersListColumns = [
  {
    key: "displayName",
    name: "Display name",
    fieldName: "displayName",
    minWidth: 50,
    maxWidth: 100,
    isResizable: true,
  },
  {
    key: "mail",
    name: "Mail",
    fieldName: "mail",
    minWidth: 50,
    maxWidth: 100,
    isResizable: true,
  },
  {
    key: "userPrincipalName",
    name: "User Principal Name",
    fieldName: "userPrincipalName",
    minWidth: 100,
    maxWidth: 200,
    isResizable: true,
  },
];

export default class GraphConsumer extends React.Component<IGraphConsumerProps, IGraphConsumerState> {

  constructor(props: IGraphConsumerProps, state: IGraphConsumerState) {
    super(props);

    // Initialize the state of the component
    this.state = {
      users: null,
      searchFor: ""
    };

    this._search = this._search.bind(this);
  }

  public render(): React.ReactElement<IGraphConsumerProps> {
    return (
      <div className={styles.graphConsumer}>
        <div>
          <div>
            <div >
              <span>Search for a user!</span>
              <p className={styles.form}>
                <TextField
                  label={strings.SearchFor}
                  required={true}
                  onChange={this._onSearchForChanged}
                  onGetErrorMessage={this._getSearchForErrorMessage}
                  value={this.state.searchFor}
                />
              </p>
              <p className={styles.form}>
                <PrimaryButton
                  text='Search'
                  title='Search'
                  onClick={this._search}
                />
              </p>
              {
                (this.state.users !== null) ?
                  <>
                    {(this.state.users.length > 0) ?
                      <p className={styles.form}>
                        <DetailsList
                          items={this.state.users}
                          columns={_usersListColumns}
                          setKey='set'
                          checkboxVisibility={CheckboxVisibility.hidden}
                          selectionMode={SelectionMode.none}
                          layoutMode={DetailsListLayoutMode.fixedColumns}
                          compact={true}
                        />
                      </p>
                      : <p>No results</p>
                    }
                  </>
                  : <p>Search for someone</p>
              }
            </div>
          </div>
        </div>
      </div>
    );
  }

  private _onSearchForChanged = (event: React.FormEvent<HTMLInputElement | HTMLTextAreaElement>, newValue?: string): void => {

    // Update the component state accordingly to the current user's input
    this.setState({
      searchFor: newValue ?? "",
    });
  }

  private _getSearchForErrorMessage = (value: string): string => {
    // The search for text cannot contain spaces
    return (value === null || value.length === 0 || value.indexOf(" ") < 0)
      ? ''
      : `${strings.SearchForValidationErrorMessage}`;
  }

  private _search = (event: React.MouseEvent<HTMLAnchorElement | HTMLButtonElement | HTMLDivElement | BaseButton | Button, MouseEvent>): void => {
    console.log(this.props.clientMode);

    if (this.props.clientMode === undefined) {
      console.error("Client mode is not defined");
      return;
    }
    // Based on the clientMode value search users
    switch (this.props.clientMode) {
      case ClientMode.aad:
        this._searchWithAad();
        break;
      case ClientMode.graph:
        this._searchWithGraph();
        break;
    }
  }

  private _searchWithGraph = (): void => {

    // Log the current operation
    console.log("Using _searchWithGraph() method");

    this.props.context.msGraphClientFactory
      .getClient('3')
      .then((client: MSGraphClientV3) => {
        // From https://github.com/microsoftgraph/msgraph-sdk-javascript sample
        client
          .api("users")
          .version("v1.0")
          .select("displayName,mail,userPrincipalName")
          .filter(`(givenName eq '${escape(this.state.searchFor)}') or (surname eq '${escape(this.state.searchFor)}') or (displayName eq '${escape(this.state.searchFor)}')`)
          .get((err, res) => {

            if (err) {
              console.error(err);
              return;
            }

            // Prepare the output array
            const users: Array<IUserItem> = new Array<IUserItem>();

            // Map the JSON response to the output array
            res.value.map((item: any) => {
              users.push({
                displayName: item.displayName,
                mail: item.mail,
                userPrincipalName: item.userPrincipalName,
              });
            });

            // Update the component state accordingly to the result
            this.setState(
              {
                users: users,
              }
            );
          })
          .catch(() => alert("Error loading Graph results"));
      })
      .catch(() => alert("Error loading Graph client"));
  }

  private _searchWithAad = (): void => {
    // Log the current operation
    console.log("Using _searchWithAad() method");

    // Using Graph here, but any 1st or 3rd party REST API that requires Azure AD auth can be used here.
    this.props.context.aadHttpClientFactory
      .getClient("https://graph.microsoft.com")
      .then((client: AadHttpClient) => {
        // Search for the users with givenName, surname, or displayName equal to the searchFor value
        return client
          .get(
            `https://graph.microsoft.com/v1.0/users?$select=displayName,mail,userPrincipalName&$filter=(givenName%20eq%20'${escape(this.state.searchFor)}')%20or%20(surname%20eq%20'${escape(this.state.searchFor)}')%20or%20(displayName%20eq%20'${escape(this.state.searchFor)}')`,
            AadHttpClient.configurations.v1
          );
      })
      .then(response => {
        return response.json();
      })
      .then(json => {

        // Prepare the output array
        const users: Array<IUserItem> = new Array<IUserItem>();

        // Log the result in the console for testing purposes
        console.log(json);

        // Map the JSON response to the output array
        json.value.map((item: any) => {
          users.push({
            displayName: item.displayName,
            mail: item.mail,
            userPrincipalName: item.userPrincipalName,
          });
        });

        // Update the component state accordingly to the result
        this.setState(
          {
            users: users,
          }
        );
      })
      .catch(error => {
        console.error(error);
      });
  }
}
