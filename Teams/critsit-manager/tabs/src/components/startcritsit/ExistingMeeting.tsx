
import { Button, Input } from "@fluentui/react-northstar";
import { OnlineMeeting } from "@microsoft/microsoft-graph-types";

export function ExistingMeeting(props: { meeting: OnlineMeeting, meetingCancelled: Function }) {
  
  const deleteMeeting = () => {
    props.meetingCancelled();
  }

  return (
    <div>
      <>
        <Input fluid label="Meeting Join URL" required value={props.meeting.joinWebUrl ?? ""} readOnly />
        <Button content="Cancel" primary onClick={deleteMeeting} />
      </>
    </div>
  );
}
