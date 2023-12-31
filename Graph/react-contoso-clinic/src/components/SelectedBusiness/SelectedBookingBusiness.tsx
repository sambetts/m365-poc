import { BookingAppointment, BookingBusiness, BookingCustomer, BookingService, BookingStaffMember, User } from "@microsoft/microsoft-graph-types";
import { Button } from "react-bootstrap";
import Tab from 'react-bootstrap/Tab';
import Tabs from 'react-bootstrap/Tabs';
import { ContosoClinicGraphLoader } from "../../services/ContosoClinicGraphLoader";
import { useEffect, useState } from "react";
import { AppointmentsList } from "./AppointmentsList";
import { NewAppointment } from "./NewAppointment";
import { CustomersList } from "./CustomersList";
import { AppointmentView } from "../../models";
import { ServicesList } from "./ServicesList";

export function SelectedBookingBusiness(props: { loader: ContosoClinicGraphLoader, business: BookingBusiness, user: User }) {

  const [newAppointment, setNewAppointment] = useState<BookingAppointment | undefined>(undefined);
  const [createdAppointment, setCreatedAppointment] = useState<BookingAppointment | undefined>(undefined);

  const [appointments, setAppointments] = useState<BookingAppointment[] | null>(null);
  const [staffMembers, setStaffMembers] = useState<BookingStaffMember[] | null>(null);
  const [services, setServices] = useState<BookingService[] | null>(null);
  const [userCustomer, setUserCustomer] = useState<BookingCustomer | null | undefined>(undefined);
  const [allCustomers, setAllCustomers] = useState<BookingCustomer[] | null>(null);
  const [view, setView] = useState<AppointmentView>(AppointmentView.List);

  // Load staff members & services
  useEffect(() => {
    if (props.business.id) {


      // Staff members
      props.loader.loadBusinessStaffMembers(props.business.id).then((r: BookingStaffMember[]) => {
        setStaffMembers(r);
      });

      // Services
      props.loader.loadBusinessServices(props.business.id).then((r: BookingService[]) => {
        setServices(r);
      });
    }

  }, [props.business.id, props.loader, props.user]);

  // Load appointments...
  useEffect(() => {

    if (props.business.id) {

      props.loader.loadBusinessAppointments(props.business.id).then((r: BookingAppointment[]) => {
        setAppointments(r);
      });
    }

  }, [props.business.id, props.loader, props.user, createdAppointment]);


  // Business customer for logged in user
  useEffect(() => {
    if (props.business.id) {

      // See if this logged in user exists as a customer for this business. Also set all customers during call
      if (userCustomer === undefined) {

        // Customer record for user not loaded yet
        props.loader.loadBusinessCustomerByGraphUser(props.business.id, props.user, (cxs: BookingCustomer[]) => setAllCustomers(cxs))
          .then((loggedInCustomer: BookingCustomer | null | undefined) => {
            setUserCustomer(loggedInCustomer);
          });
      }
      else if (userCustomer === null) {

        // No customer record in Graph for business
        props.loader.createBusinessCustomer(props.business.id!, props.user).then((createdCustomer: BookingCustomer | null | undefined) => {
          if (createdCustomer) {
            setUserCustomer(createdCustomer);
          }
          else
            alert('Unexpected result from creating customer record for user');
        });
      }
    }
  }, [props.business.id, props.loader, props.user, userCustomer]);


  useEffect(() => {

    if (props.business.id) {

      // Create appointment
      if (newAppointment) {

        props.loader.createAppointment(props.business.id, newAppointment)
          .then((r: BookingAppointment) => {
            setView(AppointmentView.List);
            setCreatedAppointment(r);
          });
      }
    }
  }, [props.business.id, props.loader, props.user, newAppointment]);


  return (
    <div>
      <h1>Selected Booking Business: {props.business.displayName}</h1>
      <p>You are registered as a customer with email '{props.user.mail}' Customers can use any email, and normally don't need an account.</p>

      <Tabs
        defaultActiveKey="appointments"
        id="tabs"
        className="mb-3"
      >
        <Tab eventKey="appointments" title="Appointments">
          {appointments && staffMembers &&
            <>
              {view === AppointmentView.List &&
                <>
                  <h3>Existing Appointments</h3>
                  <AppointmentsList data={appointments} allStaffMembers={staffMembers} forBusiness={props.business} />

                  <Button onClick={() => setView(AppointmentView.New)}>New Appointment</Button>
                </>
              }
              {view === AppointmentView.New &&
                <>
                  <h3>New Appointment</h3>
                  {staffMembers && userCustomer && services ?
                    <NewAppointment existingAppointments={appointments} forCustomer={userCustomer} staffMembers={staffMembers}
                      services={services}
                      newAppointment={(r: BookingAppointment) => setNewAppointment(r)} cancel={()=> setView(AppointmentView.List)} />
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
        <Tab eventKey="services" title="Services">
          {services &&
            <>
              <h3>Services</h3>
              <ServicesList data={services} forBusiness={props.business} />
            </>
          }
        </Tab>
      </Tabs>

      {createdAppointment &&
        <>
          <h4>Last Created Appointment</h4>
          <pre style={{maxWidth: 1200}}>{JSON.stringify(createdAppointment, null, 4)}</pre>
        </>
      }

    </div >
  );
}
