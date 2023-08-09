import { useContext, useState } from "react";
import { Button, Image } from "@fluentui/react-northstar";
import "./Tab.css";
import { useData } from "@microsoft/teamsfx-react";
import { TeamsFxContext } from "../Context";
import { createMicrosoftGraphClientWithCredential, ErrorCode, ErrorWithCode } from "@microsoft/teamsfx";
import { Client, GraphError } from "@microsoft/microsoft-graph-client";
import { SCOPES } from "../../constants";
import { Providers, ProviderState } from "@microsoft/mgt-element";
import { TeamsFxProvider } from "@microsoft/mgt-teamsfx-provider";
import { NewIncidentForm } from "./NewIncidentForm";

export function NewIncident(props: {}) {

  const { teamsUserCredential } = useContext(TeamsFxContext);
  const [graphClient, setGraphClient] = useState<Client | null>(null);
  const [graphError, setGraphError] = useState<GraphError | null>(null);
  const [errorText, setErrorText] = useState<string | null>(null);

    // Manual Login
  const authGraph = async () => {

    if (teamsUserCredential) {
      try {
        await teamsUserCredential.login(SCOPES);

        setErrorText(null);
        setGraphError(null);

      } catch (err: unknown) {
        if (err instanceof ErrorWithCode && err.code !== ErrorCode.ConsentFailed) {
          throw err;
        }
        else {
          // Silently fail because user cancels the consent dialog or popup blocker is in use
          setErrorText(JSON.stringify(err))
          alert('Could not login to Graph. Check popup blocker and reload?');
          return;
        }
      }
    }
  }

  // Test a Graph call
  const { loading, data, error } = useData(async () => {
    if (teamsUserCredential) {
      try {
        const c : Client = createMicrosoftGraphClientWithCredential(teamsUserCredential, SCOPES);

        if (c) {

          // Test client
          await c.api("/me").get();
          setGraphClient(c);

          // Initialize Graph Toolkit TeamsFx provider
          const provider = new TeamsFxProvider(teamsUserCredential, SCOPES);
          Providers.globalProvider = provider;
          Providers.globalProvider.setState(ProviderState.SignedIn);
        }

      } catch (err: unknown) {
        if (err instanceof GraphError) {
          setGraphError(err);
        } else {
          console.error(err);
        }
      }
    }
    return;
  });

  return (
    <div className="welcome page">
      <div className="narrow page-padding">
        <Image src="hello.png" />
        <h1 className="center">Manage Critsit</h1>

        {graphError ?
          <>
            {graphError.code === 'ErrorWithCode.UiRequiredError' ?
              <><div>We need your consent for Graph access. Login below:</div>
                <Button primary content="Authorize" disabled={loading} onClick={authGraph} />
              </>
              :
              <p>Unknown error: {graphError.code}</p>
            }
          </>
          :
          <>
            <div className="sections">
              {teamsUserCredential && graphClient &&
                <>
                  <NewIncidentForm teamsUserCredential={teamsUserCredential} graphClient={graphClient} />
                </>
              }
            </div>
          </>
        }

        {errorText &&
          <pre>{errorText}</pre>
        }
      </div>
    </div>
  );
}
