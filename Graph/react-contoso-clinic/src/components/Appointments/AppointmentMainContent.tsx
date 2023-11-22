

import { BookingAppointment, BookingBusiness } from "@microsoft/microsoft-graph-types";
import { Button } from "react-bootstrap";
import { ExampleAppGraphLoader } from "../../services/ExampleAppGraphLoader";
import { useEffect, useState } from "react";
import { GetDatesBetween, GetDatesExcluding } from "../../services/DateFunctions";
import { AppointmentsList } from "./AppointmentsList";
import { UserLoaderCache } from "../../services/GraphObjectsLoaderCaches";

export function AppointmentMainContent(props: { loader: ExampleAppGraphLoader, userCache : UserLoaderCache, business: BookingBusiness }) {

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

        // Figure out dates available
        const dayDatesAll = GetDatesBetween(dayStart, dayEnd, 1);
        const appointmentDates = r.map(a => a.startDateTime!).map(d=> new Date(d.dateTime!));

        setDateSlots(GetDatesExcluding(dayDatesAll, appointmentDates));
      });
    }
    
    // eslint-disable-next-line
  }, []);

  return (
    <div>
      <h1>Selected Org: {props.business.displayName}</h1>

      {appointments &&
        <>
        <h3>Existing Appointments</h3>
          <AppointmentsList data={appointments} loader={props.loader} forBusiness={props.business} />
          <pre>{JSON.stringify(dateSlots)}</pre>
        </>
      }
      <pre>{JSON.stringify(appointments)}</pre>
      <Button onClick={startCall}>New Appointment</Button>
    </div >
  );
}
