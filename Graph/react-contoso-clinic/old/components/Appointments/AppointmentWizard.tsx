
import { Button } from "@fluentui/react-components";
import { Client } from "@microsoft/microsoft-graph-client";

import { BookingBusiness } from "@microsoft/microsoft-graph-types";

export function AppointmentWizard(props: { graphClient: Client, business: BookingBusiness }) {

  const startCall = () => {
  }

  return (
    <div>
      <h3>{props.business.displayName}</h3>
      <Button onClick={startCall}>New Appointment</Button>
    </div >
  );
}
