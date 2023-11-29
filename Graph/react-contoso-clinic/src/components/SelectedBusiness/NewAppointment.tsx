import { Button } from "react-bootstrap";
import { useEffect, useState } from "react";
import { GetDatesBetween, GetDatesExcluding, addHour } from "../../services/DateFunctions";
import { TimeslotPicker } from "../common/TimeslotPicker";
import { BookingAppointment, BookingCustomer, BookingService, BookingStaffMember } from "@microsoft/microsoft-graph-types";
import { StaffList } from "./StaffList";
import { ServicePicker } from "./ServicePicker";
import moment from "moment";

interface Props {
  existingAppointments: BookingAppointment[],
  staffMembers: BookingStaffMember[],
  services: BookingService[],
  forCustomer: BookingCustomer,
  newAppointment: Function,
  cancel: Function
}

export function NewAppointment(props: Props) {

  const [dateSlots, setDateSlots] = useState<Date[] | null>(null);
  const [savingAppointment, setSavingAppointment] = useState<boolean>(false);
  const [selectedDate, setSelectedDate] = useState<Date | null>(null);
  const [selectedService, setSelectedService] = useState<BookingService | null>(null);
  const [selectedStaff, setSelectedStaff] = useState<BookingStaffMember[] | null>(null);

  const formatDate = (dt: Date) : string => {
    const m = moment(dt);
    return m.format("yyyy-MM-DD") + "T" + m.format("HH") + ":00:00.0000000";
  }

  const newAppointment = () => {

    if (!selectedService || !selectedDate || !selectedStaff || selectedStaff?.length === 0) {
      alert("Fill out form: pick staff members and a time-slot");
      return;
    }
    setSavingAppointment(true);

    const timeZone = 'Central European Standard Time';  // Hard-coded hack for now

    const bookingAppointment: BookingAppointment = {
      startDateTime: {
        dateTime: formatDate(selectedDate),
        timeZone: timeZone
      },
      endDateTime: {
        dateTime: formatDate(addHour(selectedDate, 1)),
        timeZone: timeZone
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

  useEffect(() => {

    const now = new Date();
    const dayStart = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 9)
    const dayEnd = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 18);

    // Figure out dates available
    const dayDatesAll = GetDatesBetween(dayStart, dayEnd, 1);
    
    const appointmentDates = props.existingAppointments.map(a => a.startDateTime!).map(d => moment.utc(d.dateTime).local().toDate());

    setDateSlots(GetDatesExcluding(dayDatesAll, appointmentDates));

  }, [props.existingAppointments]);

  return (
    <div>
      {!savingAppointment ?
        <>
          {dateSlots &&
            <>
              <div className="row g-3">
                <div className="col">
                  <label>Select time for today:</label>
                  <TimeslotPicker options={dateSlots} optionSelected={(dt: Date) => setSelectedDate(dt)} />
                </div>
                <div className="col">
                  <label>Select service:</label>
                  <ServicePicker options={props.services} optionSelected={(s: BookingService) => setSelectedService(s)} />
                </div>
              </div>

              <div className="row">
                <div className="col">
                  <label>Select staff:</label>
                  <StaffList allStaff={props.staffMembers} newStaffList={(s: BookingStaffMember[]) => setSelectedStaff(s)} />
                </div>
              </div>

              <div className="col-12" style={{marginTop: 20}}>
                <Button onClick={newAppointment} className="btn btn-lg btn-primary">Create Appointment</Button>
                <Button onClick={() => props.cancel()} className="btn btn-lg btn-secondary">Cancel</Button>
              </div>
            </>
          }
        </>
        :
        <>Loading...</>
      }

    </div >
  );
}
