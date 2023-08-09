
import { TeamsUserCredential } from "@microsoft/teamsfx";
import { PeoplePicker } from "@microsoft/mgt-react";

export function UserList(props: { teamsUserCredential: TeamsUserCredential, newUserSelection: Function }) {
  
  const handleSelectionChanged = (e: any ) => {

    if (e.target) {
      
      const peopleChangeEvent = e.target;
      props.newUserSelection(peopleChangeEvent.selectedPeople);
    }
  };

  return (
    <div>
      <>
        <PeoplePicker selectionChanged={handleSelectionChanged} />
      </>
    </div>
  );
}
