import '../App.css';
import { AuthenticatedTemplate } from '@azure/msal-react';
import { useEffect, useState } from 'react';
import { ContosoClinicGraphLoader } from '../services/ContosoClinicGraphLoader';
import { BookingBusiness, User } from '@microsoft/microsoft-graph-types';
import { BusinessList } from './Appointments/BusinessList';
import { AppointmentMainContent } from './Appointments/AppointmentMainContent';
import { UserLoaderCache } from '../services/GraphObjectsLoaderCaches';

export default function AppMainContent(props: { loader: ContosoClinicGraphLoader, userCache : UserLoaderCache, userLoaded: Function }) {

  const [bookingBusinesses, setBookingBusinesses] = useState<microsoftgraph.BookingBusiness[] | null>(null);
  const [selectedBookingBusiness, setSelectedBookingBusiness] = useState<microsoftgraph.BookingBusiness | null>(null);
  const [user, setUser] = useState<microsoftgraph.User | undefined>(undefined);

  const userLoadedCallback = (user: User) => {
    props.userLoaded(user);
  };

  useEffect(() => {
    props.loader.loadUserProfile().then((user: User) => {
      setUser(user);
      userLoadedCallback(user);
    });

    props.loader.loadBookingBusinesses().then((r: BookingBusiness[]) => {
      setBookingBusinesses(r);
    });

    // eslint-disable-next-line
  }, []);

  return (
    <>
      <AuthenticatedTemplate>

        <div className="dashboard-item" id="email-list">

          {selectedBookingBusiness && user ?
            <>
              <AppointmentMainContent business={selectedBookingBusiness} loader={props.loader} user={user} />
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
