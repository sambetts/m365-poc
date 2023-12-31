import { BookingAppointment, BookingBusiness, BookingCustomerInformation, BookingStaffMember } from "@microsoft/microsoft-graph-types";
import StaffMember from "./StaffMemberLabel";
import moment from "moment";

export function AppointmentsList(props: { forBusiness: BookingBusiness, data: BookingAppointment[], allStaffMembers: BookingStaffMember[] }) {

  return (
    <>
      <table className="table">
        <thead>
          <tr><th>Customer</th><th>When</th><th>With Medic(s)</th></tr>
        </thead>
        <tbody>
          {props.data.length > 0 ?

            <>
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
                      <>{moment.utc(b.startDateTime?.dateTime).local().format("DD/MM/YYYY HH:mm:ss")}</>
                    }
                  </td>
                  <td>
                    {b.staffMemberIds &&
                      <>
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
            </>
            :
            <>
            <tr>
              <td colSpan={3}>No appointments</td>
            </tr>
            </>
          }
        </tbody>
      </table>
    </>
  );
}
