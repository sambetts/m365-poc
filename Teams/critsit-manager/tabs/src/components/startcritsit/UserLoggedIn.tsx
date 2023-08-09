
import "./Tab.css";
import { UserInfo } from "@microsoft/teamsfx";
import { useData } from "@microsoft/teamsfx-react";
import { Client } from "@microsoft/microsoft-graph-client";

export function UserLoggedIn(props: { graphClient: Client }) {

  const { loading, data, error } = useData(async () => {

    const profile: UserInfo = await props.graphClient.api("/me").get();
    return profile;

    return;
  });

  return (
    <div>
      {data ?
        <h3>
          Hi, {data.displayName}
        </h3>
        :
        <>Loading...</>
      }

    </div>
  );
}
