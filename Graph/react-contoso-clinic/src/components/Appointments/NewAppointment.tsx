

import { Button } from "react-bootstrap";
import { useEffect, useState } from "react";
import { GetDatesBetween, GetDatesExcluding } from "../../services/DateFunctions";
import { TimeslotPicker } from "../common/TimeslotPicker";
import { BookingAppointment, BookingStaffMember } from "@microsoft/microsoft-graph-types";
import { StaffList } from "./StaffList";

export function NewAppointment(props: { existingAppointments: BookingAppointment[], staffMembers: BookingStaffMember[], newAppointment: Function }) {

  const [dateSlots, setDateSlots] = useState<Date[] | null>(null);
  const [selectedDate, setSelectedDate] = useState<Date | null>(null);
  const [selectedStaff, setSelectedStaff] = useState<BookingStaffMember[] | null>(null);

  const startCall = () => {
    const newAppReq = {};
    props.newAppointment(newAppReq);
  }

  const now = new Date();
  const dayStart = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 9)
  const dayEnd = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 18);

  useEffect(() => {

    // Figure out dates available
    const dayDatesAll = GetDatesBetween(dayStart, dayEnd, 1);
    const appointmentDates = props.existingAppointments.map(a => a.startDateTime!).map(d => new Date(d.dateTime!));

    setDateSlots(GetDatesExcluding(dayDatesAll, appointmentDates));

  }, [props.existingAppointments]);

  return (
    <div>
      {dateSlots &&
        <>
          <p>Select date:</p>
          <TimeslotPicker options={dateSlots} optionSelected={(dt: Date) => setSelectedDate(dt)} />
          <pre>{JSON.stringify(selectedDate)}</pre>

          
          <p>Select staff:</p>
          <StaffList allStaff={props.staffMembers} newStaffList={(s: BookingStaffMember[]) => setSelectedStaff(s)} />
          <pre>{JSON.stringify(selectedStaff)}</pre>
        </>
      }
      <Button onClick={startCall}>Confirm</Button>
    </div >
  );
}
