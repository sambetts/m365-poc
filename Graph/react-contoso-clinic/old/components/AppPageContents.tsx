import { useState } from "react";
import { Client } from "@microsoft/microsoft-graph-client";
import { SCOPES } from "../constants";
import { GraphContainer } from "./lib/GraphContainer";
import {
  Image
} from "@fluentui/react-components";
import { AppointmentOrchestrator } from "./Appointments/AppointmentOrchestrator";
import { app } from "@microsoft/teams-js";
import { useData } from "@microsoft/teamsfx-react";
import "./AppPageContents.css";

export function AppPageContents() {

  const [graphClient, setGraphClient] = useState<Client | null>(null);
  const [appContext, setAppContext] = useState<app.Context | null>(null);


  useData(async () => {
    await app.initialize();
    const context = await app.getContext();
    setAppContext(context);
    return "Whatevs";
  });


  return (
    <div className="welcome page">
      <div className="narrow page-padding">
        <h1 className="center">Appointments</h1>
        <Image src="hello.png" />

        {appContext ?
          <>
            <GraphContainer scopes={SCOPES} onGraphClientValidated={(c: Client) => setGraphClient(c)}>

              {graphClient ?
                <AppointmentOrchestrator graphClient={graphClient} />
                :
                <p>Oops. We have auth but no Graph client and/or playlists to read? Reload app maybe?</p>
              }

            </GraphContainer>
          </>
          :
          <div>
            Loading app context...
          </div>
        }

      </div>
    </div>
  );
}
