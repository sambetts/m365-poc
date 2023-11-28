

import { BookingAppointment, BookingBusiness, BookingCustomerInformation } from "@microsoft/microsoft-graph-types";
import StaffMember from "../StaffMember";
import { ContosoClinicGraphLoader } from "../../services/ContosoClinicGraphLoader";

export function AppointmentsList(props: { forBusiness: BookingBusiness, data: BookingAppointment[], loader: ContosoClinicGraphLoader }) {

  return (
    <>
      <table>
        <tbody>
          {props.data.map((b: BookingAppointment) => {
            return <tr key={b.id}>
              <td>
                {b.customers &&
                  <>
                    {b.customers.map((c: BookingCustomerInformation) => {
                      return <div>{c.name}</div>
                    })
                    }
                  </>
                }
              </td>
              <td>@
                {b.startDateTime?.dateTime &&
                  <>{new Date(b.startDateTime?.dateTime).toLocaleDateString("en-GB")}</>
                }
              </td>
              <td>
                {b.staffMemberIds &&
                  <>
                  With:
                    {b.staffMemberIds.map(id => {
                      return <>
                        <StaffMember loader={props.loader} staffMemberId={id} businessId={props.forBusiness.id!} />, 
                      </>
                    })}
                  </>
                }

              </td>
            </tr>
          })
          }
        </tbody>
      </table>
    </>
  );
}
