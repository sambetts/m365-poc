

import { Button } from "react-bootstrap";
import { useEffect, useState } from "react";
import { GetDatesBetween, GetDatesExcluding, addHour } from "../../services/DateFunctions";
import { TimeslotPicker } from "../common/TimeslotPicker";
import { BookingAppointment, BookingCustomer, BookingService, BookingStaffMember } from "@microsoft/microsoft-graph-types";
import { StaffList } from "./StaffList";
import { ServicePicker } from "../common/ServicePicker";

interface Props {
  existingAppointments: BookingAppointment[],
  staffMembers: BookingStaffMember[],
  services: BookingService[],
  forCustomer: BookingCustomer,
  newAppointment: Function
}

export function NewAppointment(props: Props) {

  const [dateSlots, setDateSlots] = useState<Date[] | null>(null);
  const [selectedDate, setSelectedDate] = useState<Date | null>(null);
  const [selectedService, setSelectedService] = useState<BookingService | null>(null);
  const [selectedStaff, setSelectedStaff] = useState<BookingStaffMember[] | null>(null);

  const newAppointment = () => {

    if (!selectedService || !selectedDate || !selectedStaff || selectedStaff?.length === 0) {
      alert("Fill out form");
      return;
    }

    const bookingAppointment: BookingAppointment = {
      startDateTime: {
        dateTime: selectedDate.toISOString(),
        timeZone: 'UTC'
      },
      endDateTime: {
        dateTime: addHour(selectedDate, 1).toISOString(),
        timeZone: 'UTC'
      },
      price: 0,
      priceType: "notSet",
      isLocationOnline: true,
      optOutOfCustomerEmail: false,
      anonymousJoinWebUrl: null,
      postBuffer: 'PT10M',
      preBuffer: 'PT5M',
      reminders: [],
      serviceId: selectedService.id,
      serviceName: selectedService.displayName,
      serviceNotes: 'Customer requires punctual service.',
      "serviceLocation": {
        "address": {
          "city": "Buffalo",
          "countryOrRegion": "USA",
          "postalCode": "98052",
          "state": "NY",
          "street": "123 First Avenue",
        },
        "coordinates": null,
        "displayName": "Customer location",
        "locationEmailAddress": null,
        "locationType": null,
        "locationUri": null,
        "uniqueId": null,
        "uniqueIdType": null
      },
      maximumAttendeesCount: 5,
      filledAttendeesCount: 1,
      staffMemberIds: [],
      customers: [
        {
          "@odata.type": "#microsoft.graph.bookingCustomerInformation",
          "customerId": props.forCustomer.id,
          "name": props.forCustomer.displayName,
          "emailAddress": props.forCustomer.emailAddress,
          "timeZone": "",
          "notes": "",
          "location": {
            "displayName": "",
            "locationEmailAddress": "",
            "locationUri": "",
            "locationType": "default",
            "uniqueId": null,
            "uniqueIdType": null,
            "address": {
              "street": "",
              "city": "",
              "state": "",
              "countryOrRegion": "",
              "postalCode": ""
            },
            "coordinates": {
              "altitude": 0,
              "latitude": 0,
              "longitude": 0,
              "accuracy": 0,
              "altitudeAccuracy": 0
            }
          },
          "customQuestionAnswers": []
        }
      ]
    };

    selectedStaff.forEach((s: BookingStaffMember) => bookingAppointment.staffMemberIds?.push(s.id!));

    props.newAppointment(bookingAppointment);
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

          <p>Select service:</p>
          <ServicePicker options={props.services} optionSelected={(s: BookingService) => setSelectedService(s)} />

          <p>Select staff:</p>
          <StaffList allStaff={props.staffMembers} newStaffList={(s: BookingStaffMember[]) => setSelectedStaff(s)} />
          <pre>{JSON.stringify(selectedStaff)}</pre>
        </>
      }
      <Button onClick={newAppointment}>Create</Button>
    </div >
  );
}
