

import { BookingAppointment, BookingBusiness } from "@microsoft/microsoft-graph-types";
import { Button } from "react-bootstrap";
import { ExampleAppGraphLoader } from "../../services/ExampleAppGraphLoader";
import { useEffect, useState } from "react";

export function AppointmentWizard(props: { loader: ExampleAppGraphLoader, business: BookingBusiness }) {

  const [appointments, setAppointments] = useState<microsoftgraph.BookingAppointment[] | null>(null);

  const startCall = () => {
  }

  useEffect(() => {

    if (props.business.id) {
      props.loader.loadBusinessAppointments(props.business.id).then((r: BookingAppointment[]) => {
        setAppointments(r);
      });
    }

  }, [props.loader]);

  return (
    <div>
      <h3>{props.business.displayName}</h3>

      {appointments &&

        <>
          <pre>{JSON.stringify(appointments.map(a => a.startDateTime!))}</pre>
        </>

      }
      <pre>{JSON.stringify(appointments)}</pre>
      <Button onClick={startCall}>New Appointment</Button>
    </div >
  );
}
