import { useContext } from "react";
import { TeamsFxContext } from "./Context";
import { NewIncident } from "./startcritsit/NewIncident";

export default function Tab() {
  const { themeString } = useContext(TeamsFxContext);
  return (
    <div className={themeString === "default" ? "" : "dark"}>
      <NewIncident />
    </div>
  );
}
