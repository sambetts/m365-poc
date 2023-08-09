
import { Button, Input } from "@fluentui/react-northstar";
import { SyntheticEvent, useState } from "react";
import { OnlineMeeting } from "@microsoft/microsoft-graph-types";
import { Client } from "@microsoft/microsoft-graph-client";

export function CreateNewMeeting(props: { graphClient : Client, newMeetingSelection: Function }) {

  const [title, setTitle] = useState<string>("Contoso CritSit");

  const onChange = (e: SyntheticEvent<HTMLElement>) => {
    if (e.currentTarget instanceof HTMLInputElement) {
      setTitle(e.currentTarget.value);
    }
  }

  const createMeeting = async () => {

    var end = new Date();
    end.setHours(end.getHours() + 1);

    const newMeeting = {
      startDateTime: new Date().toISOString(),
      endDateTime: end.toISOString(),
      subject: title
    }
    const meeting: OnlineMeeting = await props.graphClient.api("/me/onlineMeetings").post(newMeeting);
    props.newMeetingSelection(meeting);
  }

  return (
    <div>
      <>
        <Input label="Title" required value={title} onChange={(e) => onChange(e)}
          successIndicator={title.length > 0} error={title.length === 0} />
        <Button content="Create new Meeting" onClick={createMeeting} disabled={title.length === 0} />
      </>
    </div>
  );
}
