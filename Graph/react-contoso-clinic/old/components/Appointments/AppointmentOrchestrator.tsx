
import { useState } from "react";
import { Client } from "@microsoft/microsoft-graph-client";

import { BookingBusiness } from "@microsoft/microsoft-graph-types";
import React from "react";
import { IGraphArrayResponse } from "../lib/GraphResponse";
import { BusinessList } from "./BusinessList";
import { AppointmentWizard } from "./AppointmentWizard";

export function AppointmentOrchestrator(props: { graphClient: Client }) {

  const [selectedBookingBusiness, setSelectedBookingBusiness] = useState<BookingBusiness | null>(null);
  const [allBookingBusiness, setAllBookingBusiness] = useState<BookingBusiness[] | null>(null); 
  
  // Load businesses
  React.useEffect(() => {

    props.graphClient.api(`/solutions/bookingBusinesses`).get()
      .then((r: IGraphArrayResponse<BookingBusiness>) => {
        setAllBookingBusiness(r.value);
      })
      .catch(er => alert("Couldn't post call to channel: " + er.Message));
  }, []);

  return (
    <div>
      {selectedBookingBusiness ?
        <>
          <AppointmentWizard business={selectedBookingBusiness} graphClient={props.graphClient} />
        </>
        :
        <>
          {allBookingBusiness ?
            <>
              <h3>Pick a Business</h3>
              <BusinessList businesses={allBookingBusiness} select={(b: BookingBusiness) => setSelectedBookingBusiness(b)} />
            </>
            :
            <>
              <p>Loading Businesses...</p>
            </>
          }
        </>
      }

    </div >
  );
}
