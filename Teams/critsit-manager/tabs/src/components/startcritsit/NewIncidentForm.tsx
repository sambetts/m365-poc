
import { TeamsUserCredential, UserInfo } from "@microsoft/teamsfx";
import { useState } from "react";
import { UserLoggedIn } from "./UserLoggedIn";
import { UserList } from "./UserList";
import { Button } from "@fluentui/react-northstar";
import { CreateNewMeeting } from "./CreateNewMeeting";
import { OnlineMeeting, User } from "@microsoft/microsoft-graph-types";
import { ExistingMeeting } from "./ExistingMeeting";
import { Client } from "@microsoft/microsoft-graph-client";

export function NewIncidentForm(props: { teamsUserCredential: TeamsUserCredential, graphClient: Client }) {

  const [users, setUsers] = useState<UserInfo[]>([]);
  const [isLoadingBot, setIsLoadingBot] = useState<boolean>(false);
  const [onlineMeeting, setOnlineMeeting] = useState<OnlineMeeting | null>(null);
  const [goText, setGoText] = useState<string>("Create CritSit");

  const handleSelectionChanged = (e: UserInfo[]) => {
    setUsers(e);
    setGoText(`Create CritSit for ${e.length} Users`)
  };


  const handleNewMeetingSelection = (e: OnlineMeeting) => {
    setOnlineMeeting(e);
  };

  const notifyBot = () => {

    setIsLoadingBot(true);

    let userIds: string[] = [];
    users.map((u: User) => {
      userIds.push(u.id ?? "");
    });

    const botBody = {
      name: "Test",
      time: "12:00:00 05/29/2022",
      tenantId: "f6cd3d8c-b7de-4b84-a736-446fa13aefe0",
      objectIds: userIds,
      joinURL: onlineMeeting?.joinWebUrl,
      "removeFromDefaultRoutingGroup": true,
      "allowConversationWithoutHost": true
    }

    var req: any = {
      method: "POST",
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(botBody)
    };

    const url = "https://sambettsbot.eu.ngrok.io/incidents/raise";
    fetch(url, req)
      .then(async response => {

        setIsLoadingBot(false);
        if (response.ok) {
          const dataText: string = await response.text();
          console.info(`${url}: '${dataText}'`);
          alert('Incident created. Users being called now to join');
        }
        else {
          const dataText: string = await response.text();
          const errorTitle = `Error ${response.status} POSTing to/from API '${url}'`;

          if (dataText !== "")
            alert(`${errorTitle}: ${dataText}`)
          else
            alert(errorTitle);

        }
      });
  }

  return (
    <div>
      <>
        <UserLoggedIn graphClient={props.graphClient} />
        <p>Create and meeting and have Incident Bot call everyone below:</p>
        {onlineMeeting ?
          <><ExistingMeeting meeting={onlineMeeting} meetingCancelled={() => setOnlineMeeting(null)} /></>
          :
          <><CreateNewMeeting graphClient={props.graphClient} newMeetingSelection={handleNewMeetingSelection} /></>
        }

        <UserList teamsUserCredential={props.teamsUserCredential} newUserSelection={handleSelectionChanged} />

        <div>
          {isLoadingBot === true ?
            <div>Sending to bot...</div>
            :
            <Button content={goText} primary disabled={users.length === 0 || onlineMeeting === null}
              onClick={notifyBot} />
          }

        </div>
      </>
    </div>
  );
}
