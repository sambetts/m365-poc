

import { BookingAppointment, BookingBusiness } from "@microsoft/microsoft-graph-types";
import { Button } from "react-bootstrap";
import { ExampleAppGraphLoader } from "../../services/ExampleAppGraphLoader";
import { useEffect, useState } from "react";
import { GetDatesBetween, GetDatesExcluding } from "../../services/DateFunctions";

export function AppointmentWizard(props: { loader: ExampleAppGraphLoader, business: BookingBusiness }) {

  const [appointments, setAppointments] = useState<microsoftgraph.BookingAppointment[] | null>(null);
  const [dateSlots, setDateSlots] = useState<Date[] | null>(null);

  const startCall = () => {
  }

  const now = new Date();
  const dayStart = new Date (now.getFullYear (), now.getMonth (), now.getDate (), 9)
  const dayEnd = new Date (now.getFullYear (), now.getMonth (), now.getDate (), 18);

  useEffect(() => {

    if (props.business.id) {
      props.loader.loadBusinessAppointments(props.business.id).then((r: BookingAppointment[]) => {
        setAppointments(r);

        const dayDatesAll = GetDatesBetween(dayStart, dayEnd, 1);
        const appointmentDates = r.map(a => a.startDateTime!).map(d=> new Date(d.dateTime!));

        setDateSlots(GetDatesExcluding(dayDatesAll, appointmentDates));
      });
    }
  }, [props.loader]);

  return (
    <div>
      <h3>{props.business.displayName}</h3>

      {appointments &&

        <>
          <pre>{JSON.stringify(dateSlots)}</pre>
        </>

      }
      <pre>{JSON.stringify(appointments)}</pre>
      <Button onClick={startCall}>New Appointment</Button>
    </div >
  );
}
