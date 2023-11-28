

import { BookingAppointment, BookingBusiness, BookingCustomer, BookingStaffMember, User } from "@microsoft/microsoft-graph-types";
import { Button } from "react-bootstrap";
import Tab from 'react-bootstrap/Tab';
import Tabs from 'react-bootstrap/Tabs';
import { ContosoClinicGraphLoader } from "../../services/ContosoClinicGraphLoader";
import { useEffect, useState } from "react";
import { AppointmentsList } from "./AppointmentsList";
import { UserLoaderCache } from "../../services/GraphObjectsLoaderCaches";
import { NewAppointment } from "./NewAppointment";
import { CustomersList } from "./CustomersList";


export function AppointmentMainContent(props: { loader: ContosoClinicGraphLoader, userCache: UserLoaderCache, business: BookingBusiness, user: User }) {

  const [appointments, setAppointments] = useState<BookingAppointment[] | null>(null);
  const [staffMembers, setStaffMembers] = useState<BookingStaffMember[] | null>(null);
  const [userCustomer, setUserCustomer] = useState<BookingCustomer | null>(null);
  const [allCustomers, setAllCustomers] = useState<BookingCustomer[] | null>(null);
  const [view, setView] = useState<AppointmentView>(AppointmentView.List);

  useEffect(() => {

    // On load....

    // Load appointments
    if (props.business.id) {
      props.loader.loadBusinessAppointments(props.business.id).then((r: BookingAppointment[]) => {
        setAppointments(r);
      });

      // Staff members
      props.loader.loadBusinessStaffMembers(props.business.id).then((r: BookingStaffMember[]) => {
        setStaffMembers(r);
      });

      // See if this logged in user exists as a customer for this business. Also set all customers during call
      if (!userCustomer) {
        props.loader.loadBusinessCustomerByGraphUser(props.business.id, props.user, (cxs: BookingCustomer[]) => setAllCustomers(cxs))
          .then((c: BookingCustomer | null | undefined) => {
            if (!c) {
              props.loader.createBusinessCustomer(props.business.id!, props.user).then((createdCustomer: BookingCustomer | null | undefined) => {
                if (createdCustomer) {
                  setUserCustomer(createdCustomer);
                }
                else
                  alert('Unexpected result from creating customer record for user');
              });
            }
            else
              setUserCustomer(c);
          });
      }

    }

    // eslint-disable-next-line
  }, []);

  return (
    <div>
      <h1>Selected Org: {props.business.displayName}</h1>
      <p>Your customer record with email '{props.user.mail}':</p>
      <pre>{JSON.stringify(userCustomer)}</pre>

      <Tabs
        defaultActiveKey="appointments"
        id="tabs"
        className="mb-3"
      >
        <Tab eventKey="appointments" title="Appointments">
          {appointments &&
            <>
              {view === AppointmentView.List &&
                <>
                  <h3>Existing Appointments</h3>
                  <AppointmentsList data={appointments} loader={props.loader} forBusiness={props.business} />

                  <Button onClick={() => setView(AppointmentView.New)}>New Appointment</Button>
                </>
              }
              {view === AppointmentView.New &&
                <>
                  <h3>New Appointment</h3>
                  {staffMembers ?
                    <NewAppointment existingAppointments={appointments} staffMembers={staffMembers}
                      newAppointment={() => setView(AppointmentView.List)} />
                    :
                    <p>No staff members found</p>
                  }
                </>
              }
            </>
          }
        </Tab>
        <Tab eventKey="customers" title="Customers">
          {allCustomers &&
            <>
              <h3>Customers</h3>
              <CustomersList data={allCustomers} forBusiness={props.business} />

            </>
          }
        </Tab>
      </Tabs>

    </div >
  );
}

enum AppointmentView {
  List,
  New
}