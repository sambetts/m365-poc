import '../App.css';
import { AuthenticatedTemplate } from '@azure/msal-react';
import { useEffect, useState } from 'react';
import { ExampleAppGraphLoader } from '../services/ExampleAppGraphLoader';
import { BookingBusiness, User } from '@microsoft/microsoft-graph-types';
import { BusinessList } from './Appointments/BusinessList';
import { AppointmentWizard } from './Appointments/AppointmentWizard';

export default function AppMainContent(props: { loader: ExampleAppGraphLoader, userLoaded: Function }) {

  const [bookingBusinesses, setBookingBusinesses] = useState<microsoftgraph.BookingBusiness[] | null>(null);
  const [selectedBookingBusiness, setSelectedBookingBusiness] = useState<microsoftgraph.BookingBusiness | null>(null);

  useEffect(() => {

    props.loader.loadUserProfile().then((user: User) => {
      props.userLoaded(user);
    });

    props.loader.loadBookingBusinesses().then((r: BookingBusiness[]) => {
      setBookingBusinesses(r);
    });

  }, [props.loader]);

  return (
    <>
      <AuthenticatedTemplate>

        <div className="dashboard-item" id="email-list">

          {selectedBookingBusiness ?
            <>
              <AppointmentWizard business={selectedBookingBusiness} loader={props.loader} />
            </>
            :
            <>
              <h2>Select Business</h2>

              {bookingBusinesses ?
                <BusinessList businesses={bookingBusinesses} select={(b: BookingBusiness) => setSelectedBookingBusiness(b)} />
                :
                <p>Loading businesses...</p>
              }
            </>
          }

        </div>
      </AuthenticatedTemplate>
    </>
  );
}
