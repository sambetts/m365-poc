import { BookingAppointment, BookingBusiness, BookingCustomer, BookingService, BookingStaffMember, User } from "@microsoft/microsoft-graph-types";
import { Button } from "react-bootstrap";
import Tab from 'react-bootstrap/Tab';
import Tabs from 'react-bootstrap/Tabs';
import { ContosoClinicGraphLoader } from "../../services/ContosoClinicGraphLoader";
import { useEffect, useState } from "react";
import { AppointmentsList } from "./AppointmentsList";
import { StaffMemberLoaderCache, UserLoaderCache } from "../../services/GraphObjectsLoaderCaches";
import { NewAppointment } from "./NewAppointment";
import { CustomersList } from "./CustomersList";
import { AppointmentView } from "../../models";
import { ServicesList } from "./ServicesList";

export function AppointmentMainContent(props: { loader: ContosoClinicGraphLoader, userCache: UserLoaderCache, business: BookingBusiness, user: User }) {

  const [newAppointment, setNewAppointment] = useState<BookingAppointment | undefined>(undefined);
  const [createdAppointment, setCreatedAppointment] = useState<BookingAppointment | undefined>(undefined);

  const [staffMemberLoaderCache, setStaffMemberLoaderCache] = useState<StaffMemberLoaderCache | null>(null);

  const [appointments, setAppointments] = useState<BookingAppointment[] | null>(null);
  const [staffMembers, setStaffMembers] = useState<BookingStaffMember[] | null>(null);
  const [services, setServices] = useState<BookingService[] | null>(null);
  const [userCustomer, setUserCustomer] = useState<BookingCustomer | null | undefined>(undefined);
  const [allCustomers, setAllCustomers] = useState<BookingCustomer[] | null>(null);
  const [view, setView] = useState<AppointmentView>(AppointmentView.List);

  // Load staff members & services
  useEffect(() => {
    if (props.business.id) {

      if (!staffMemberLoaderCache) {
        setStaffMemberLoaderCache(new StaffMemberLoaderCache(props.loader, props.business.id));
      }

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

    if (props.business.id && !appointments) {

      props.loader.loadBusinessAppointments(props.business.id).then((r: BookingAppointment[]) => {
        setAppointments(r);
      });
    }

  }, [props.business.id, props.loader, props.user, newAppointment]);


  // Business customer for logged in user
  useEffect(() => {
    if (props.business.id) {

      // See if this logged in user exists as a customer for this business. Also set all customers during call
      if (userCustomer === undefined) {

        // Customer record for user not loaded yet
        props.loader.loadBusinessCustomerByGraphUser(props.business.id, props.user, (cxs: BookingCustomer[]) => setAllCustomers(cxs))
          .then((c: BookingCustomer | null | undefined) => {

            setUserCustomer(null);    // Avoid reload. Effect will rerun. 
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
      <h1>Selected Org: {props.business.displayName}</h1>
      <p>Your customer record with email '{props.user.mail}':</p>
      <pre>{JSON.stringify(userCustomer)}</pre>

      <Tabs
        defaultActiveKey="appointments"
        id="tabs"
        className="mb-3"
      >
        <Tab eventKey="appointments" title="Appointments">
          {appointments && staffMemberLoaderCache &&
            <>
              {view === AppointmentView.List &&
                <>
                  <h3>Existing Appointments</h3>
                  <AppointmentsList data={appointments} staffLoader={staffMemberLoaderCache} forBusiness={props.business} />

                  <Button onClick={() => setView(AppointmentView.New)}>New Appointment</Button>
                </>
              }
              {view === AppointmentView.New &&
                <>
                  <h3>New Appointment</h3>
                  {staffMembers && userCustomer && services ?
                    <NewAppointment existingAppointments={appointments} forCustomer={userCustomer} staffMembers={staffMembers}
                      services={services}
                      newAppointment={(r: BookingAppointment) => setNewAppointment(r)} />
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
          <h3>Created Appointment</h3>
          <pre>{JSON.stringify(createdAppointment)}</pre>
        </>

      }

    </div >
  );
}
