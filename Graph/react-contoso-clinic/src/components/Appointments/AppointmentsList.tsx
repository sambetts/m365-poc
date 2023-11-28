

import { BookingAppointment, BookingBusiness, BookingCustomerInformation, BookingStaffMember } from "@microsoft/microsoft-graph-types";
import StaffMember from "../StaffMember";
import moment from "moment";

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
                  <>{moment(new Date(b.startDateTime?.dateTime)).format("DD/MM/YYYY HH:mm:ss")}</>
                }
              </td>
              <td>
                {b.staffMemberIds &&
                  <>
                  <span>With: </span>
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
