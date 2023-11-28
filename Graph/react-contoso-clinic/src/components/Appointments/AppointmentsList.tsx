

import { BookingAppointment, BookingBusiness, BookingCustomerInformation, BookingStaffMember } from "@microsoft/microsoft-graph-types";
import StaffMember from "../StaffMember";

export function AppointmentsList(props: { forBusiness: BookingBusiness, data: BookingAppointment[], allStaffMembers: BookingStaffMember[] }) {

  return (
    <>
      <table className="table">
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
                        <StaffMember allStaffMembers={props.allStaffMembers} staffMemberId={id} />, 
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
